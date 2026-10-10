using DiaperScout.Application;
using DiaperScout.Domain;
using Microsoft.EntityFrameworkCore;

namespace DiaperScout.Infrastructure;

internal sealed partial class RetailerDiscovery
{
    public async Task<RetailerProductListingItem> UpsertAsync(UpsertRetailerListing command, CancellationToken cancellationToken = default)
    {
        await RequireCurrentPackAsync(command.PackTypeId, cancellationToken);
        var retailer = db.Retailers.Local.FirstOrDefault(x => x.Id == command.RetailerId) ?? await db.Retailers.SingleOrDefaultAsync(x => x.Id == command.RetailerId, cancellationToken)
            ?? throw new CatalogueValidationException("retailerId", "Choose an existing retailer.");
        var url = NormalizeUrlForComparison(command.ListingUrl)
            ?? throw new CatalogueValidationException("listingUrl", "Enter a valid HTTP or HTTPS product listing URL.");
        var listing = await db.RetailerProductListings.SingleOrDefaultAsync(x =>
            x.PackTypeId == command.PackTypeId && x.RetailerId == command.RetailerId && x.ListingUrl == url, cancellationToken);
        try
        {
            if (listing is null)
            {
                listing = new RetailerProductListing(command.PackTypeId, command.RetailerId, url,
                    command.DiscoveryProvider, command.SourceUrl, command.ExternalListingId);
                db.RetailerProductListings.Add(listing);
            }
            else listing.UpdateDiscovery(url, command.DiscoveryProvider, command.SourceUrl, command.ExternalListingId);
            ValidateMetadata(listing);
        }
        catch (ArgumentException exception) { throw Validation(exception); }
        await db.SaveChangesAsync(cancellationToken);
        return ToListingItem(listing, retailer);
    }

    public async Task<RetailerProductListingItem> CreateManualListingAsync(AuthenticatedUser actor,
        ManualRetailerListingRequest command, CancellationToken cancellationToken = default)
    {
        await RequireListingModeratorAsync(actor, cancellationToken);
        var pack = await db.PackTypes.AsNoTracking().SingleOrDefaultAsync(x => x.Id == command.PackTypeId, cancellationToken);
        var size = pack is null ? null : await db.SizeVariants.AsNoTracking().SingleOrDefaultAsync(x => x.Id == pack.SizeVariantId, cancellationToken);
        var variant = size is null ? null : await db.ProductVariants.AsNoTracking().SingleOrDefaultAsync(x => x.Id == size.ProductVariantId, cancellationToken);
        if (pack is null || size is null || variant is null || size.Id != command.SizeVariantId ||
            variant.Id != command.ProductVariantId || variant.ProductId != command.ProductId)
            throw new CatalogueValidationException("packTypeId", "Choose a pack belonging to the selected product, variant and size.");
        var url = NormalizeUrlForComparison(command.ListingUrl);
        if (url is not null && await db.RetailerProductListings.AnyAsync(x =>
            x.PackTypeId == command.PackTypeId && x.RetailerId == command.RetailerId && x.ListingUrl == url, cancellationToken))
            throw new CatalogueValidationException("listingUrl", "This retailer already has that listing for this pack. Edit the existing listing instead.");
        return await UpsertAsync(new(command.PackTypeId, command.RetailerId, command.ListingUrl,
            "Manual", command.SourceUrl, command.ExternalListingId), cancellationToken);
    }

    public async Task<RetailerProductListingItem> UpdateListingAsync(AuthenticatedUser actor, Guid listingId,
        RetailerListingUpdate command, CancellationToken cancellationToken = default)
    {
        await RequireListingModeratorAsync(actor, cancellationToken);
        var listing = await db.RetailerProductListings.SingleOrDefaultAsync(x => x.Id == listingId, cancellationToken) ?? throw new KeyNotFoundException();
        var url = NormalizeUrlForComparison(command.ListingUrl)
            ?? throw new CatalogueValidationException("listingUrl", "Enter a valid HTTP or HTTPS product listing URL.");
        if (await db.RetailerProductListings.AnyAsync(x => x.Id != listingId && x.PackTypeId == listing.PackTypeId &&
            x.RetailerId == listing.RetailerId && x.ListingUrl == url, cancellationToken))
            throw new CatalogueValidationException("listingUrl", "This retailer already has that URL for this pack.");
        var old = (listing.ListingUrl, listing.SourceUrl, listing.ExternalListingId);
        try
        {
            listing.UpdateDiscovery(url, listing.DiscoveryProvider, command.SourceUrl, command.ExternalListingId);
            ValidateMetadata(listing);
        }
        catch (ArgumentException exception) { throw Validation(exception); }
        if (old != (listing.ListingUrl, listing.SourceUrl, listing.ExternalListingId)) listing.MarkNeedsReview();
        await db.SaveChangesAsync(cancellationToken);
        var retailer = await db.Retailers.SingleAsync(x => x.Id == listing.RetailerId, cancellationToken);
        return ToListingItem(listing, retailer);
    }

    public async Task<RetailerProductListingItem> SetListingStatusAsync(AuthenticatedUser actor, Guid listingId,
        RetailerProductDiscoveryStatus status, CancellationToken cancellationToken = default)
    {
        await RequireListingModeratorAsync(actor, cancellationToken);
        var listing = await db.RetailerProductListings.SingleOrDefaultAsync(x => x.Id == listingId, cancellationToken) ?? throw new KeyNotFoundException();
        var retailer = await db.Retailers.SingleAsync(x => x.Id == listing.RetailerId, cancellationToken);
        switch (status)
        {
            case RetailerProductDiscoveryStatus.Verified:
                if (retailer.Status != RetailerStatus.Verified)
                    throw new CatalogueValidationException("retailerId", "Verify the retailer identity before confirming its listing.");
                await RequireCurrentPackAsync(listing.PackTypeId, cancellationToken);
                listing.MarkVerified(); break;
            case RetailerProductDiscoveryStatus.NeedsReview: listing.MarkNeedsReview(); break;
            case RetailerProductDiscoveryStatus.Inactive: listing.MarkInactive(); break;
            default: throw new CatalogueValidationException("status", "Choose Verified, NeedsReview or Inactive.");
        }
        await db.SaveChangesAsync(cancellationToken);
        return ToListingItem(listing, retailer);
    }

