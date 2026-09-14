using DiaperScout.Domain;
using DiaperScout.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DiaperScout.Infrastructure;

/// <summary>
/// Adds the deliberately small, curated development catalogue. This is invoked only
/// by the Development API startup path; it is not EF seed data or a production import.
/// </summary>
public static class DevelopmentCatalogue
{
    private const string Gtin = "5060572900820";
    private const string Sku = "NLRM";

    public static async Task PopulateDevelopmentCatalogueAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DiaperScoutDbContext>();
        await db.Database.MigrateAsync(cancellationToken);

        await EnsureDevelopmentModeratorAsync(db, cancellationToken);

        var existingRecord = await FindRecordByGtinAsync(db, cancellationToken);

        if (existingRecord is not null)
        {
            ValidateExistingRecord(existingRecord);
            await EnsureSkuAsync(db, existingRecord.PackTypeId, cancellationToken);
            return;
        }

        var manufacturer = await GetOrCreateManufacturerAsync(db, cancellationToken);
        var brand = await GetOrCreateBrandAsync(db, manufacturer, cancellationToken);
        var product = await GetOrCreateProductAsync(db, manufacturer, brand, cancellationToken);
        var variant = await GetOrCreateVariantAsync(db, product, cancellationToken);
        var size = await GetOrCreateSizeAsync(db, variant, cancellationToken);
        var pack = await GetOrCreatePackAsync(db, size, cancellationToken);

