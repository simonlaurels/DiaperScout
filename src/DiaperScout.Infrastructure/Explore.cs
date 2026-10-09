using DiaperScout.Application;
using DiaperScout.Domain;
using Microsoft.EntityFrameworkCore;

namespace DiaperScout.Infrastructure;

internal sealed partial class AtlasQueries
{
    public async Task<IReadOnlyList<RecentCatalogueProduct>> RecentProductsAsync(CancellationToken cancellationToken = default)
    {
        // ProductCreated is the existing canonical creation/publication audit, not a real-world release date.
        // Legacy products without trustworthy creation evidence are not labelled recently added.
        var created = db.CatalogueAuditRecords.AsNoTracking()
            .Where(a => a.Action == CatalogueAuditAction.ProductCreated)
            .GroupBy(a => a.ProductId).Select(g => new { ProductId = g.Key, Added = g.Min(a => a.OccurredAtUtc) });
        var rows = await (from product in db.Products.AsNoTracking()
                          join date in created on product.Id equals date.ProductId
                          join maker in db.Manufacturers.AsNoTracking() on product.ManufacturerId equals maker.Id
                          join brand in db.Brands.AsNoTracking() on product.BrandId equals brand.Id into brands
                          from brand in brands.DefaultIfEmpty()
                          where product.Status == ProductStatus.Current && product.Variants.Any()
                          orderby date.Added descending, product.Id
                          select new { product.Id, product.Name, product.Slug, BrandName = brand == null ? null : brand.Name,
                              ManufacturerName = maker.Name, date.Added }).Take(8).ToListAsync(cancellationToken);
        var ids = rows.Select(r => r.Id).ToArray();
        var variants = await db.ProductVariants.AsNoTracking().Where(v => ids.Contains(v.ProductId))
            .OrderBy(v => v.Name).ThenBy(v => v.Id).Select(v => new { v.ProductId, v.Id }).ToListAsync(cancellationToken);
        // Reuse public catalogue image eligibility and primary-image precedence.
        var images = await db.CatalogueSubmissionImages.AsNoTracking()
            .Where(i => i.ProductId.HasValue && ids.Contains(i.ProductId.Value) && i.Visibility == CatalogueContentVisibility.Public)
            .OrderByDescending(i => i.IsPrimary).ThenBy(i => i.Role).ThenBy(i => i.CreatedAtUtc)
            .Select(i => new { i.ProductId, i.Id }).ToListAsync(cancellationToken);
        return rows.Select(r => new RecentCatalogueProduct(r.Id, PublicProductIdentity.DisplayName(r.BrandName, r.Name), r.Slug, r.BrandName, r.ManufacturerName,
            variants.First(v => v.ProductId == r.Id).Id,
            images.FirstOrDefault(i => i.ProductId == r.Id) is { } image ? $"/api/v1/products/{r.Id}/images/{image.Id}" : null,
            r.Added)).ToArray();
    }
}
