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

internal sealed class CanonicalCatalogue(DiaperScoutDbContext db) : ICanonicalCatalogue
{
    private static readonly Regex SlugPattern = new("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.Compiled);
    private static readonly Regex GtinPattern = new("^[0-9]{8,14}$", RegexOptions.Compiled);

    public async Task<CanonicalProductReceipt> CreateProductAsync(
        AuthenticatedUser actor,
        CreateCanonicalProduct command,
        CancellationToken cancellationToken = default)
    {
        Validate(command);

        var gtin = string.IsNullOrWhiteSpace(command.Gtin)
            ? null
            : command.Gtin.Replace(" ", string.Empty).Replace("-", string.Empty);

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

        if (gtin is not null &&
            await db.ProductIdentifiers.AnyAsync(
                identifier =>
                    identifier.Type == IdentifierType.Gtin &&
                    identifier.Value == gtin,
                cancellationToken))
            throw new CatalogueValidationException(
                "gtin",
                "This GTIN is already assigned to a pack type.");

        var product = new Product(
            command.ManufacturerId,
            command.BrandId,
            command.ProductName.Trim(),
            command.ProductSlug,
            command.ProductType,
            command.Status);

        db.Products.Add(product);

        var createdVariantIds = new List<Guid>();
        var createdEntityIds = new List<Guid> { product.Id };

        foreach (var variantCommand in command.Variants)
        {
            var variant = new ProductVariant(
                product.Id,
                variantCommand.Name.Trim(),
                variantCommand.BackingType);

            var size = new SizeVariant(
                variant.Id,
                command.ManufacturerSize.Trim(),
                command.WaistMinimumCm,
                command.WaistMaximumCm);

            var pack = new PackType(
                size.Id,
                command.QuantityPerPack,
                command.PackagingType);

            db.AddRange(variant, size, pack);

            createdVariantIds.Add(variant.Id);
            createdEntityIds.Add(variant.Id);
            createdEntityIds.Add(size.Id);
            createdEntityIds.Add(pack.Id);

            // A submission-level GTIN is only valid for a single-variant
            // publication. Multi-variant submissions must capture GTINs at
            // their eventual size/pack level.
            if (gtin is not null)
            {
                var identifier = new ProductIdentifier(
                    pack.Id,
                    IdentifierType.Gtin,
                    gtin);

                db.ProductIdentifiers.Add(identifier);
                createdEntityIds.Add(identifier.Id);
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        var audit = new CatalogueAuditRecord(
            CatalogueAuditAction.ProductCreated,
            product.Id,
            actor.UserId,
            DateTimeOffset.UtcNow,
            JsonSerializer.Serialize(command with { Gtin = gtin ?? string.Empty }),
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
        var firstVariant = await db.ProductVariants
            .AsNoTracking()
            .SingleAsync(
                value => value.Id == firstVariantId,
                cancellationToken);

        var firstSizeId = await db.SizeVariants
            .AsNoTracking()
            .Where(value => value.ProductVariantId == firstVariantId)
            .Select(value => value.Id)
            .SingleAsync(cancellationToken);

        var firstPackId = await db.PackTypes
            .AsNoTracking()
            .Where(value => value.SizeVariantId == firstSizeId)
            .Select(value => value.Id)
            .SingleAsync(cancellationToken);

        return new CanonicalProductReceipt(
            product.Id,
            firstVariant.Id,
            firstSizeId,
            firstPackId,
            audit.Id,
            gtin);
    }

    private static void Validate(CreateCanonicalProduct command)
    {
        Required(command.ProductName, "productName");
        Required(command.ProductSlug, "productSlug");
        Required(command.ManufacturerSize, "manufacturerSize");
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

        if (!SlugPattern.IsMatch(command.ProductSlug))
            throw new CatalogueValidationException(
                "productSlug",
                "Product slug must contain lowercase letters, digits, and single hyphens only.");

        if (!string.IsNullOrWhiteSpace(command.Gtin) &&
            !GtinPattern.IsMatch(
                command.Gtin.Replace(" ", string.Empty).Replace("-", string.Empty)))
            throw new CatalogueValidationException(
                "gtin",
                "GTIN must contain 8 to 14 digits.");

        if (command.Variants.Count > 1 && !string.IsNullOrWhiteSpace(command.Gtin))
            throw new CatalogueValidationException(
                "gtin",
                "A single GTIN cannot be assigned to a multi-variant product.");

        if (!Enum.IsDefined(command.ProductType) ||
            !Enum.IsDefined(command.Status) ||
            !Enum.IsDefined(command.PackagingType) ||
            command.Variants.Any(variant => !Enum.IsDefined(variant.BackingType)))
            throw new CatalogueValidationException(
                "catalogue",
                "One or more catalogue classifications are invalid.");

        if (command.QuantityPerPack <= 0)
            throw new CatalogueValidationException(
                "quantityPerPack",
                "Quantity per pack must be greater than zero.");

        if (command.WaistMinimumCm is not null &&
            command.WaistMaximumCm is not null &&
            command.WaistMinimumCm > command.WaistMaximumCm)
            throw new CatalogueValidationException(
                "waist",
                "Waist minimum cannot exceed waist maximum.");

        if (command.SourceReferences is null ||
            command.SourceReferences.Count == 0 ||
            command.SourceReferences.Any(string.IsNullOrWhiteSpace))
            throw new CatalogueValidationException(
                "sourceReferences",
                "At least one source reference is required.");
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
