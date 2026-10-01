using DiaperScout.Application;
using DiaperScout.Domain;
using DiaperScout.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DiaperScout.Infrastructure;

internal sealed class CanonicalCommerce(DiaperScoutDbContext db, ICommercePluginOrchestrator plugins,
    IRetailerDiscovery listings) : ICanonicalCommerce
{
    public async Task<CanonicalCommerceDiscoveryRun> DiscoverListingsAsync(Guid packTypeId, CancellationToken cancellationToken = default)
    {
        var item = await GetItemAsync(packTypeId, cancellationToken);
        var batch = await plugins.DiscoverAsync(item, cancellationToken);
        var recorded = new List<RetailerProductListingItem>();
        var review = new List<CommerceDiscoveryReview>();
        var keys = new HashSet<(Guid, string)>();
        foreach (var observation in batch.Observations)
        {
            var candidate = observation.Value;
            string? problem = null;
            if (candidate.QuantityPerPack.HasValue && candidate.QuantityPerPack != item.QuantityPerPack)
                problem = "PackQuantityMismatch";
            else if (candidate.ObservedIdentifiers.Any(i => !item.Identifiers.Any(known => known.Type == i.Type && known.Value == i.Value)))
                problem = "IdentifierMismatch";
            if (problem is not null) { review.Add(new(observation.Provenance.PluginId, problem)); continue; }
            // Providers supply evidence. Only the application associates it with an existing canonical retailer.
            var retailerHost = Host(candidate.Retailer.WebsiteUrl);
            var retailers = await db.Retailers.AsNoTracking().Where(r => candidate.Retailer.CanonicalRetailerId == null
                || r.Id == candidate.Retailer.CanonicalRetailerId).ToListAsync(cancellationToken);
            var matches = retailers.Where(r => retailerHost is not null && Host(r.WebsiteUrl) == retailerHost).ToArray();
            if (matches.Length != 1)
            {
                review.Add(new(observation.Provenance.PluginId, "RetailerNotAssociated"));
                continue;
            }
            var retailer = matches[0];
            var url = new Uri(candidate.ListingUrl).ToString();
            if (!keys.Add((retailer.Id, url))) continue;
            // A discovery observation never rewrites human-verified provenance or promotes a listing to Verified.
            var existing = await db.RetailerProductListings.AsNoTracking().SingleOrDefaultAsync(l => l.PackTypeId == item.PackTypeId
                && l.RetailerId == retailer.Id && l.ListingUrl == url, cancellationToken);
            if (existing?.Status == RetailerProductDiscoveryStatus.Verified)
            {
                recorded.Add(new(existing.Id, existing.PackTypeId, retailer.Id, retailer.Name, retailer.Status,
                    existing.ListingUrl, existing.DiscoveryProvider, existing.SourceUrl, existing.ExternalListingId,
                    existing.Status, existing.DiscoveredAtUtc, existing.LastCheckedAtUtc));
                continue;
            }
            recorded.Add(await listings.UpsertAsync(new(item.PackTypeId, retailer.Id, url,
                observation.Provenance.PluginId, observation.Provenance.SourceUrl, observation.Provenance.ExternalListingId), cancellationToken));
        }
        return new(recorded, review, batch.Executions, batch.Observations);
    }

    public async Task<CommercePluginBatch<PriceObservation>> RefreshPriceAsync(Guid listingId, CancellationToken cancellationToken = default) =>
        await plugins.RefreshPriceAsync(await GetListingAsync(listingId, cancellationToken), cancellationToken);
    public async Task<CommercePluginBatch<AvailabilityObservation>> RefreshAvailabilityAsync(Guid listingId, CancellationToken cancellationToken = default) =>
        await plugins.RefreshAvailabilityAsync(await GetListingAsync(listingId, cancellationToken), cancellationToken);
    public async Task<CommerceAffiliateDestination> ResolveAffiliateAsync(Guid listingId, CancellationToken cancellationToken = default) =>
        await plugins.ResolveAffiliateAsync(await GetListingAsync(listingId, cancellationToken), cancellationToken);

    private async Task<CanonicalCommerceListing> GetListingAsync(Guid listingId, CancellationToken ct)
    {
        var value = await (from listing in db.RetailerProductListings.AsNoTracking()
                           join retailer in db.Retailers.AsNoTracking() on listing.RetailerId equals retailer.Id
                           where listing.Id == listingId
                           select new { listing.Id, listing.PackTypeId, listing.RetailerId, RetailerName = retailer.Name,
                               retailer.WebsiteUrl, listing.ListingUrl, listing.ExternalListingId, RetailerStatus = retailer.Status, ListingStatus = listing.Status })
            .SingleOrDefaultAsync(ct) ?? throw new KeyNotFoundException();
        var item = await GetItemAsync(value.PackTypeId, ct);
        var programmes = await db.RetailerAffiliateProgrammes.AsNoTracking().Where(p => p.RetailerId == value.RetailerId)
            .Select(p => new CommerceAffiliateProgramme(p.Id, p.Network, p.ProgrammeId, p.Status, p.IsPreferred, p.DeepLinksAllowed)).ToListAsync(ct);
        return new(value.Id, item, value.RetailerId, value.RetailerName, value.WebsiteUrl, value.ListingUrl,
            value.ExternalListingId, programmes, value.RetailerStatus, value.ListingStatus);
    }

    private async Task<CanonicalSellableItem> GetItemAsync(Guid packTypeId, CancellationToken ct)
    {
        var row = await (from pack in db.PackTypes.AsNoTracking()
                         join size in db.SizeVariants.AsNoTracking() on pack.SizeVariantId equals size.Id
                         join variant in db.ProductVariants.AsNoTracking() on size.ProductVariantId equals variant.Id
                         join product in db.Products.AsNoTracking() on variant.ProductId equals product.Id
                         join brand in db.Brands.AsNoTracking() on product.BrandId equals brand.Id into brands
                         from brand in brands.DefaultIfEmpty()
                         where pack.Id == packTypeId && product.Status == ProductStatus.Current
                         select new { ProductId = product.Id, ProductVariantId = variant.Id, SizeVariantId = size.Id, PackTypeId = pack.Id,
                             ProductName = product.Name, BrandName = brand == null ? null : brand.Name, VariantName = variant.Name,
                             SizeName = size.ManufacturerSize, pack.QuantityPerPack, pack.PackagingType }).SingleOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException();
        var identifiers = await db.ProductIdentifiers.AsNoTracking().Where(i => i.PackTypeId == packTypeId)
            .Select(i => new CommerceIdentifier(i.Type, i.Value)).ToListAsync(ct);
        return new(row.ProductId, row.ProductVariantId, row.SizeVariantId, row.PackTypeId,
            row.ProductName, row.BrandName, row.VariantName, row.SizeName, row.QuantityPerPack, row.PackagingType, identifiers);
    }
    private static string? Host(string? url) => Uri.TryCreate(url, UriKind.Absolute, out var uri)
        ? (uri.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? uri.Host[4..] : uri.Host).ToLowerInvariant() : null;
}
