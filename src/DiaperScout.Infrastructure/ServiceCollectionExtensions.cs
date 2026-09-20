using DiaperScout.Application;
using DiaperScout.Domain;
using DiaperScout.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace DiaperScout.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDiaperScoutInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("diaperscout")
            ?? throw new InvalidOperationException("Connection string 'diaperscout' is required.");

        services.AddDbContext<DiaperScoutDbContext>(options => options.UseNpgsql(connectionString));
        services.AddHttpContextAccessor();

        services.AddScoped<IAtlasQueries, AtlasQueries>();
        services.AddScoped<IObservationSubmissions, ObservationSubmissions>();
        services.AddScoped<ICatalogueSubmissions, CatalogueSubmissions>();
        services.AddSingleton<ICatalogueSubmissionImageStorage, CatalogueSubmissionImageStorage>();
        services.AddScoped<ICatalogueRetailQueries, CatalogueRetailQueries>();
        services.AddScoped<ICurrentExplorer, CurrentExplorer>();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<IEditorialAuthorisation, EditorialAuthorisation>();
        services.AddScoped<IPrivilegedRoleAssignments, PrivilegedRoleAssignments>();
        services.AddScoped<ICanonicalCatalogue, CanonicalCatalogue>();
        services.AddScoped<ICanonicalCatalogueQueries, CanonicalCatalogueQueries>();

        return services;
    }
}

public sealed class CurrentExplorer(DiaperScoutDbContext db, IHttpContextAccessor httpContextAccessor) : ICurrentExplorer
{
    public async Task<ExplorerIdentity?> GetAsync(CancellationToken cancellationToken = default)
    {
        var principal = httpContextAccessor.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true) return null;
        var subject = principal.FindFirstValue("sub") ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(subject)) return null;
        return await (from user in db.Users.AsNoTracking()
                      join explorer in db.ExplorerProfiles.AsNoTracking() on user.Id equals explorer.UserId
                      where user.Subject == subject && user.Status == UserAccountStatus.Active
                      select new ExplorerIdentity(user.Id, explorer.Id, user.Subject, explorer.DisplayName))
            .SingleOrDefaultAsync(cancellationToken);
    }
}

internal sealed class AtlasQueries(DiaperScoutDbContext db, ICatalogueSubmissionImageStorage imageStorage, IEditorialAuthorisation editorialAuthorisation) : IAtlasQueries
{
    public async Task<ProductSummary?> GetProductBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        await db.Products.AsNoTracking()
            .Where(p => p.Slug == slug)
            .Select(p => new ProductSummary(p.Id, p.Name, p.Slug, p.ProductType, p.Status))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<ProductIdentification?> GetProductByGtinAsync(string gtin, CancellationToken cancellationToken = default) =>
        await (from identifier in db.ProductIdentifiers.AsNoTracking()
               join pack in db.PackTypes on identifier.PackTypeId equals pack.Id
               join size in db.SizeVariants on pack.SizeVariantId equals size.Id
               join variant in db.ProductVariants on size.ProductVariantId equals variant.Id
               join product in db.Products on variant.ProductId equals product.Id
               where identifier.Type == IdentifierType.Gtin && identifier.Value == gtin
               select new ProductIdentification(new ProductSummary(product.Id, product.Name, product.Slug, product.ProductType, product.Status), variant.Id, variant.Name, size.Id, size.ManufacturerSize, pack.Id, pack.QuantityPerPack, pack.PackagingType, identifier.Value))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<CatalogueProductListItem>> SearchProductsAsync(string? query, int limit, CancellationToken cancellationToken = default) =>
        (await SearchCatalogueAsync(query, new CatalogueProductFilters([], [], [], [], [], []), "relevance", limit, 0, cancellationToken)).Products;

    public Task<CatalogueProductSearch> SearchCatalogueAsync(
        string? query,
        CatalogueProductFilters filters,
        string sort,
        int limit,
        int offset = 0,
        CancellationToken cancellationToken = default) =>
        SearchCatalogueCoreAsync(query, filters, [ProductStatus.Current], sort, limit, offset, cancellationToken);

