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

internal sealed class CanonicalCatalogue(DiaperScoutDbContext db, IEditorialAuthorisation editorialAuthorisation) : ICanonicalCatalogue
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
