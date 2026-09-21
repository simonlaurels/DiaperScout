using System.Text.Json;
using System.Text.RegularExpressions;
using DiaperScout.Application;
using DiaperScout.Domain;
using DiaperScout.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace DiaperScout.Infrastructure;

internal sealed class CurrentUser(DiaperScoutDbContext db, IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public async Task<AuthenticatedUser?> GetAsync(CancellationToken cancellationToken = default)
    {
        var principal = httpContextAccessor.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true) return null;
        var subject = principal.FindFirst("sub")?.Value ?? principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(subject)) return null;

        return await db.Users.AsNoTracking()
            .Where(user => user.Subject == subject && user.Status == UserAccountStatus.Active)
            .Select(user => new AuthenticatedUser(user.Id, user.Subject))
            .SingleOrDefaultAsync(cancellationToken);
    }
}

internal sealed class EditorialAuthorisation(DiaperScoutDbContext db) : IEditorialAuthorisation
{
    public Task<bool> CanPublishAtlasAsync(AuthenticatedUser user, CancellationToken cancellationToken = default) =>
        db.PrivilegedRoleAssignments.AnyAsync(assignment => assignment.UserId == user.UserId
            && assignment.Role == PrivilegedRole.Moderator
            && assignment.RevokedAtUtc == null, cancellationToken);

    public Task<bool> CanManageCatalogueAsync(AuthenticatedUser user, CancellationToken cancellationToken = default) =>
        db.PrivilegedRoleAssignments.AnyAsync(assignment => assignment.UserId == user.UserId
            && (assignment.Role == PrivilegedRole.Moderator || assignment.Role == PrivilegedRole.Administrator)
            && assignment.RevokedAtUtc == null, cancellationToken);
}

internal sealed class PrivilegedRoleAssignments(DiaperScoutDbContext db) : IPrivilegedRoleAssignments
{
    public async Task<PrivilegedRoleAssignmentReceipt> GrantAsync(AuthenticatedUser actor, Guid subjectUserId, PrivilegedRole role, CancellationToken cancellationToken = default)
    {
        await RequireAdministratorAsync(actor, cancellationToken);
        if (!await db.Users.AnyAsync(user => user.Id == subjectUserId && user.Status == UserAccountStatus.Active, cancellationToken))
            throw new CatalogueValidationException("subjectUserId", "The subject user was not found or is not active.");
        if (await db.PrivilegedRoleAssignments.AnyAsync(assignment => assignment.UserId == subjectUserId && assignment.Role == role && assignment.RevokedAtUtc == null, cancellationToken))
            throw new CatalogueValidationException("role", "The user already has this active role.");

        var now = DateTimeOffset.UtcNow;
        var assignment = new PrivilegedRoleAssignment(subjectUserId, role, actor.UserId, now);
        db.AddRange(assignment, new PrivilegedRoleAssignmentAudit(actor.UserId, subjectUserId, role, PrivilegedRoleAssignmentAction.Granted, now));
        await db.SaveChangesAsync(cancellationToken);
        return new PrivilegedRoleAssignmentReceipt(assignment.Id, subjectUserId, role, true);
    }