    public Task<CatalogueProductSearch> SearchCatalogueManagementAsync(
        string? query,
        CatalogueProductManagementFilters filters,
        string sort,
        int limit,
        int offset = 0,
        CancellationToken cancellationToken = default) =>
        SearchCatalogueCoreAsync(
            query,
            new CatalogueProductFilters(filters.ManufacturerIds, [], filters.ProductTypes, [], [], []),
            filters.Statuses.Count == 0 ? Enum.GetValues<ProductStatus>() : filters.Statuses,
            sort,
            limit,
            offset,
            cancellationToken);

    private async Task<CatalogueProductSearch> SearchCatalogueCoreAsync(
        string? query,
        CatalogueProductFilters filters,
        IReadOnlyList<ProductStatus> statuses,
        string sort,
        int limit,
        int offset,
        CancellationToken cancellationToken)
    {
        limit = Math.Clamp(limit, 1, 50);
        offset = Math.Max(offset, 0);
        var baseQuery = db.Products.AsNoTracking().Where(p => statuses.Contains(p.Status));

        if (!string.IsNullOrWhiteSpace(query))
        {
            var pattern = $"%{query.Trim()}%";
            baseQuery = baseQuery.Where(product =>
                EF.Functions.ILike(product.Name, pattern) ||
                EF.Functions.ILike(product.Slug, pattern) ||
                db.Manufacturers.Any(manufacturer => manufacturer.Id == product.ManufacturerId && EF.Functions.ILike(manufacturer.Name, pattern)) ||
                (product.BrandId.HasValue && db.Brands.Any(brand => brand.Id == product.BrandId.Value && EF.Functions.ILike(brand.Name, pattern))));
        }

        var filteredQuery = ApplyFilters(baseQuery, filters);
        var totalCount = await filteredQuery.CountAsync(cancellationToken);

        var orderedQuery = sort.Trim().ToLowerInvariant() switch
        {
            "manufacturer" => filteredQuery.OrderBy(p => p.ManufacturerId).ThenBy(p => p.Name),
            "newest" => filteredQuery.OrderByDescending(p => p.Id),
            _ => filteredQuery.OrderBy(p => p.Name)
        };

        var products = await (from product in orderedQuery.Skip(offset).Take(limit)
                              join manufacturer in db.Manufacturers.AsNoTracking() on product.ManufacturerId equals manufacturer.Id
                              join brand in db.Brands.AsNoTracking() on product.BrandId equals brand.Id into brandJoin
                              from brand in brandJoin.DefaultIfEmpty()
                              select new { product.Id, product.Name, product.Slug, product.ProductType, product.Status, ManufacturerName = manufacturer.Name, BrandName = brand == null ? null : brand.Name })
            .ToListAsync(cancellationToken);

        var productIds = products.Select(p => p.Id).ToArray();
        var variants = await db.ProductVariants.AsNoTracking()
            .Where(v => productIds.Contains(v.ProductId))
            .Select(v => new { v.ProductId, v.BackingType, Sizes = v.Sizes.Select(s => new { s.ManufacturerSize, PackagingTypes = s.PackTypes.Select(p => p.PackagingType) }) })
            .ToListAsync(cancellationToken);

        var images = await db.CatalogueSubmissionImages.AsNoTracking()
            .Where(image => image.ProductId.HasValue && productIds.Contains(image.ProductId.Value) && image.Visibility == CatalogueContentVisibility.Public)
            .OrderBy(image => image.Role)
            .ThenBy(image => image.CreatedAtUtc)
            .Select(image => new { ProductId = image.ProductId!.Value, ImageId = image.Id })
            .ToListAsync(cancellationToken);

        var retailDestinations = await (from submission in db.CatalogueSubmissions.AsNoTracking()
                                        join destination in db.CatalogueSubmissionRetailDestinations.AsNoTracking() on submission.Id equals destination.SubmissionId
                                        join retailer in db.Retailers.AsNoTracking() on destination.RetailerId equals retailer.Id
                                        where submission.Status == CatalogueSubmissionStatus.Published
                                              && submission.PublishedProductId != null
                                              && productIds.Contains(submission.PublishedProductId.Value)
                                        select new { ProductId = submission.PublishedProductId!.Value, destination.Id, RetailerName = retailer.Name, destination.ListingUrl })
            .ToListAsync(cancellationToken);

        var productResults = products.Select(product =>
        {
            var productVariants = variants.Where(v => v.ProductId == product.Id).ToList();
            var image = images.FirstOrDefault(value => value.ProductId == product.Id);
            return new CatalogueProductListItem(
                product.Id,
                product.Name,
                product.Slug,
                product.ProductType,
                product.Status,
                product.ManufacturerName,
                product.BrandName,
                productVariants.Count,
                image is null ? null : $"/api/v1/products/{product.Id}/images/{image.ImageId}",
                productVariants.SelectMany(v => v.Sizes).Select(s => s.ManufacturerSize).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(s => s).ToArray(),
                productVariants.Select(v => v.BackingType).Where(v => v != BackingType.Unknown).Distinct().OrderBy(v => v).ToArray(),
                productVariants.SelectMany(v => v.Sizes).SelectMany(s => s.PackagingTypes).Distinct().OrderBy(v => v).ToArray(),
                retailDestinations.Where(d => d.ProductId == product.Id).Select(d => new CatalogueRetailDestination(d.Id, d.RetailerName, d.ListingUrl)).ToArray());
        }).ToArray();

        var facets = new List<CatalogueFacet>
        {
            await BuildManufacturerFacetAsync(baseQuery, filters, cancellationToken),
            await BuildBrandFacetAsync(baseQuery, filters, cancellationToken),
            await BuildProductTypeFacetAsync(baseQuery, filters, cancellationToken),
            await BuildSizeFacetAsync(baseQuery, filters, cancellationToken),
            await BuildBackingFacetAsync(baseQuery, filters, cancellationToken),
            await BuildPackagingFacetAsync(baseQuery, filters, cancellationToken)
        };

        return new CatalogueProductSearch(productResults, totalCount, facets.Where(f => f.Options.Count > 0).ToArray());
    }