    public async Task<IReadOnlyList<RetailListingProductOption>> GetListingCatalogueAsync(AuthenticatedUser actor, CancellationToken cancellationToken = default)
    {
        await RequireListingModeratorAsync(actor, cancellationToken);
        // Current canonical products are the public catalogue; drafts live separately in submissions.
        var products = await db.Products.AsNoTracking().Where(x => x.Status == ProductStatus.Current).OrderBy(x => x.Name).ToListAsync(cancellationToken);
        var productIds = products.Select(x => x.Id).ToArray();
        var variants = await db.ProductVariants.AsNoTracking().Where(x => productIds.Contains(x.ProductId)).OrderBy(x => x.Name).ToListAsync(cancellationToken);
        var variantIds = variants.Select(x => x.Id).ToArray();
        var sizes = await db.SizeVariants.AsNoTracking().Where(x => variantIds.Contains(x.ProductVariantId)).OrderBy(x => x.ManufacturerSize).ToListAsync(cancellationToken);
        var sizeIds = sizes.Select(x => x.Id).ToArray();
        var packs = await db.PackTypes.AsNoTracking().Where(x => sizeIds.Contains(x.SizeVariantId)).OrderBy(x => x.QuantityPerPack).ToListAsync(cancellationToken);
        var packIds = packs.Select(x => x.Id).ToArray();
        var gtins = await db.ProductIdentifiers.AsNoTracking().Where(x => packIds.Contains(x.PackTypeId) && x.Type == IdentifierType.Gtin).ToListAsync(cancellationToken);
        return products.Select(product => new RetailListingProductOption(product.Id, product.Name, product.Slug,
            variants.Where(v => v.ProductId == product.Id).Select(v => new RetailListingVariantOption(v.Id, v.Name,
                sizes.Where(s => s.ProductVariantId == v.Id).Select(s => new RetailListingSizeOption(s.Id, s.ManufacturerSize,
                    packs.Where(p => p.SizeVariantId == s.Id).Select(p => new RetailListingPackOption(p.Id, p.QuantityPerPack,
                        p.PackagingType, gtins.FirstOrDefault(g => g.PackTypeId == p.Id)?.Value)).ToArray())).ToArray())).ToArray())).ToArray();
    }

    public async Task<IReadOnlyList<ManagedRetailerListing>> GetListingsAsync(AuthenticatedUser actor, Guid? retailerId = null, CancellationToken cancellationToken = default)
    {
        await RequireListingModeratorAsync(actor, cancellationToken);
        return await (from listing in db.RetailerProductListings.AsNoTracking()
                      join retailer in db.Retailers on listing.RetailerId equals retailer.Id
                      join pack in db.PackTypes on listing.PackTypeId equals pack.Id
                      join size in db.SizeVariants on pack.SizeVariantId equals size.Id
                      join variant in db.ProductVariants on size.ProductVariantId equals variant.Id
                      join product in db.Products on variant.ProductId equals product.Id
                      where retailerId == null || listing.RetailerId == retailerId
                      orderby listing.DiscoveredAtUtc descending
                      select new ManagedRetailerListing(new RetailerProductListingItem(listing.Id, pack.Id, retailer.Id,
                          retailer.Name, retailer.Status, listing.ListingUrl, listing.DiscoveryProvider, listing.SourceUrl,
                          listing.ExternalListingId, listing.Status, listing.DiscoveredAtUtc, listing.LastCheckedAtUtc),
                          product.Id, product.Name, product.Slug, variant.Id, variant.Name, size.Id, size.ManufacturerSize,
                          pack.QuantityPerPack, pack.PackagingType)).ToListAsync(cancellationToken);
    }

    private async Task RequireListingModeratorAsync(AuthenticatedUser actor, CancellationToken cancellationToken)
    {
        if (!await editorialAuthorisation.CanPublishAtlasAsync(actor, cancellationToken)) throw new UnauthorizedAccessException();
    }
    private async Task RequireCurrentPackAsync(Guid packId, CancellationToken cancellationToken)
    {
        if (!await (from pack in db.PackTypes
                    join size in db.SizeVariants on pack.SizeVariantId equals size.Id
                    join variant in db.ProductVariants on size.ProductVariantId equals variant.Id
                    join product in db.Products on variant.ProductId equals product.Id
                    where pack.Id == packId && product.Status == ProductStatus.Current
                    select pack.Id).AnyAsync(cancellationToken))
            throw new CatalogueValidationException("packTypeId", "Choose a sellable pack from a current canonical product.");
    }
    private static void ValidateMetadata(RetailerProductListing listing)
    {
        if (listing.ListingUrl.Length > 2000 || listing.SourceUrl?.Length > 2000)
            throw new ArgumentException("URLs must be 2000 characters or fewer.", "listingUrl");
        if (listing.ExternalListingId?.Length > 300)
            throw new ArgumentException("External listing identifier must be 300 characters or fewer.", "externalListingId");
    }
    private static CatalogueValidationException Validation(ArgumentException exception) =>
        new(exception.ParamName ?? "listing", exception.Message);
}
