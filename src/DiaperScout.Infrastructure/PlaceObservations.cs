using DiaperScout.Application;
using DiaperScout.Domain;
using DiaperScout.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace DiaperScout.Infrastructure;

internal sealed class PlaceObservations(DiaperScoutDbContext db, IEditorialAuthorisation authorisation, GeoapifyPlaces provider) : IPlaceObservations
{
    private static readonly HashSet<string> Currencies = CultureInfo.GetCultures(CultureTypes.SpecificCultures)
        .Select(c => new RegionInfo(c.Name).ISOCurrencySymbol).ToHashSet(StringComparer.Ordinal);

    public async Task<IReadOnlyList<PlaceCountry>> CountriesAsync(CancellationToken ct = default) =>
        await db.Countries.AsNoTracking().OrderBy(c => c.Name).Select(c => new PlaceCountry(c.IsoCode, c.Name)).ToListAsync(ct);

    private IQueryable<Location> QualifyingPlaces => db.Locations.AsNoTracking().Where(l => l.IsPublicCommercialPlace && l.Latitude != null && l.Longitude != null);
    private IQueryable<PlaceItem> Project(IQueryable<Location> locations) => from l in locations
        join c in db.Countries on l.CountryId equals c.Id
        where l.IsPublicCommercialPlace && l.Latitude != null && l.Longitude != null
        select new PlaceItem(l.Id, l.Name, l.AddressLine1, l.Locality, l.Postcode, c.IsoCode, l.Latitude!.Value, l.Longitude!.Value, l.Category, null, l.ProviderSnapshotJson != null ? "© OpenStreetMap contributors · Powered by Geoapify" : null);