    private static IQueryable<Product> ApplyFilters(IQueryable<Product> query, CatalogueProductFilters filters, string? excludedFacet = null)
    {
        if (excludedFacet != "manufacturer" && filters.ManufacturerIds.Count > 0)
            query = query.Where(p => filters.ManufacturerIds.Contains(p.ManufacturerId));

        if (excludedFacet != "brand" && filters.BrandIds.Count > 0)
            query = query.Where(p => p.BrandId.HasValue && filters.BrandIds.Contains(p.BrandId.Value));

        if (excludedFacet != "productType" && filters.ProductTypes.Count > 0)
            query = query.Where(p => filters.ProductTypes.Contains(p.ProductType));

        if (excludedFacet != "size" && filters.Sizes.Count > 0)
            query = query.Where(p => p.Variants.Any(v => v.Sizes.Any(s => filters.Sizes.Contains(s.ManufacturerSize))));

        if (excludedFacet != "backing" && filters.Backings.Count > 0)
            query = query.Where(p => p.Variants.Any(v => filters.Backings.Contains(v.BackingType)));

        if (excludedFacet != "packaging" && filters.PackagingTypes.Count > 0)
            query = query.Where(p => p.Variants.Any(v => v.Sizes.Any(s => s.PackTypes.Any(pack => filters.PackagingTypes.Contains(pack.PackagingType)))));

        return query;
    }

