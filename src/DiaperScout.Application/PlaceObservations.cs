namespace DiaperScout.Application;

public sealed record PlaceItem(Guid Id, string Name, string AddressLine1, string Locality, string Postcode,
    string CountryCode, decimal Latitude, decimal Longitude);
public sealed record PlaceCountry(string Code, string Name);
public sealed record CreatePublicShopRequest(string Name, string AddressLine1, string Locality, string Postcode,
    string CountryCode, decimal? Latitude, decimal? Longitude, bool ConfirmPublicShop, bool ConfirmShopPosition);
public sealed record CreatePhysicalObservationRequest(Guid PackTypeId, Guid LocationId, DateTimeOffset ObservedAtUtc,
    decimal? PriceAmount, string? CurrencyCode, Guid ContributionId);
public sealed record PhysicalObservationReceipt(Guid Id, Guid LocationId, Guid PackTypeId, DateTimeOffset ObservedAtUtc);
public sealed record PlaceProductObservation(Guid ObservationId, Guid ProductId, Guid ProductVariantId, Guid SizeVariantId,
    Guid PackTypeId, string Name, string ProductUrl, string Size, int Quantity, DateTimeOffset ObservedAtUtc,
    decimal? PriceAmount, string? CurrencyCode);
public sealed record AtlasPlace(PlaceItem Place, int ProductCount, DateTimeOffset LatestObservedAtUtc,
    IReadOnlyList<PlaceProductObservation> Observations);
public interface IPlaceObservations
{
    Task<IReadOnlyList<PlaceCountry>> CountriesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<PlaceItem>> SearchAsync(string? query, CancellationToken ct = default);
    Task<PlaceItem> CreateShopAsync(ExplorerIdentity actor, CreatePublicShopRequest request, CancellationToken ct = default);
    Task<PhysicalObservationReceipt> ObserveAsync(ExplorerIdentity actor, CreatePhysicalObservationRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<AtlasPlace>> AtlasAsync(CancellationToken ct = default);
    Task<ProductIdentification?> PackAsync(Guid id, CancellationToken ct = default);
}