    public async Task RevokeAsync(AuthenticatedUser actor, Guid subjectUserId, PrivilegedRole role, CancellationToken cancellationToken = default)
    {
        await RequireAdministratorAsync(actor, cancellationToken);
        var assignment = await db.PrivilegedRoleAssignments.SingleOrDefaultAsync(value => value.UserId == subjectUserId && value.Role == role && value.RevokedAtUtc == null, cancellationToken)
            ?? throw new CatalogueValidationException("role", "The user does not have this active role.");
        var now = DateTimeOffset.UtcNow;
        assignment.Revoke(actor.UserId, now);
        db.PrivilegedRoleAssignmentAudits.Add(new PrivilegedRoleAssignmentAudit(actor.UserId, subjectUserId, role, PrivilegedRoleAssignmentAction.Revoked, now));
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task RequireAdministratorAsync(AuthenticatedUser actor, CancellationToken cancellationToken)
    {
        if (!await db.PrivilegedRoleAssignments.AnyAsync(assignment => assignment.UserId == actor.UserId && assignment.Role == PrivilegedRole.Administrator && assignment.RevokedAtUtc == null, cancellationToken))
            throw new UnauthorizedAccessException("Only an assigned Administrator may manage privileged roles.");
    }
}

internal sealed class CanonicalCatalogue(DiaperScoutDbContext db, IEditorialAuthorisation editorialAuthorisation, ICatalogueSubmissionImageStorage imageStorage) : ICanonicalCatalogue
{
    private static readonly Regex SlugPattern = new("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.Compiled);
    private static readonly Regex GtinPattern = new("^[0-9]{8,14}$", RegexOptions.Compiled);

    public async Task SetDescriptionVisibilityAsync(
        AuthenticatedUser actor,
        Guid productId,
        CatalogueContentVisibility visibility,
        CancellationToken cancellationToken = default)
    {
        if (!await editorialAuthorisation.CanPublishAtlasAsync(actor, cancellationToken))
            throw new UnauthorizedAccessException();

        var product = await db.Products.SingleOrDefaultAsync(value => value.Id == productId, cancellationToken)
            ?? throw new KeyNotFoundException("The catalogue product was not found.");

        if (!Enum.IsDefined(visibility))
            throw new ArgumentException("The description visibility is invalid.", nameof(visibility));

        product.SetDescriptionVisibility(visibility);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateProductIdentityAsync(
        AuthenticatedUser actor,
        Guid productId,
        UpdateCanonicalProductIdentity command,
        CancellationToken cancellationToken = default)
    {
        if (!await editorialAuthorisation.CanManageCatalogueAsync(actor, cancellationToken))
            throw new UnauthorizedAccessException();

        Validate(command);

        var product = await db.Products.SingleOrDefaultAsync(value => value.Id == productId, cancellationToken)
            ?? throw new KeyNotFoundException("The catalogue product was not found.");

        if (!await db.Manufacturers.AnyAsync(value => value.Id == command.ManufacturerId, cancellationToken))
            throw new CatalogueValidationException("manufacturerId", "The manufacturer was not found.");

        if (command.BrandId is { } brandId &&
            !await db.Brands.AnyAsync(value => value.Id == brandId && value.ManufacturerId == command.ManufacturerId, cancellationToken))
            throw new CatalogueValidationException("brandId", "The brand was not found for the selected manufacturer.");

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        product.UpdateIdentity(command.ManufacturerId, command.BrandId, command.ProductName, command.ProductType, command.ProductFamily, command.Description, command.DescriptionVisibility, command.OfficialWebsiteUrl);
        await db.SaveChangesAsync(cancellationToken);

        db.CatalogueAuditRecords.Add(new CatalogueAuditRecord(
            CatalogueAuditAction.ProductChanged,
            product.Id,
            actor.UserId,
            DateTimeOffset.UtcNow,
            JsonSerializer.Serialize(command),
            JsonSerializer.Serialize(new[] { product.Id }),
            command.SourceSummary.Trim(),
            JsonSerializer.Serialize(command.SourceReferences.Select(value => value.Trim()).Where(value => value.Length > 0)),
            command.EditorialRationale.Trim(),
            command.CorrelationId?.Trim()));

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task AddProductVariantAsync(
        AuthenticatedUser actor,
        Guid productId,
        CreateCanonicalProductVariantManagement command,
        CancellationToken cancellationToken = default)
    {
        await RequireCatalogueManagerAsync(actor, cancellationToken);
        ValidateVariantCommand(command.Name, command.BackingType, command.FastenerType, command.Appearance, command.PrimaryColour, command.WaistbandStyle, command.Fragrance, command.DesignedFor, command.FastenerCount, command.SourceSummary, command.EditorialRationale);

        var product = await db.Products.SingleOrDefaultAsync(value => value.Id == productId, cancellationToken)
            ?? throw new KeyNotFoundException("The catalogue product was not found.");

        var duplicate = await db.ProductVariants.AnyAsync(
            value => value.ProductId == productId && value.Name.ToLower() == command.Name.Trim().ToLower(),
            cancellationToken);
        if (duplicate)
            throw new CatalogueValidationException("name", "A product variant with this name already exists.");

        var variant = new ProductVariant(product.Id, command.Name.Trim(), command.BackingType, command.FastenerType, command.Appearance, command.PrimaryColour.ToString(), command.HasWetnessIndicator, command.HasStandingLeakGuards, command.WaistbandStyle, command.Fragrance, command.IsLatexFree, command.DesignedFor.ToString(), command.FastenerCount, command.ConstructionNotes);
        db.ProductVariants.Add(variant);
        await db.SaveChangesAsync(cancellationToken);

        await AddAuditAsync(
            CatalogueAuditAction.ProductChanged,
            product.Id,
            actor,
            command,
            new[] { product.Id, variant.Id },
            cancellationToken);
    }

    public async Task UpdateProductVariantAsync(
        AuthenticatedUser actor,
        Guid productId,
        Guid variantId,
        UpdateCanonicalProductVariantManagement command,
        CancellationToken cancellationToken = default)
    {
        await RequireCatalogueManagerAsync(actor, cancellationToken);
        ValidateVariantCommand(command.Name, command.BackingType, command.FastenerType, command.Appearance, command.PrimaryColour, command.WaistbandStyle, command.Fragrance, command.DesignedFor, command.FastenerCount, command.SourceSummary, command.EditorialRationale);

        var variant = await db.ProductVariants.SingleOrDefaultAsync(
            value => value.Id == variantId && value.ProductId == productId,
            cancellationToken)
            ?? throw new KeyNotFoundException("The product variant was not found.");

        var duplicate = await db.ProductVariants.AnyAsync(
            value => value.ProductId == productId && value.Id != variantId && value.Name.ToLower() == command.Name.Trim().ToLower(),
            cancellationToken);
        if (duplicate)
            throw new CatalogueValidationException("name", "A product variant with this name already exists.");

        variant.UpdateDetails(command.Name, command.BackingType, command.FastenerType, command.Appearance, command.PrimaryColour, command.HasWetnessIndicator, command.HasStandingLeakGuards, command.WaistbandStyle, command.Fragrance, command.IsLatexFree, command.DesignedFor, command.FastenerCount, command.ConstructionNotes);
        await db.SaveChangesAsync(cancellationToken);

        await AddAuditAsync(
            CatalogueAuditAction.ProductChanged,
            productId,
            actor,
            command,
            new[] { productId, variantId },
            cancellationToken);
    }

    public async Task RemoveProductVariantAsync(
        AuthenticatedUser actor,
        Guid productId,
        Guid variantId,
        string sourceSummary,
        IReadOnlyList<string> sourceReferences,
        string editorialRationale,
        string? correlationId,
        CancellationToken cancellationToken = default)
    {
        await RequireCatalogueManagerAsync(actor, cancellationToken);
        ValidateAudit(sourceSummary, editorialRationale);

        var variants = await db.ProductVariants
            .Where(value => value.ProductId == productId)
            .OrderBy(value => value.Name)
            .ToListAsync(cancellationToken);

        var variant = variants.SingleOrDefault(value => value.Id == variantId)
            ?? throw new KeyNotFoundException("The product variant was not found.");

        if (variants.Count <= 1)
            throw new CatalogueValidationException("variantId", "A product must have at least one variant.");

        db.ProductVariants.Remove(variant);
        await db.SaveChangesAsync(cancellationToken);

        await AddAuditAsync(
            CatalogueAuditAction.ProductChanged,
            productId,
            actor,
            new { Action = "RemoveVariant", VariantId = variantId, SourceSummary = sourceSummary, SourceReferences = sourceReferences, EditorialRationale = editorialRationale, CorrelationId = correlationId },
            new[] { productId, variantId },
            cancellationToken,
            sourceSummary,
            sourceReferences,
            editorialRationale,
            correlationId);
    }

    public async Task AddProductSizeAsync(
        AuthenticatedUser actor,
        Guid productId,
        Guid variantId,
        CreateCanonicalProductSizeManagement command,
        CancellationToken cancellationToken = default)
    {
        await RequireCatalogueManagerAsync(actor, cancellationToken);
        ValidateSizeCommand(command.ManufacturerSize, command.ManufacturerPackQuantity, command.PackagingType, command.Gtin, command.SourceSummary, command.EditorialRationale);

        var variantExists = await db.ProductVariants.AnyAsync(
            value => value.Id == variantId && value.ProductId == productId,
            cancellationToken);
        if (!variantExists)
            throw new KeyNotFoundException("The product variant was not found.");

        var duplicate = await db.SizeVariants.AnyAsync(
            value => value.ProductVariantId == variantId && value.ManufacturerSize.ToLower() == command.ManufacturerSize.Trim().ToLower(),
            cancellationToken);
        if (duplicate)
            throw new CatalogueValidationException("manufacturerSize", "A size with this manufacturer size already exists for this product variant.");

        var gtin = NormaliseOptionalGtin(command.Gtin);
        if (gtin is not null && await db.ProductIdentifiers.AnyAsync(value => value.Type == IdentifierType.Gtin && value.Value == gtin, cancellationToken))
            throw new CatalogueValidationException("gtin", $"GTIN {gtin} is already assigned to a published catalogue product.");

        var size = new SizeVariant(
            variantId,
            command.ManufacturerSize,
            command.WaistMinimumCm,
            command.WaistMaximumCm,
            command.HipMinimumCm,
            command.HipMaximumCm,
            command.ManufacturerStatedAbsorbencyMl,
            command.FitMeasurementBasis,
            command.AbsorbencyBasisMethod,
            command.AbsorbencySource,
            command.LengthMm,
            command.WidthMm,
            command.WeightGrams);
        var pack = new PackType(size.Id, command.ManufacturerPackQuantity, command.PackagingType);
        db.AddRange(size, pack);

        if (gtin is not null)
            db.ProductIdentifiers.Add(new ProductIdentifier(pack.Id, IdentifierType.Gtin, gtin));

        await db.SaveChangesAsync(cancellationToken);
        await AddAuditAsync(
            CatalogueAuditAction.ProductChanged,
            productId,
            actor,
            command,
            new[] { productId, variantId, size.Id, pack.Id },
            cancellationToken);
    }

    public async Task UpdateProductSizeAsync(
        AuthenticatedUser actor,
        Guid productId,
        Guid variantId,
        Guid sizeId,
        UpdateCanonicalProductSizeManagement command,
        CancellationToken cancellationToken = default)
    {
        await RequireCatalogueManagerAsync(actor, cancellationToken);
        ValidateAudit(command.SourceSummary, command.EditorialRationale);

        var size = await db.SizeVariants.SingleOrDefaultAsync(
            value => value.Id == sizeId && value.ProductVariantId == variantId,
            cancellationToken)
            ?? throw new KeyNotFoundException("The size variant was not found.");

        if (!await db.ProductVariants.AnyAsync(value => value.Id == variantId && value.ProductId == productId, cancellationToken))
            throw new KeyNotFoundException("The product variant was not found.");

        var duplicate = await db.SizeVariants.AnyAsync(
            value => value.ProductVariantId == variantId && value.Id != sizeId && value.ManufacturerSize.ToLower() == command.ManufacturerSize.Trim().ToLower(),
            cancellationToken);
        if (duplicate)
            throw new CatalogueValidationException("manufacturerSize", "A size with this manufacturer size already exists for this product variant.");

        size.UpdateMeasurements(
            command.ManufacturerSize,
            command.WaistMinimumCm,
            command.WaistMaximumCm,
            command.HipMinimumCm,
            command.HipMaximumCm,
            command.ManufacturerStatedAbsorbencyMl,
            command.FitMeasurementBasis,
            command.AbsorbencyBasisMethod,
            command.AbsorbencySource,
            command.LengthMm,
            command.WidthMm,
            command.WeightGrams);

        if (command.ManufacturerPackQuantity.HasValue || command.PackagingType.HasValue || command.Gtin is not null)
        {
            var pack = await db.PackTypes.Include(value => value.Identifiers)
                .Where(value => value.SizeVariantId == sizeId)
                .OrderBy(value => value.QuantityPerPack)
                .FirstOrDefaultAsync(cancellationToken);

            if (pack is null)
            {
                var newPack = new PackType(size.Id, command.ManufacturerPackQuantity ?? 1, command.PackagingType ?? PackagingType.Bag);
                db.PackTypes.Add(newPack);
                if (NormaliseOptionalGtin(command.Gtin) is { } newGtin)
                    db.ProductIdentifiers.Add(new ProductIdentifier(newPack.Id, IdentifierType.Gtin, newGtin));
            }
            else
            {
                pack.UpdateDetails(command.ManufacturerPackQuantity ?? pack.QuantityPerPack, command.PackagingType ?? pack.PackagingType);
                var desiredGtin = NormaliseOptionalGtin(command.Gtin);
                if (desiredGtin is not null && await db.ProductIdentifiers.AnyAsync(
                        value => value.Type == IdentifierType.Gtin && value.Value == desiredGtin && value.PackTypeId != pack.Id,
                        cancellationToken))
                    throw new CatalogueValidationException("gtin", $"GTIN {desiredGtin} is already assigned to a published catalogue product.");

                var existingGtin = pack.Identifiers.FirstOrDefault(value => value.Type == IdentifierType.Gtin);
                if (existingGtin is not null && desiredGtin is null)
                    db.ProductIdentifiers.Remove(existingGtin);
                else if (existingGtin is not null && desiredGtin is not null)
                    existingGtin.UpdateValue(desiredGtin);
                else if (desiredGtin is not null)
                    db.ProductIdentifiers.Add(new ProductIdentifier(pack.Id, IdentifierType.Gtin, desiredGtin));
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        await AddAuditAsync(
            CatalogueAuditAction.ProductChanged,
            productId,
            actor,
            command,
            new[] { productId, variantId, sizeId },
            cancellationToken);
    }

    public async Task RemoveProductSizeAsync(
        AuthenticatedUser actor,
        Guid productId,
        Guid variantId,
        Guid sizeId,
        string sourceSummary,
        IReadOnlyList<string> sourceReferences,
        string editorialRationale,
        string? correlationId,
        CancellationToken cancellationToken = default)
    {
        await RequireCatalogueManagerAsync(actor, cancellationToken);
        ValidateAudit(sourceSummary, editorialRationale);

        var sizes = await db.SizeVariants
            .Where(value => value.ProductVariantId == variantId)
            .OrderBy(value => value.ManufacturerSize)
            .ToListAsync(cancellationToken);
        var size = sizes.SingleOrDefault(value => value.Id == sizeId)
            ?? throw new KeyNotFoundException("The size variant was not found.");

        if (!await db.ProductVariants.AnyAsync(value => value.Id == variantId && value.ProductId == productId, cancellationToken))
            throw new KeyNotFoundException("The product variant was not found.");

        if (sizes.Count <= 1)
            throw new CatalogueValidationException("sizeId", "A product variant must have at least one size.");

        db.SizeVariants.Remove(size);
        await db.SaveChangesAsync(cancellationToken);
        await AddAuditAsync(
            CatalogueAuditAction.ProductChanged,
            productId,
            actor,
            new { Action = "RemoveSize", VariantId = variantId, SizeId = sizeId },
            new[] { productId, variantId, sizeId },
            cancellationToken,
            sourceSummary,
            sourceReferences,
            editorialRationale,
            correlationId);
    }

    private async Task RequireCatalogueManagerAsync(AuthenticatedUser actor, CancellationToken cancellationToken)
    {
        if (!await editorialAuthorisation.CanManageCatalogueAsync(actor, cancellationToken))
            throw new UnauthorizedAccessException();
    }

    private static void ValidateVariantCommand(
        string name,
        BackingType backingType,
        FastenerType fastenerType,
        CatalogueVariantAppearance appearance,
        CatalogueVariantColour primaryColour,
        WaistbandStyle waistbandStyle,
        FragranceType fragrance,
        CatalogueVariantDesignedFor designedFor,
        int? fastenerCount,
        string sourceSummary,
        string editorialRationale)
    {
        Required(name, "name");
        ValidateAudit(sourceSummary, editorialRationale);
        if (!Enum.IsDefined(backingType) || !Enum.IsDefined(fastenerType) || !Enum.IsDefined(appearance) ||
            !Enum.IsDefined(primaryColour) || !Enum.IsDefined(waistbandStyle) || !Enum.IsDefined(fragrance) ||
            !Enum.IsDefined(designedFor))
            throw new CatalogueValidationException("variant", "One or more variant specification values are invalid.");
        if (fastenerCount is < 0)
            throw new CatalogueValidationException("fastenerCount", "Fastener count cannot be negative.");
    }

    private static void ValidateSizeCommand(string manufacturerSize, int manufacturerPackQuantity, PackagingType packagingType, string? gtin, string sourceSummary, string editorialRationale)
    {
        Required(manufacturerSize, "manufacturerSize");
        ValidateAudit(sourceSummary, editorialRationale);
        if (manufacturerPackQuantity <= 0)
            throw new CatalogueValidationException("manufacturerPackQuantity", "Pack quantity must be greater than zero.");
        if (!Enum.IsDefined(packagingType))
            throw new CatalogueValidationException("packagingType", "The packaging type is invalid.");
        if (NormaliseOptionalGtin(gtin) is { Length: > 0 } value && !GtinPattern.IsMatch(value))
            throw new CatalogueValidationException("gtin", "GTIN must contain 8 to 14 digits.");
    }

    private static void ValidateAudit(string sourceSummary, string editorialRationale)
    {
        Required(sourceSummary, "sourceSummary");
        Required(editorialRationale, "editorialRationale");
    }

    private static string? NormaliseOptionalGtin(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return NormaliseGtin(value);
    }

    private async Task AddAuditAsync(
        CatalogueAuditAction action,
        Guid productId,
        AuthenticatedUser actor,
        object command,
        IReadOnlyList<Guid> entityIds,
        CancellationToken cancellationToken,
        string? sourceSummary = null,
        IReadOnlyList<string>? sourceReferences = null,
        string? editorialRationale = null,
        string? correlationId = null)
    {
        if (command is UpdateCanonicalProductVariantManagement variantUpdate)
        {
            sourceSummary = variantUpdate.SourceSummary;
            sourceReferences = variantUpdate.SourceReferences;
            editorialRationale = variantUpdate.EditorialRationale;
            correlationId = variantUpdate.CorrelationId;
        }
        else if (command is CreateCanonicalProductVariantManagement variantCreate)
        {
            sourceSummary = variantCreate.SourceSummary;
            sourceReferences = variantCreate.SourceReferences;
            editorialRationale = variantCreate.EditorialRationale;
            correlationId = variantCreate.CorrelationId;
        }
        else if (command is CreateCanonicalProductSizeManagement sizeCreate)
        {
            sourceSummary = sizeCreate.SourceSummary;
            sourceReferences = sizeCreate.SourceReferences;
            editorialRationale = sizeCreate.EditorialRationale;
            correlationId = sizeCreate.CorrelationId;
        }
        else if (command is UpdateCanonicalProductSizeManagement sizeUpdate)
        {
            sourceSummary = sizeUpdate.SourceSummary;
            sourceReferences = sizeUpdate.SourceReferences;
            editorialRationale = sizeUpdate.EditorialRationale;
            correlationId = sizeUpdate.CorrelationId;
        }

        db.CatalogueAuditRecords.Add(new CatalogueAuditRecord(
            action,
            productId,
            actor.UserId,
            DateTimeOffset.UtcNow,
            JsonSerializer.Serialize(command),
            JsonSerializer.Serialize(entityIds),
            sourceSummary!.Trim(),
            JsonSerializer.Serialize((sourceReferences ?? []).Select(value => value.Trim()).Where(value => value.Length > 0)),
            editorialRationale!.Trim(),
            correlationId?.Trim()));
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<CatalogueModeratorProductImage> AddProductImageAsync(
        AuthenticatedUser actor,
        Guid productId,
        string storageKey,
        string originalFileName,
        string contentType,
        long fileSizeBytes,
        AddCanonicalProductImageMetadata command,
        CancellationToken cancellationToken = default)
    {
        await RequireCatalogueManagerAsync(actor, cancellationToken);
        ValidateAudit(command.SourceSummary, command.EditorialRationale);

        var product = await db.Products.AsNoTracking()
            .SingleOrDefaultAsync(value => value.Id == productId, cancellationToken)
            ?? throw new KeyNotFoundException("The catalogue product was not found.");

        var submissionId = await db.CatalogueSubmissions
            .Where(value => value.PublishedProductId == productId)
            .OrderByDescending(value => value.UpdatedAtUtc)
            .Select(value => (Guid?)value.Id)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new CatalogueValidationException("image", "The canonical product does not have a published submission to associate the image with.");

        if (command.Role != CatalogueSubmissionImageRole.Other &&
            await db.CatalogueSubmissionImages.AnyAsync(value => value.ProductId == productId && value.Role == command.Role, cancellationToken))
            throw new CatalogueValidationException("role", "An image with this role already exists. Edit or remove the existing image first.");

        var image = new CatalogueSubmissionImage(
            submissionId,
            command.Role,
            storageKey,
            originalFileName,
            contentType,
            fileSizeBytes,
            command.SourceType,
            command.SourceUrl,
            command.SourceNotes,
            command.PermissionStatus,
            command.PermissionEvidence);
        image.PublishToProduct(productId);
        db.CatalogueSubmissionImages.Add(image);
        await db.SaveChangesAsync(cancellationToken);

        if (command.IsPrimary)
            await SetProductImagePrimaryAsync(actor, productId, image.Id, command.SourceSummary, command.SourceReferences, command.EditorialRationale, command.CorrelationId, cancellationToken);

        await AddAuditAsync(
            CatalogueAuditAction.ProductChanged,
            productId,
            actor,
            command,
            new[] { productId, image.Id },
            cancellationToken);

        return ToModeratorImage(image, productId);
    }

    public async Task<CatalogueModeratorProductImage> UpdateProductImageAsync(
        AuthenticatedUser actor,
        Guid productId,
        Guid imageId,
        UpdateCanonicalProductImageMetadata command,
        CancellationToken cancellationToken = default)
    {
        await RequireCatalogueManagerAsync(actor, cancellationToken);
        ValidateAudit(command.SourceSummary, command.EditorialRationale);

        var image = await db.CatalogueSubmissionImages
            .SingleOrDefaultAsync(value => value.ProductId == productId && value.Id == imageId, cancellationToken)
            ?? throw new KeyNotFoundException("The product image was not found.");

        if (command.Role != CatalogueSubmissionImageRole.Other &&
            await db.CatalogueSubmissionImages.AnyAsync(value => value.ProductId == productId && value.Id != imageId && value.Role == command.Role, cancellationToken))
            throw new CatalogueValidationException("role", "An image with this role already exists.");

        image.UpdateRole(command.Role);
        image.UpdateMetadata(command.SourceType, command.SourceUrl, command.SourceNotes, command.PermissionStatus, command.PermissionEvidence);
        await db.SaveChangesAsync(cancellationToken);

        await AddAuditAsync(
            CatalogueAuditAction.ProductChanged,
            productId,
            actor,
            command,
            new[] { productId, image.Id },
            cancellationToken);

        return ToModeratorImage(image, productId);
    }

    public async Task RemoveProductImageAsync(
        AuthenticatedUser actor,
        Guid productId,
        Guid imageId,
        string sourceSummary,
        IReadOnlyList<string> sourceReferences,
        string editorialRationale,
        string? correlationId,
        CancellationToken cancellationToken = default)
    {
        await RequireCatalogueManagerAsync(actor, cancellationToken);
        ValidateAudit(sourceSummary, editorialRationale);

        var image = await db.CatalogueSubmissionImages
            .SingleOrDefaultAsync(value => value.ProductId == productId && value.Id == imageId, cancellationToken)
            ?? throw new KeyNotFoundException("The product image was not found.");

        var storageKey = image.StorageKey;
        db.CatalogueSubmissionImages.Remove(image);
        await db.SaveChangesAsync(cancellationToken);
        await imageStorage.DeleteAsync(storageKey, cancellationToken);

        await AddAuditAsync(
            CatalogueAuditAction.ProductChanged,
            productId,
            actor,
            new { Action = "RemoveImage", ImageId = imageId, SourceSummary = sourceSummary, SourceReferences = sourceReferences, EditorialRationale = editorialRationale, CorrelationId = correlationId },
            new[] { productId, imageId },
            cancellationToken,
            sourceSummary,
            sourceReferences,
            editorialRationale,
            correlationId);
    }

    public async Task SetProductImagePrimaryAsync(
        AuthenticatedUser actor,
        Guid productId,
        Guid imageId,
        string sourceSummary,
        IReadOnlyList<string> sourceReferences,
        string editorialRationale,
        string? correlationId,
        CancellationToken cancellationToken = default)
    {
        await RequireCatalogueManagerAsync(actor, cancellationToken);
        ValidateAudit(sourceSummary, editorialRationale);

        var images = await db.CatalogueSubmissionImages
            .Where(value => value.ProductId == productId)
            .ToListAsync(cancellationToken);
        var selected = images.SingleOrDefault(value => value.Id == imageId)
            ?? throw new KeyNotFoundException("The product image was not found.");

        foreach (var image in images)
            image.SetPrimary(image.Id == selected.Id);

        await db.SaveChangesAsync(cancellationToken);

        await AddAuditAsync(
            CatalogueAuditAction.ProductChanged,
            productId,
            actor,
            new { Action = "SetPrimaryImage", ImageId = imageId, SourceSummary = sourceSummary, SourceReferences = sourceReferences, EditorialRationale = editorialRationale, CorrelationId = correlationId },
            new[] { productId, imageId },
            cancellationToken,
            sourceSummary,
            sourceReferences,
            editorialRationale,
            correlationId);
    }

    private static CatalogueModeratorProductImage ToModeratorImage(CatalogueSubmissionImage image, Guid productId) =>
        new(
            image.Id,
            image.Role,
            image.IsPrimary,
            image.Visibility,
            image.SourceType,
            image.SourceUrl,
            image.SourceNotes,
            image.PermissionStatus,
            image.PermissionEvidence,
            image.OriginalFileName,
            image.FileSizeBytes,
            $"/api/v1/products/{productId}/moderator-images/{image.Id}");

    public async Task<CanonicalProductReceipt> CreateProductAsync(
        AuthenticatedUser actor,
        CreateCanonicalProduct command,
        CancellationToken cancellationToken = default)
    {
        Validate(command);

        await using var transaction =
            await db.Database.BeginTransactionAsync(cancellationToken);

        var manufacturerExists = await db.Manufacturers.AnyAsync(
            manufacturer => manufacturer.Id == command.ManufacturerId,
            cancellationToken);

        if (!manufacturerExists)
            throw new CatalogueValidationException(
                "manufacturerId",
                "The manufacturer was not found.");

        if (command.BrandId is { } brandId &&
            !await db.Brands.AnyAsync(
                brand => brand.Id == brandId &&
                         brand.ManufacturerId == command.ManufacturerId,
                cancellationToken))
            throw new CatalogueValidationException(
                "brandId",
                "The brand was not found for the selected manufacturer.");

        if (await db.Products.AnyAsync(
                product => product.Slug == command.ProductSlug,
                cancellationToken))
            throw new CatalogueValidationException(
                "productSlug",
                "A product with this slug already exists.");

        var product = new Product(
            command.ManufacturerId,
            command.BrandId,
            command.ProductName.Trim(),
            command.ProductSlug,
            command.ProductType,
            command.Status,
            command.ProductFamily,
            command.Description,
            command.OfficialWebsiteUrl,
            command.DescriptionVisibility);

        db.Products.Add(product);

        var createdVariantIds = new List<Guid>();
        var createdEntityIds = new List<Guid> { product.Id };
        var firstGtin = default(string);

        foreach (var variantCommand in command.Variants)
        {
            var variant = new ProductVariant(
                product.Id,
                variantCommand.Name.Trim(),
                variantCommand.BackingType,
                variantCommand.FastenerType,
                variantCommand.Appearance,
                variantCommand.PrimaryColour,
                variantCommand.HasWetnessIndicator,
                variantCommand.HasStandingLeakGuards,
                variantCommand.WaistbandStyle,
                variantCommand.Fragrance,
                variantCommand.IsLatexFree,
                variantCommand.DesignedFor,
                variantCommand.FastenerCount,
                variantCommand.ConstructionNotes);

            db.ProductVariants.Add(variant);
            createdVariantIds.Add(variant.Id);
            createdEntityIds.Add(variant.Id);

            foreach (var sizeCommand in variantCommand.Sizes!)
            {
                var size = new SizeVariant(
                    variant.Id,
                    sizeCommand.ManufacturerSize.Trim(),
                    sizeCommand.WaistMinimumCm,
                    sizeCommand.WaistMaximumCm,
                    sizeCommand.HipMinimumCm,
                    sizeCommand.HipMaximumCm,
                    sizeCommand.ManufacturerStatedAbsorbencyMl,
                    sizeCommand.FitMeasurementBasis,
                    sizeCommand.AbsorbencyBasisMethod,
                    sizeCommand.AbsorbencySource,
                    sizeCommand.LengthMm,
                    sizeCommand.WidthMm,
                    sizeCommand.WeightGrams);

                var pack = new PackType(
                    size.Id,
                    sizeCommand.ManufacturerPackQuantity,
                    sizeCommand.PackagingType);

                db.AddRange(size, pack);

                createdEntityIds.Add(size.Id);
                createdEntityIds.Add(pack.Id);

                var gtin = string.IsNullOrWhiteSpace(sizeCommand.Gtin)
                    ? null
                    : NormaliseGtin(sizeCommand.Gtin);

                if (gtin is not null)
                {
                    if (await db.ProductIdentifiers.AnyAsync(
                            identifier =>
                                identifier.Type == IdentifierType.Gtin &&
                                identifier.Value == gtin,
                            cancellationToken))
                        throw new CatalogueValidationException(
                            "gtin",
                            $"GTIN {gtin} is already assigned to a pack type.");

                    var identifier = new ProductIdentifier(
                        pack.Id,
                        IdentifierType.Gtin,
                        gtin);

                    db.ProductIdentifiers.Add(identifier);
                    createdEntityIds.Add(identifier.Id);
                    firstGtin ??= gtin;
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        var audit = new CatalogueAuditRecord(
            CatalogueAuditAction.ProductCreated,
            product.Id,
            actor.UserId,
            DateTimeOffset.UtcNow,
            JsonSerializer.Serialize(command),
            JsonSerializer.Serialize(createdEntityIds),
            command.SourceSummary.Trim(),
            JsonSerializer.Serialize(
                command.SourceReferences.Select(reference => reference.Trim())),
            command.EditorialRationale.Trim(),
            command.CorrelationId?.Trim());

        db.CatalogueAuditRecords.Add(audit);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var firstVariantId = createdVariantIds[0];
        var firstSizeId = await db.SizeVariants
            .AsNoTracking()
            .Where(value => value.ProductVariantId == firstVariantId)
            .OrderBy(value => value.Id)
            .Select(value => value.Id)
            .FirstAsync(cancellationToken);

        var firstPackId = await db.PackTypes
            .AsNoTracking()
            .Where(value => value.SizeVariantId == firstSizeId)
            .OrderBy(value => value.Id)
            .Select(value => value.Id)
            .FirstAsync(cancellationToken);

        return new CanonicalProductReceipt(
            product.Id,
            firstVariantId,
            firstSizeId,
            firstPackId,
            audit.Id,
            firstGtin);
    }

    private static string NormaliseGtin(string value) =>
        value.Replace(" ", string.Empty).Replace("-", string.Empty);

    private static void Validate(UpdateCanonicalProductIdentity command)
    {
        Required(command.ProductName, "productName");
        Required(command.SourceSummary, "sourceSummary");
        Required(command.EditorialRationale, "editorialRationale");

        if (!Enum.IsDefined(command.ProductType))
            throw new CatalogueValidationException("productType", "The product type is invalid.");
    }

    private static void Validate(CreateCanonicalProduct command)
    {
        Required(command.ProductName, "productName");
        Required(command.ProductSlug, "productSlug");
        Required(command.SourceSummary, "sourceSummary");
        Required(command.EditorialRationale, "editorialRationale");

        if (command.Variants is null || command.Variants.Count == 0)
            throw new CatalogueValidationException(
                "variants",
                "At least one product variant is required.");

        if (command.Variants.Any(variant => string.IsNullOrWhiteSpace(variant.Name)))
            throw new CatalogueValidationException(
                "variants",
                "Every canonical product variant must have a display name.");

        if (command.Variants
            .GroupBy(variant => variant.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .Any(group => group.Count() > 1))
            throw new CatalogueValidationException(
                "variants",
                "Canonical product variant names must be unique.");

        if (command.Variants.Any(variant => variant.Sizes is null || variant.Sizes.Count == 0))
            throw new CatalogueValidationException(
                "variants",
                "Every canonical product variant must have at least one size variant.");

        if (!SlugPattern.IsMatch(command.ProductSlug))
            throw new CatalogueValidationException(
                "productSlug",
                "Product slug must contain lowercase letters, digits, and single hyphens only.");

        if (!Enum.IsDefined(command.ProductType) ||
            !Enum.IsDefined(command.Status) ||
            command.Variants.Any(variant => !Enum.IsDefined(variant.BackingType) ||
                !Enum.IsDefined(variant.FastenerType) ||
                !Enum.IsDefined(variant.WaistbandStyle) ||
                !Enum.IsDefined(variant.Fragrance)))
            throw new CatalogueValidationException(
                "catalogue",
                "One or more catalogue classifications are invalid.");

        var gtins = new HashSet<string>(StringComparer.Ordinal);
        foreach (var variant in command.Variants)
        {
            foreach (var size in variant.Sizes!)
            {
                Required(size.ManufacturerSize, "manufacturerSize");

                if (size.ManufacturerPackQuantity <= 0)
                    throw new CatalogueValidationException(
                        "manufacturerPackQuantity",
                        "Manufacturer pack quantity must be greater than zero.");

                if (!Enum.IsDefined(size.PackagingType))
                    throw new CatalogueValidationException(
                        "packagingType",
                        "Packaging type is invalid.");

                ValidateRange(size.WaistMinimumCm, size.WaistMaximumCm, "waist");
                ValidateRange(size.HipMinimumCm, size.HipMaximumCm, "hip");
                ValidateNonNegative(size.ManufacturerStatedAbsorbencyMl, "manufacturerStatedAbsorbencyMl");
                ValidateNonNegative(size.LengthMm, "lengthMm");
                ValidateNonNegative(size.WidthMm, "widthMm");
                ValidateNonNegative(size.WeightGrams, "weightGrams");

                if (!string.IsNullOrWhiteSpace(size.Gtin))
                {
                    var gtin = NormaliseGtin(size.Gtin);
                    if (!GtinPattern.IsMatch(gtin))
                        throw new CatalogueValidationException(
                            "gtin",
                            "GTIN must contain 8 to 14 digits.");

                    if (!gtins.Add(gtin))
                        throw new CatalogueValidationException(
                            "gtin",
                            "GTINs must be unique within the product.");
                }
            }
        }

        if (command.SourceReferences is null ||
            command.SourceReferences.Count == 0 ||
            command.SourceReferences.Any(string.IsNullOrWhiteSpace))
            throw new CatalogueValidationException(
                "sourceReferences",
                "At least one source reference is required.");
    }

    private static void ValidateRange(int? minimum, int? maximum, string name)
    {
        if (minimum is < 0 || maximum is < 0)
            throw new CatalogueValidationException(
                name,
                $"{name} measurements cannot be negative.");

        if (minimum.HasValue && maximum.HasValue && minimum > maximum)
            throw new CatalogueValidationException(
                name,
                $"{name} minimum cannot exceed maximum.");
    }

    private static void ValidateNonNegative(int? value, string name)
    {
        if (value is < 0)
            throw new CatalogueValidationException(
                name,
                $"{name} cannot be negative.");
    }

    private static void Required(string value, string field)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new CatalogueValidationException(field, "This value is required.");
    }
}

internal sealed class CanonicalCatalogueQueries(DiaperScoutDbContext db) : ICanonicalCatalogueQueries
{
    public async Task<CatalogueEntryOptions> GetEntryOptionsAsync(CancellationToken cancellationToken = default)
    {
        var manufacturers = await db.Manufacturers.AsNoTracking().OrderBy(value => value.Name)
            .Select(value => new CatalogueManufacturerOption(value.Id, value.Name)).ToListAsync(cancellationToken);
        var brands = await db.Brands.AsNoTracking().OrderBy(value => value.Name)
            .Select(value => new CatalogueBrandOption(value.Id, value.ManufacturerId, value.Name)).ToListAsync(cancellationToken);
        return new CatalogueEntryOptions(manufacturers, brands);
    }
}