    private async Task<CatalogueFacet> BuildManufacturerFacetAsync(IQueryable<Product> baseQuery, CatalogueProductFilters filters, CancellationToken cancellationToken)
    {
        var rows = await (from product in ApplyFilters(baseQuery, filters, "manufacturer")
                          join manufacturer in db.Manufacturers.AsNoTracking() on product.ManufacturerId equals manufacturer.Id
                          group product by new { manufacturer.Id, manufacturer.Name } into groupValue
                          orderby groupValue.Key.Name
                          select new CatalogueFacetOption(groupValue.Key.Id.ToString(), groupValue.Key.Name, groupValue.Count()))
            .ToListAsync(cancellationToken);
        return new CatalogueFacet("manufacturer", "Manufacturer", rows);
    }

    private async Task<CatalogueFacet> BuildBrandFacetAsync(IQueryable<Product> baseQuery, CatalogueProductFilters filters, CancellationToken cancellationToken)
    {
        var rows = await (from product in ApplyFilters(baseQuery, filters, "brand")
                          where product.BrandId.HasValue
                          join brand in db.Brands.AsNoTracking() on product.BrandId equals brand.Id
                          group product by new { brand.Id, brand.Name } into groupValue
                          orderby groupValue.Key.Name
                          select new CatalogueFacetOption(groupValue.Key.Id.ToString(), groupValue.Key.Name, groupValue.Count()))
            .ToListAsync(cancellationToken);
        return new CatalogueFacet("brand", "Brand", rows);
    }

    private async Task<CatalogueFacet> BuildProductTypeFacetAsync(
    IQueryable<Product> baseQuery,
    CatalogueProductFilters filters,
    CancellationToken cancellationToken)
{
    var rows = await ApplyFilters(baseQuery, filters, "productType")
        .GroupBy(p => p.ProductType)
        .Select(groupValue => new
        {
            Value = groupValue.Key,
            Count = groupValue.Count()
        })
        .OrderBy(value => value.Value)
        .ToListAsync(cancellationToken);

    var options = rows
        .Select(value => new CatalogueFacetOption(
            value.Value.ToString(),
            value.Value.ToString(),
            value.Count))
        .ToList();

    return new CatalogueFacet("productType", "Product type", options);
}

    private async Task<CatalogueFacet> BuildSizeFacetAsync(IQueryable<Product> baseQuery, CatalogueProductFilters filters, CancellationToken cancellationToken)
    {
        var rows = await (from product in ApplyFilters(baseQuery, filters, "size")
                          from variant in product.Variants
                          from size in variant.Sizes
                          group product by size.ManufacturerSize into groupValue
                          orderby groupValue.Key
                          select new CatalogueFacetOption(groupValue.Key, groupValue.Key, groupValue.Select(p => p.Id).Distinct().Count()))
            .ToListAsync(cancellationToken);
        return new CatalogueFacet("size", "Size", rows);
    }

    private async Task<CatalogueFacet> BuildBackingFacetAsync(IQueryable<Product> baseQuery, CatalogueProductFilters filters, CancellationToken cancellationToken)
    {
        var rows = await (from product in ApplyFilters(baseQuery, filters, "backing")
                          from variant in product.Variants
                          where variant.BackingType != BackingType.Unknown
                          group product by variant.BackingType into groupValue
                          orderby groupValue.Key
                          select new CatalogueFacetOption(groupValue.Key.ToString(), groupValue.Key.ToString(), groupValue.Select(p => p.Id).Distinct().Count()))
            .ToListAsync(cancellationToken);
        return new CatalogueFacet("backing", "Backing", rows);
    }

    private async Task<CatalogueFacet> BuildPackagingFacetAsync(IQueryable<Product> baseQuery, CatalogueProductFilters filters, CancellationToken cancellationToken)
    {
        var rows = await (from product in ApplyFilters(baseQuery, filters, "packaging")
                          from variant in product.Variants
                          from size in variant.Sizes
                          from pack in size.PackTypes
                          group product by pack.PackagingType into groupValue
                          orderby groupValue.Key
                          select new CatalogueFacetOption(groupValue.Key.ToString(), groupValue.Key.ToString(), groupValue.Select(p => p.Id).Distinct().Count()))
            .ToListAsync(cancellationToken);
        return new CatalogueFacet("packaging", "Packaging", rows);
    }