        db.ProductIdentifiers.AddRange(
            new ProductIdentifier(pack.Id, IdentifierType.Gtin, Gtin),
            new ProductIdentifier(pack.Id, IdentifierType.Other, Sku));
        await db.SaveChangesAsync(cancellationToken);
    }

    private const string DevelopmentModeratorSubject = "development-moderator";

    private static async Task EnsureDevelopmentModeratorAsync(
        DiaperScoutDbContext db,
        CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(
            value => value.Subject == DevelopmentModeratorSubject,
            cancellationToken);

        if (user is null)
        {
            user = new User(DevelopmentModeratorSubject);
            db.Users.Add(user);
            await db.SaveChangesAsync(cancellationToken);
        }

        var assignmentExists = await db.PrivilegedRoleAssignments.AnyAsync(
            value => value.UserId == user.Id
                && value.Role == PrivilegedRole.Moderator
                && value.RevokedAtUtc == null,
            cancellationToken);

        if (!assignmentExists)
        {
            var now = DateTimeOffset.UtcNow;
            var assignment = new PrivilegedRoleAssignment(
                user.Id,
                PrivilegedRole.Moderator,
                user.Id,
                now);

            db.AddRange(
                assignment,
                new PrivilegedRoleAssignmentAudit(
                    user.Id,
                    user.Id,
                    PrivilegedRole.Moderator,
                    PrivilegedRoleAssignmentAction.Granted,
                    now));

            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task<CuratedRecord?> FindRecordByGtinAsync(DiaperScoutDbContext db, CancellationToken cancellationToken) =>
        await (from identifier in db.ProductIdentifiers
               join pack in db.PackTypes on identifier.PackTypeId equals pack.Id
               join size in db.SizeVariants on pack.SizeVariantId equals size.Id
               join variant in db.ProductVariants on size.ProductVariantId equals variant.Id
               join product in db.Products on variant.ProductId equals product.Id
               join brand in db.Brands on product.BrandId equals brand.Id
               join manufacturer in db.Manufacturers on product.ManufacturerId equals manufacturer.Id
               where identifier.Type == IdentifierType.Gtin && identifier.Value == Gtin
               select new CuratedRecord(
                   manufacturer.Name, manufacturer.Slug,
                   brand.Id, brand.Name, brand.Slug, brand.ManufacturerId,
                   product.Name, product.Slug, product.ManufacturerId, product.BrandId, product.ProductType,
                   variant.Name, variant.BackingType,
                   size.ManufacturerSize, size.WaistMinimumCm, size.WaistMaximumCm,
                   pack.Id, pack.QuantityPerPack, pack.PackagingType))
            .SingleOrDefaultAsync(cancellationToken);

    private static void ValidateExistingRecord(CuratedRecord record)
    {
        if (record.ManufacturerName != "Nappies R Us" || record.ManufacturerSlug != "nappies-r-us" ||
            record.BrandName != "NRU" || record.BrandSlug != "nru" || record.BrandManufacturerId != record.ProductManufacturerId || record.ProductBrandId != record.BrandId ||
            record.ProductName != "Little Rascals" || record.ProductSlug != "little-rascals" || record.ProductType != ProductType.Tape ||
            record.VariantName != "Little Rascals V2" || record.BackingType != BackingType.Plastic ||
            record.ManufacturerSize != "Medium" || record.WaistMinimumCm != 81 || record.WaistMaximumCm != 102 ||
            record.QuantityPerPack != 10 || record.PackagingType != PackagingType.Bag)
        {
            throw new InvalidOperationException($"The existing GTIN '{Gtin}' does not match the curated development catalogue record.");
        }
    }

    private static async Task<Manufacturer> GetOrCreateManufacturerAsync(DiaperScoutDbContext db, CancellationToken cancellationToken)
    {
        var value = await db.Manufacturers.SingleOrDefaultAsync(entity => entity.Slug == "nappies-r-us", cancellationToken);
        if (value is null) return db.Manufacturers.Add(new Manufacturer("Nappies R Us", "nappies-r-us")).Entity;
        if (value.Name != "Nappies R Us") throw Conflict("manufacturer", value.Slug);
        return value;
    }

    private static async Task<Brand> GetOrCreateBrandAsync(DiaperScoutDbContext db, Manufacturer manufacturer, CancellationToken cancellationToken)
    {
        var value = await db.Brands.SingleOrDefaultAsync(entity => entity.Slug == "nru", cancellationToken);
        if (value is null) return db.Brands.Add(new Brand(manufacturer.Id, "NRU", "nru")).Entity;
        if (value.Name != "NRU" || value.ManufacturerId != manufacturer.Id) throw Conflict("brand", value.Slug);
        return value;
    }

    private static async Task<Product> GetOrCreateProductAsync(DiaperScoutDbContext db, Manufacturer manufacturer, Brand brand, CancellationToken cancellationToken)
    {
        var value = await db.Products.SingleOrDefaultAsync(entity => entity.Slug == "little-rascals", cancellationToken);
        if (value is null) return db.Products.Add(new Product(manufacturer.Id, brand.Id, "Little Rascals", "little-rascals", ProductType.Tape)).Entity;
        if (value.Name != "Little Rascals" || value.ManufacturerId != manufacturer.Id || value.BrandId != brand.Id || value.ProductType != ProductType.Tape) throw Conflict("product", value.Slug);
        return value;
    }

    private static async Task<ProductVariant> GetOrCreateVariantAsync(DiaperScoutDbContext db, Product product, CancellationToken cancellationToken)
    {
        var value = await db.ProductVariants.SingleOrDefaultAsync(entity => entity.ProductId == product.Id && entity.Name == "Little Rascals V2", cancellationToken);
        if (value is null) return db.ProductVariants.Add(new ProductVariant(product.Id, "Little Rascals V2", BackingType.Plastic)).Entity;
        if (value.BackingType != BackingType.Plastic) throw Conflict("product variant", value.Name);
        return value;
    }

    private static async Task<SizeVariant> GetOrCreateSizeAsync(DiaperScoutDbContext db, ProductVariant variant, CancellationToken cancellationToken)
    {
        var value = await db.SizeVariants.SingleOrDefaultAsync(entity => entity.ProductVariantId == variant.Id && entity.ManufacturerSize == "Medium", cancellationToken);
        if (value is null) return db.SizeVariants.Add(new SizeVariant(variant.Id, "Medium", 81, 102)).Entity;
        if (value.WaistMinimumCm != 81 || value.WaistMaximumCm != 102) throw Conflict("size variant", value.ManufacturerSize);
        return value;
    }

    private static async Task<PackType> GetOrCreatePackAsync(DiaperScoutDbContext db, SizeVariant size, CancellationToken cancellationToken)
    {
        var value = await db.PackTypes.SingleOrDefaultAsync(entity => entity.SizeVariantId == size.Id && entity.QuantityPerPack == 10 && entity.PackagingType == PackagingType.Bag, cancellationToken);
        return value ?? db.PackTypes.Add(new PackType(size.Id, 10, PackagingType.Bag)).Entity;
    }

    private static async Task EnsureSkuAsync(DiaperScoutDbContext db, Guid packTypeId, CancellationToken cancellationToken)
    {
        var existing = await db.ProductIdentifiers.SingleOrDefaultAsync(identifier => identifier.Type == IdentifierType.Other && identifier.Value == Sku, cancellationToken);
        if (existing is null)
        {
            db.ProductIdentifiers.Add(new ProductIdentifier(packTypeId, IdentifierType.Other, Sku));
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        if (existing.PackTypeId != packTypeId)
            throw new InvalidOperationException($"The existing SKU '{Sku}' does not match the curated development catalogue record.");
    }

    private static InvalidOperationException Conflict(string entity, string identity) => new($"The existing {entity} '{identity}' conflicts with the curated development catalogue record.");

    private sealed record CuratedRecord(
        string ManufacturerName, string ManufacturerSlug,
        Guid BrandId, string BrandName, string BrandSlug, Guid BrandManufacturerId,
        string ProductName, string ProductSlug, Guid ProductManufacturerId, Guid? ProductBrandId, ProductType ProductType,
        string VariantName, BackingType BackingType,
        string ManufacturerSize, int? WaistMinimumCm, int? WaistMaximumCm,
        Guid PackTypeId, int QuantityPerPack, PackagingType PackagingType);
}
