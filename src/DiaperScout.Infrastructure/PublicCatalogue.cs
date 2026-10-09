using DiaperScout.Application;
using DiaperScout.Domain;
using Microsoft.EntityFrameworkCore;

namespace DiaperScout.Infrastructure;

internal sealed partial class AtlasQueries
{
    private sealed class PublicCatalogueRow
    {
        public Product Product { get; init; } = null!;
        public ProductVariant Variant { get; init; } = null!;
        public string ManufacturerName { get; init; } = null!;
        public string? BrandName { get; init; }
        public int VariantCount { get; init; }
        public DateTimeOffset? AddedAtUtc { get; init; }
    }

    private IQueryable<PublicCatalogueRow> FilterPublicVariants(IQueryable<PublicCatalogueRow> rows, CatalogueProductFilters filters, string? excluded = null)
    {
        if (excluded != "manufacturer" && filters.ManufacturerIds.Count > 0)
            rows = rows.Where(r => filters.ManufacturerIds.Contains(r.Product.ManufacturerId));
        if (excluded != "brand" && filters.BrandIds.Count > 0)
            rows = rows.Where(r => r.Product.BrandId.HasValue && filters.BrandIds.Contains(r.Product.BrandId.Value));
        if (excluded != "productType" && filters.ProductTypes.Count > 0)
            rows = rows.Where(r => filters.ProductTypes.Contains(r.Product.ProductType));
        if (excluded != "size" && filters.Sizes.Count > 0)
            rows = rows.Where(r => r.Variant.Sizes.Any(s => filters.Sizes.Contains(s.ManufacturerSize)));
        if (excluded != "backing" && filters.Backings.Count > 0)
            rows = rows.Where(r => filters.Backings.Contains(r.Variant.BackingType));
        if (excluded != "packaging" && filters.PackagingTypes.Count > 0)
            rows = rows.Where(r => r.Variant.Sizes.Any(s => s.PackTypes.Any(p => filters.PackagingTypes.Contains(p.PackagingType))));
        return rows;
    }