    public async Task<CatalogueProductDetails?> GetProductDetailsBySlugAsync(string slug, CancellationToken cancellationToken = default)
        => (await GetProductDetailsCoreAsync(slug, includeModeratorOnly: false, cancellationToken))?.PublicDetails;

    public async Task<CatalogueModeratorProductDetails?> GetProductDetailsForModeratorAsync(
        AuthenticatedUser actor,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        if (!await editorialAuthorisation.CanPublishAtlasAsync(actor, cancellationToken))
            throw new UnauthorizedAccessException();

        return (await GetProductDetailsCoreByIdAsync(productId, includeModeratorOnly: true, cancellationToken))?.ModeratorDetails;
    }

    public async Task<CatalogueProductManagementDetails?> GetProductManagementDetailsAsync(
        AuthenticatedUser actor,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        if (!await editorialAuthorisation.CanManageCatalogueAsync(actor, cancellationToken))
            throw new UnauthorizedAccessException();

        var product = await (from value in db.Products.AsNoTracking()
                             join manufacturer in db.Manufacturers.AsNoTracking() on value.ManufacturerId equals manufacturer.Id
                             join brand in db.Brands.AsNoTracking() on value.BrandId equals brand.Id into brandJoin
                             from brand in brandJoin.DefaultIfEmpty()
                             where value.Id == productId
                             select new
                             {
                                 value.Id, value.Name, value.Slug, value.ManufacturerId, value.BrandId,
                                 ManufacturerName = manufacturer.Name,
                                 BrandName = brand == null ? null : brand.Name,
                                 ProductFamily = value.Family, value.ProductType, value.Status, value.OfficialWebsiteUrl
                             })
            .SingleOrDefaultAsync(cancellationToken);

        if (product is null)
            return null;

        var variants = await db.ProductVariants.AsNoTracking()
            .Where(value => value.ProductId == product.Id)
            .OrderBy(value => value.Name)
            .ToListAsync(cancellationToken);

        var variantResults = new List<CatalogueProductVariant>();
        foreach (var variant in variants)
        {
            var sizes = await db.SizeVariants.AsNoTracking()
                .Where(value => value.ProductVariantId == variant.Id)
                .OrderBy(value => value.ManufacturerSize)
                .ToListAsync(cancellationToken);

            var sizeResults = new List<CatalogueProductSize>();
            foreach (var size in sizes)
            {
                var packs = await db.PackTypes.AsNoTracking()
                    .Where(value => value.SizeVariantId == size.Id)
                    .OrderBy(value => value.QuantityPerPack)
                    .ToListAsync(cancellationToken);

                var packResults = new List<CatalogueProductPack>();
                foreach (var pack in packs)
                {
                    var gtins = await db.ProductIdentifiers.AsNoTracking()
                        .Where(value => value.PackTypeId == pack.Id && value.Type == IdentifierType.Gtin)
                        .OrderBy(value => value.Value)
                        .Select(value => value.Value)
                        .ToListAsync(cancellationToken);
                    packResults.Add(new CatalogueProductPack(pack.Id, pack.QuantityPerPack, pack.PackagingType, gtins));
                }

                sizeResults.Add(new CatalogueProductSize(size.Id, size.ManufacturerSize, size.WaistMinimumCm, size.WaistMaximumCm, size.HipMinimumCm, size.HipMaximumCm, size.ManufacturerStatedAbsorbencyMl, size.FitMeasurementBasis, size.AbsorbencyBasisMethod, size.AbsorbencySource, size.LengthMm, size.WidthMm, size.WeightGrams, packResults));
            }

            variantResults.Add(new CatalogueProductVariant(variant.Id, variant.Name, variant.BackingType, sizeResults));
        }

        return new CatalogueProductManagementDetails(
            product.Id, product.Name, product.Slug, product.ManufacturerId, product.BrandId,
            product.ManufacturerName, product.BrandName, product.ProductFamily, product.ProductType,
            product.Status, product.OfficialWebsiteUrl, variantResults);
    }