    public async Task<IReadOnlyList<PlaceItem>> SearchAsync(string? query, CancellationToken ct = default)
    {
        query = query?.Trim();
        if (query?.Length > 150) throw Invalid("query", "Search must be 150 characters or fewer.");
        var q = QualifyingPlaces;
        if (!string.IsNullOrEmpty(query)) q = q.Where(l => l.Name.ToLower().Contains(query.ToLower()) ||
            l.Locality.ToLower().Contains(query.ToLower()) || l.Postcode.ToLower().Contains(query.ToLower()));
        return await Project(q.OrderBy(l => l.Name).ThenBy(l => l.Id).Take(50)).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<NearbyPlace>> NearbyAsync(NearbyPlaceRequest r, CancellationToken ct = default)
    {
        return await provider.NearbyAsync(r, ct);
    }

    public async Task<PlaceItem> SelectProviderPlaceAsync(ExplorerIdentity actor, SelectProviderPlaceRequest request, CancellationToken ct = default)
    {
        var snapshot = provider.Verify(request.SelectionToken);
        var json = System.Text.Json.JsonSerializer.Serialize(snapshot);
        var identity = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
        var existing = await db.Locations.AsNoTracking().SingleOrDefaultAsync(l => l.PlaceIdentity == identity, ct);
        if (existing is not null) return await Project(QualifyingPlaces.Where(l => l.Id == existing.Id)).SingleAsync(ct);
        var country = await db.Countries.SingleOrDefaultAsync(c => c.IsoCode == snapshot.CountryCode, ct)
            ?? throw Invalid("place", "This country is not supported yet.");
        var shop = Location.PublicShop(actor.UserId, country.Id, snapshot.Name, snapshot.AddressLine1,
            snapshot.Locality, snapshot.Postcode, snapshot.Latitude, snapshot.Longitude, identity, snapshot.Category);
        shop.RecordProviderSnapshot(json);
        db.Locations.Add(shop);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: "23505", ConstraintName: "IX_locations_PlaceIdentity" })
        {
            db.Entry(shop).State = EntityState.Detached;
            return await SelectProviderPlaceAsync(actor, request, ct);
        }
        return await Project(QualifyingPlaces.Where(l => l.Id == shop.Id)).SingleAsync(ct);
    }

    public async Task<PlaceItem> CreateShopAsync(ExplorerIdentity actor, CreatePublicShopRequest r, CancellationToken ct = default)
    {
        ValidateCategory(r.Category);
        if (!r.ConfirmPublicShop || !r.ConfirmShopPosition)
            throw Invalid("confirmation", "Confirm this is a public shop and the selected position belongs to that shop, not your home or current location.");
        if (r.Latitude is null or < -90 or > 90 || r.Longitude is null or < -180 or > 180)
            throw Invalid("coordinates", "Select the shop position or enter valid latitude and longitude.");
        var name = Text(r.Name, 200, "name"); var address = Text(r.AddressLine1, 250, "addressLine1");
        var locality = Text(r.Locality, 150, "locality"); var postcode = Text(r.Postcode, 32, "postcode");
        var countryCode = Text(r.CountryCode, 2, "countryCode").ToUpperInvariant();
        var country = await db.Countries.SingleOrDefaultAsync(c => c.IsoCode == countryCode, ct)
            ?? throw Invalid("countryCode", "Choose an existing country.");
        var identity = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('|',
            country.Id, Normalise(name), Normalise(address), Normalise(locality), Normalise(postcode).Replace(" ", "")))));
        var existing = await db.Locations.AsNoTracking().SingleOrDefaultAsync(l => l.PlaceIdentity == identity, ct);
        if (existing is not null) return await Project(QualifyingPlaces.Where(l => l.Id == existing.Id)).SingleAsync(ct);
        var shop = Location.PublicShop(actor.UserId, country.Id, name, address, locality, postcode,
            decimal.Round(r.Latitude.Value, 6), decimal.Round(r.Longitude.Value, 6), identity, r.Category);
        db.Locations.Add(shop);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: "23505" })
        {
            db.Entry(shop).State = EntityState.Detached;
            var duplicate = await db.Locations.AsNoTracking().SingleOrDefaultAsync(l => l.PlaceIdentity == identity, ct);
            if (duplicate is null) throw;
            return await Project(QualifyingPlaces.Where(l => l.Id == duplicate.Id)).SingleAsync(ct);
        }
        return await Project(QualifyingPlaces.Where(l => l.Id == shop.Id)).SingleAsync(ct);
    }

    public async Task<PlaceItem> UpdateCategoryAsync(AuthenticatedUser actor, Guid id, UpdatePlaceCategoryRequest request, CancellationToken ct = default)
    {
        if (!await authorisation.CanManageCatalogueAsync(actor, ct)) throw new UnauthorizedAccessException();
        ValidateCategory(request.Category);
        var place = await db.Locations.SingleOrDefaultAsync(l => l.Id == id && l.IsPublicCommercialPlace && l.Latitude != null && l.Longitude != null, ct)
            ?? throw new KeyNotFoundException();
        place.SetCategory(request.Category); await db.SaveChangesAsync(ct);
        return await Project(QualifyingPlaces.Where(l => l.Id == id)).SingleAsync(ct);
    }
    private static void ValidateCategory(PlaceCategory? category) {
        if (category.HasValue && !Enum.IsDefined(category.Value)) throw Invalid("category", "Choose a supported place type or leave it unspecified.");
    }
    public async Task<ProductIdentification?> PackAsync(Guid id, CancellationToken ct = default)
    {
        var result = await (from p in db.PackTypes.AsNoTracking()
            join s in db.SizeVariants on p.SizeVariantId equals s.Id
            join v in db.ProductVariants on s.ProductVariantId equals v.Id
            join product in db.Products on v.ProductId equals product.Id
            join brand in db.Brands on product.BrandId equals brand.Id into brands
            from brand in brands.DefaultIfEmpty()
            where p.Id == id && product.Status == ProductStatus.Current
            select new { Brand = brand == null ? null : brand.Name, Identification = new ProductIdentification(new ProductSummary(product.Id, product.Name, product.Slug, product.ProductType, product.Status),
                v.Id, v.Name, s.Id, s.ManufacturerSize, p.Id, p.QuantityPerPack, p.PackagingType, "") }).SingleOrDefaultAsync(ct);
        return result is null ? null : result.Identification with { Product = result.Identification.Product with { Name = PublicProductIdentity.DisplayName(result.Brand, result.Identification.Product.Name) } };
    }

    public async Task<PhysicalObservationReceipt> ObserveAsync(ExplorerIdentity actor, CreatePhysicalObservationRequest r, CancellationToken ct = default)
    {
        // PostgreSQL timestamps store microseconds. Compare retries at the same durable precision.
        r = r with { ObservedAtUtc = new DateTimeOffset(r.ObservedAtUtc.UtcTicks - r.ObservedAtUtc.UtcTicks % 10, TimeSpan.Zero) };
        if (r.ContributionId == Guid.Empty) throw Invalid("contributionId", "A contribution identifier is required.");
        var existing = await db.Observations.AsNoTracking().SingleOrDefaultAsync(o => o.AuthorUserId == actor.UserId && o.ContributionId == r.ContributionId, ct);
        if (existing is not null)
        {
            if (existing.PackTypeId != r.PackTypeId || existing.LocationId != r.LocationId || existing.PriceAmount != r.PriceAmount ||
                existing.PriceCurrencyCode != (r.PriceAmount.HasValue ? r.CurrencyCode?.Trim().ToUpperInvariant() : null) || existing.ObservedAtUtc != r.ObservedAtUtc)
                throw Invalid("contributionId", "This contribution identifier has already been used for another observation.");
            return Receipt(existing);
        }
        var pack = await PackAsync(r.PackTypeId, ct) ?? throw Invalid("packTypeId", "This pack is no longer available in the current catalogue. Scan or choose another item.");
        if (!await QualifyingPlaces.AnyAsync(l => l.Id == r.LocationId, ct)) throw Invalid("locationId", "Choose a public shop with a confirmed position.");
        if (r.ObservedAtUtc < new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero) || r.ObservedAtUtc > DateTimeOffset.UtcNow.AddMinutes(5))
            throw Invalid("observedAtUtc", "Choose a valid observation time, not in the future.");
        var currency = r.CurrencyCode?.Trim().ToUpperInvariant();
        if (r.PriceAmount.HasValue && (r.PriceAmount < 0 || r.PriceAmount > 9999999999.99m || decimal.Round(r.PriceAmount.Value, 2) != r.PriceAmount ||
            currency is null || !Currencies.Contains(currency))) throw Invalid("price", "Enter a nonnegative price with at most two decimal places and a recognised currency code.");
        var observation = new Observation(actor.UserId, ObservationType.RetailAvailability, r.ObservedAtUtc.ToUniversalTime(), pack.Product.Id, locationId: r.LocationId);
        observation.RecordExactPack(r.PackTypeId, r.ContributionId, r.PriceAmount, currency);
        observation.Submit(); db.Observations.Add(observation);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: "23505", ConstraintName: "IX_observations_AuthorUserId_ContributionId" })
        {
            db.Entry(observation).State = EntityState.Detached;
            return await ObserveAsync(actor, r, ct);
        }
        return Receipt(observation);
    }

    public async Task<IReadOnlyList<string>> PublicPlaceSnapshotsAsync(CancellationToken ct = default)
    {
        var publishedIds = db.Observations.Where(o => o.Type == ObservationType.RetailAvailability && o.PackTypeId != null &&
            (o.State == ObservationState.Submitted || o.State == ObservationState.Accepted)).Select(o => o.LocationId);
        return await db.Locations.AsNoTracking().Where(l => publishedIds.Contains(l.Id) && l.IsPublicCommercialPlace && l.ProviderSnapshotJson != null)
            .OrderBy(l => l.Id).Select(l => l.ProviderSnapshotJson!).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<AtlasPlace>> AtlasAsync(CancellationToken ct = default)
    {
        var records = await (from o in db.Observations.AsNoTracking()
            join l in db.Locations on o.LocationId equals l.Id
            join p in db.PackTypes on o.PackTypeId equals p.Id
            join s in db.SizeVariants on p.SizeVariantId equals s.Id
            join v in db.ProductVariants on s.ProductVariantId equals v.Id
            join product in db.Products on v.ProductId equals product.Id
            join brand in db.Brands on product.BrandId equals brand.Id into brands
            from brand in brands.DefaultIfEmpty()
            where l.IsPublicCommercialPlace && l.Latitude != null && l.Longitude != null &&
                o.Type == ObservationType.RetailAvailability && (o.State == ObservationState.Submitted || o.State == ObservationState.Accepted) &&
                product.Status == ProductStatus.Current
            orderby o.ObservedAtUtc descending, o.Id
            select new { LocationId = l.Id, Brand = brand == null ? null : brand.Name, Variant = v.Name, Observation = new PlaceProductObservation(o.Id, product.Id, v.Id, s.Id, p.Id,
                product.Name, "/products/" + product.Slug + "?variantId=" + v.Id + "&packTypeId=" + p.Id,
                s.ManufacturerSize, p.QuantityPerPack, o.ObservedAtUtc, o.PriceAmount, o.PriceCurrencyCode) }).Take(2000).ToListAsync(ct);
        var ids = records.Select(r => r.LocationId).Distinct().ToArray();
        var places = await Project(QualifyingPlaces.Where(l => ids.Contains(l.Id))).ToListAsync(ct);
        return places.Select(l => {
            var observations = records.Where(r => r.LocationId == l.Id).Select(r => r.Observation with { Name = PublicProductIdentity.DisplayName(r.Brand, r.Observation.Name, r.Variant) }).ToArray();
            return new AtlasPlace(l, observations.Select(o => o.PackTypeId).Distinct().Count(), observations.Max(o => o.ObservedAtUtc), observations);
        }).ToArray();
    }

    private static PhysicalObservationReceipt Receipt(Observation o) => new(o.Id, o.LocationId!.Value, o.PackTypeId!.Value, o.ObservedAtUtc);
    private static string Normalise(string value) => string.Join(' ', value.Normalize(NormalizationForm.FormKC).ToUpperInvariant().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    private static string Text(string? value, int max, string field) => string.IsNullOrWhiteSpace(value) || value.Trim().Length > max ?
        throw Invalid(field, $"Enter a value of at most {max} characters.") : value.Trim();
    private static CatalogueValidationException Invalid(string field, string message) => new(field, message);
}