    private async Task<CatalogueProductSearch> SearchPublicCatalogueAsync(string? query, CatalogueProductFilters filters,
        string sort, int limit, int offset, CancellationToken cancellationToken)
    {
        // Canonical variants have no separate publication status: eligibility follows their current Product.
        var rows = from product in db.Products.AsNoTracking()
                   join variant in db.ProductVariants.AsNoTracking() on product.Id equals variant.ProductId
                   join manufacturer in db.Manufacturers.AsNoTracking() on product.ManufacturerId equals manufacturer.Id
                   join brand in db.Brands.AsNoTracking() on product.BrandId equals brand.Id into brands
                   from brand in brands.DefaultIfEmpty()
                   where product.Status == ProductStatus.Current
                   select new PublicCatalogueRow { Product = product, Variant = variant, ManufacturerName = manufacturer.Name, BrandName = brand == null ? null : brand.Name, VariantCount = product.Variants.Count,
                       AddedAtUtc = db.CatalogueAuditRecords.Where(a => a.ProductId == product.Id && a.Action == CatalogueAuditAction.ProductCreated)
                           .Select(a => (DateTimeOffset?)a.OccurredAtUtc).Min() };
        if (!string.IsNullOrWhiteSpace(query))
        {
            var pattern = $"%{query.Trim()}%";
            rows = rows.Where(r => EF.Functions.ILike(r.Product.Name, pattern)
                || EF.Functions.ILike(r.Product.Slug, pattern)
                || EF.Functions.ILike(r.Variant.Name, pattern)
                || EF.Functions.ILike(r.ManufacturerName, pattern)
                || (r.BrandName != null && EF.Functions.ILike(r.BrandName, pattern))
                || EF.Functions.ILike((r.BrandName ?? "") + " " + r.Product.Name + " " + r.Variant.Name, pattern));
        }
        var filtered = FilterPublicVariants(rows, filters);
        var count = await filtered.CountAsync(cancellationToken);
        var ordered = sort.Trim().ToLowerInvariant() switch
        {
            "manufacturer" => filtered.OrderBy(r => r.ManufacturerName).ThenBy(r => r.Product.Name).ThenBy(r => r.Variant.Name),
            // Undated legacy products remain browseable, after products with creation evidence.
            "newest" => filtered.OrderByDescending(r => r.AddedAtUtc.HasValue).ThenByDescending(r => r.AddedAtUtc)
                .ThenBy(r => r.Product.Id).ThenBy(r => r.Variant.Name),
            _ => filtered.OrderBy(r => r.BrandName).ThenBy(r => r.Product.Name).ThenBy(r => r.Variant.Name)
        };
        var page = await ordered.ThenBy(r => r.Variant.Id).Skip(Math.Max(0, offset)).Take(Math.Clamp(limit, 1, 50)).ToListAsync(cancellationToken);
        var ids = page.Select(r => r.Variant.Id).ToArray();
        var sizes = await db.SizeVariants.AsNoTracking().Where(s => ids.Contains(s.ProductVariantId))
            .Select(s => new { s.ProductVariantId, s.ManufacturerSize, Packaging = s.PackTypes.Select(p => p.PackagingType).ToArray() }).ToListAsync(cancellationToken);
        var productIds = page.Select(r => r.Product.Id).Distinct().ToArray();
        var images = await db.CatalogueSubmissionImages.AsNoTracking()
            .Where(i => i.ProductId.HasValue && productIds.Contains(i.ProductId.Value) && i.Visibility == CatalogueContentVisibility.Public)
            .OrderByDescending(i => i.IsPrimary).ThenBy(i => i.Role).ThenBy(i => i.CreatedAtUtc)
            .Select(i => new { i.ProductId, i.Id }).ToListAsync(cancellationToken);
        var destinations = await (from listing in db.RetailerProductListings.AsNoTracking()
                                  join pack in db.PackTypes on listing.PackTypeId equals pack.Id
                                  join size in db.SizeVariants on pack.SizeVariantId equals size.Id
                                  join retailer in db.Retailers on listing.RetailerId equals retailer.Id
                                  where ids.Contains(size.ProductVariantId) && listing.Status == RetailerProductDiscoveryStatus.Verified && retailer.Status == RetailerStatus.Verified
                                  select new { size.ProductVariantId, listing.Id, RetailerName = retailer.Name, listing.ListingUrl }).ToListAsync(cancellationToken);
        var results = page.Select(r =>
        {
            var image = images.FirstOrDefault(i => i.ProductId == r.Product.Id);
            var variantSizes = sizes.Where(s => s.ProductVariantId == r.Variant.Id).ToArray();
            return new CatalogueProductListItem(r.Product.Id,
                PublicProductIdentity.DisplayName(r.BrandName, r.Product.Name, r.Variant.Name, r.VariantCount), r.Product.Slug,
                r.Product.ProductType, r.Product.Status, r.ManufacturerName, r.BrandName, 1,
                image == null ? null : $"/api/v1/products/{r.Product.Id}/images/{image.Id}",
                variantSizes.Select(s => s.ManufacturerSize).Distinct().Order().ToArray(),
                r.Variant.BackingType == BackingType.Unknown ? [] : [r.Variant.BackingType],
                variantSizes.SelectMany(s => s.Packaging).Distinct().Order().ToArray(),
                destinations.Where(d => d.ProductVariantId == r.Variant.Id).Select(d => new CatalogueRetailDestination(d.Id, d.RetailerName, d.ListingUrl)).ToArray(), r.Variant.Id);
        }).ToArray();

        var facets = new List<CatalogueFacet>();
        facets.Add(new("manufacturer", "Manufacturer", await FilterPublicVariants(rows, filters, "manufacturer")
            .GroupBy(r => new { r.Product.ManufacturerId, r.ManufacturerName }).OrderBy(g => g.Key.ManufacturerName)
            .Select(g => new CatalogueFacetOption(g.Key.ManufacturerId.ToString(), g.Key.ManufacturerName, g.Count())).ToListAsync(cancellationToken)));
        facets.Add(new("brand", "Brand", await FilterPublicVariants(rows, filters, "brand").Where(r => r.Product.BrandId.HasValue)
            .GroupBy(r => new { r.Product.BrandId, r.BrandName }).OrderBy(g => g.Key.BrandName)
            .Select(g => new CatalogueFacetOption(g.Key.BrandId!.Value.ToString(), g.Key.BrandName!, g.Count())).ToListAsync(cancellationToken)));
        facets.Add(new("productType", "Product type", await FilterPublicVariants(rows, filters, "productType")
            .GroupBy(r => r.Product.ProductType).OrderBy(g => g.Key)
            .Select(g => new CatalogueFacetOption(g.Key.ToString(), g.Key.ToString(), g.Count())).ToListAsync(cancellationToken)));
        facets.Add(new("backing", "Backing", await FilterPublicVariants(rows, filters, "backing").Where(r => r.Variant.BackingType != BackingType.Unknown)
            .GroupBy(r => r.Variant.BackingType).OrderBy(g => g.Key)
            .Select(g => new CatalogueFacetOption(g.Key.ToString(), g.Key.ToString(), g.Count())).ToListAsync(cancellationToken)));
        facets.Add(new("size", "Size", await (from r in FilterPublicVariants(rows, filters, "size")
            from size in r.Variant.Sizes group r by size.ManufacturerSize into g orderby g.Key
            select new CatalogueFacetOption(g.Key, g.Key, g.Select(r => r.Variant.Id).Distinct().Count())).ToListAsync(cancellationToken)));
        facets.Add(new("packaging", "Packaging", await (from r in FilterPublicVariants(rows, filters, "packaging")
            from size in r.Variant.Sizes from pack in size.PackTypes group r by pack.PackagingType into g orderby g.Key
            select new CatalogueFacetOption(g.Key.ToString(), g.Key.ToString(), g.Select(r => r.Variant.Id).Distinct().Count())).ToListAsync(cancellationToken)));
        return new(results, count, facets.Where(f => f.Options.Count > 0).ToArray());
    }
}