    public async Task<CatalogueProductImageContent?> GetProductImageContentAsync(
        Guid productId,
        Guid imageId,
        bool moderatorOnly,
        CancellationToken cancellationToken = default)
    {
        var image = await db.CatalogueSubmissionImages
            .AsNoTracking()
            .SingleOrDefaultAsync(
                value => value.ProductId == productId &&
                         value.Id == imageId &&
                         (moderatorOnly || value.Visibility == CatalogueContentVisibility.Public),
                cancellationToken);

        if (image is null)
            return null;

        var stream = await imageStorage.OpenReadAsync(image.StorageKey, cancellationToken);
        return stream is null
            ? null
            : new CatalogueProductImageContent(stream, image.ContentType, image.OriginalFileName);
    }

    private async Task<(CatalogueProductDetails PublicDetails, CatalogueModeratorProductDetails ModeratorDetails)?> GetProductDetailsCoreByIdAsync(
        Guid productId,
        bool includeModeratorOnly,
        CancellationToken cancellationToken)
    {
        var slug = await db.Products.AsNoTracking()
            .Where(value => value.Id == productId)
            .Select(value => value.Slug)
            .SingleOrDefaultAsync(cancellationToken);

        return slug is null ? null : await GetProductDetailsCoreAsync(slug, includeModeratorOnly, cancellationToken);
    }

    private async Task<(CatalogueProductDetails PublicDetails, CatalogueModeratorProductDetails ModeratorDetails)?> GetProductDetailsCoreAsync(
        string slug,
        bool includeModeratorOnly,
        CancellationToken cancellationToken)
    {
        var product = await (from value in db.Products.AsNoTracking()
                             join manufacturer in db.Manufacturers.AsNoTracking() on value.ManufacturerId equals manufacturer.Id
                             join brand in db.Brands.AsNoTracking() on value.BrandId equals brand.Id into brandJoin
                             from brand in brandJoin.DefaultIfEmpty()
                             where value.Slug == slug
                             select new { value.Id, value.Name, value.Slug, value.ProductType, value.Status, ManufacturerName = manufacturer.Name, BrandName = brand == null ? null : brand.Name, value.Description, value.DescriptionVisibility, value.OfficialWebsiteUrl })
            .SingleOrDefaultAsync(cancellationToken);

        if (product is null)
            return null;

        var variants = await db.ProductVariants.AsNoTracking()
            .Where(v => v.ProductId == product.Id)
            .OrderBy(v => v.Name)
            .ToListAsync(cancellationToken);

        var variantResults = new List<CatalogueProductVariant>();
        foreach (var variant in variants)
        {
            var sizes = await db.SizeVariants.AsNoTracking()
                .Where(s => s.ProductVariantId == variant.Id)
                .OrderBy(s => s.ManufacturerSize)
                .ToListAsync(cancellationToken);

            var sizeResults = new List<CatalogueProductSize>();
            foreach (var size in sizes)
            {
                var packs = await db.PackTypes.AsNoTracking()
                    .Where(p => p.SizeVariantId == size.Id)
                    .OrderBy(p => p.QuantityPerPack)
                    .ToListAsync(cancellationToken);

                var packResults = new List<CatalogueProductPack>();
                foreach (var pack in packs)
                {
                    var gtins = await db.ProductIdentifiers.AsNoTracking()
                        .Where(i => i.PackTypeId == pack.Id && i.Type == IdentifierType.Gtin)
                        .OrderBy(i => i.Value)
                        .Select(i => i.Value)
                        .ToListAsync(cancellationToken);

                    packResults.Add(new CatalogueProductPack(pack.Id, pack.QuantityPerPack, pack.PackagingType, gtins));
                }

                sizeResults.Add(new CatalogueProductSize(size.Id, size.ManufacturerSize, size.WaistMinimumCm, size.WaistMaximumCm, size.HipMinimumCm, size.HipMaximumCm, size.ManufacturerStatedAbsorbencyMl, size.FitMeasurementBasis, size.AbsorbencyBasisMethod, size.AbsorbencySource, size.LengthMm, size.WidthMm, size.WeightGrams, packResults));
            }

            variantResults.Add(new CatalogueProductVariant(variant.Id, variant.Name, variant.BackingType, sizeResults));
        }

        var images = await db.CatalogueSubmissionImages
            .AsNoTracking()
            .Where(value => value.ProductId == product.Id && (includeModeratorOnly || value.Visibility == CatalogueContentVisibility.Public))
            .OrderBy(value => value.Role)
            .ThenBy(value => value.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var publicImages = images
            .Where(value => value.Visibility == CatalogueContentVisibility.Public)
            .Select(value => new CatalogueProductImage(
                value.Id,
                value.Role,
                $"/api/v1/products/{product.Id}/images/{value.Id}"))
            .ToList();

        var moderatorImages = images
            .Select(value => new CatalogueModeratorProductImage(
                value.Id,
                value.Role,
                value.Visibility,
                value.SourceType,
                value.SourceUrl,
                value.PermissionStatus,
                $"/api/v1/products/{product.Id}/moderator-images/{value.Id}"))
            .ToList();

        var publicDescription = product.DescriptionVisibility == CatalogueContentVisibility.Public
            ? product.Description
            : null;

        return (
            new CatalogueProductDetails(product.Id, product.Name, product.Slug, product.ProductType, product.Status, product.ManufacturerName, product.BrandName, publicDescription, product.DescriptionVisibility, product.OfficialWebsiteUrl, variantResults, publicImages),
            new CatalogueModeratorProductDetails(product.Id, product.Name, product.Slug, product.ProductType, product.Status, product.ManufacturerName, product.BrandName, product.Description, product.DescriptionVisibility, product.OfficialWebsiteUrl, variantResults, moderatorImages));
    }


}

internal sealed class ObservationSubmissions(DiaperScoutDbContext db) : IObservationSubmissions
{
    public async Task<ObservationReceipt> SubmitAsync(ExplorerIdentity explorer, ObservationSubmission submission, CancellationToken cancellationToken = default)
    {
        if (submission.ProductId is { } productId && !await db.Products.AnyAsync(product => product.Id == productId, cancellationToken)) throw new InvalidOperationException("Referenced product was not found.");
        if (submission.LocationId is { } locationId && !await db.Locations.AnyAsync(location => location.Id == locationId, cancellationToken)) throw new InvalidOperationException("Referenced location was not found.");
        var observation = new Observation(explorer.UserId, submission.Type, submission.ObservedAtUtc, submission.ProductId, submission.CandidateProductName, submission.LocationId, submission.Narrative);
        observation.Submit(); db.Observations.Add(observation); await db.SaveChangesAsync(cancellationToken);
        return new ObservationReceipt(observation.Id, observation.State, observation.CreatedAtUtc);
    }
}


internal sealed class CatalogueRetailQueries(DiaperScoutDbContext db) : ICatalogueRetailQueries
{
    public async Task<IReadOnlyList<CatalogueRetailerOption>> GetRetailersAsync(CancellationToken cancellationToken = default) =>
        await db.Retailers.AsNoTracking()
            .OrderBy(r => r.Name)
            .Select(r => new CatalogueRetailerOption(r.Id, r.Name))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CatalogueSubmissionRetailDestinationReceipt>> GetDestinationsAsync(
        Guid submissionId, CancellationToken cancellationToken = default) =>
        await (from destination in db.CatalogueSubmissionRetailDestinations.AsNoTracking()
               join retailer in db.Retailers.AsNoTracking() on destination.RetailerId equals retailer.Id
               where destination.SubmissionId == submissionId
               orderby retailer.Name, destination.AddedAtUtc
               select new CatalogueSubmissionRetailDestinationReceipt(
                   destination.Id, destination.SubmissionId, destination.RetailerId,
                   destination.ListingUrl, destination.Notes, destination.AddedAtUtc))
            .ToListAsync(cancellationToken);
}
