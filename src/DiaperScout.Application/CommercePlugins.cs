using DiaperScout.Domain;

namespace DiaperScout.Application;

public enum CommercePluginFamily { RetailDiscovery, Pricing, Availability, Affiliate }
public enum CommercePluginConfiguration { Ready, MissingConfiguration, DisabledByConfiguration }
public enum CommercePluginExecutionStatus { Succeeded, Unsupported, Disabled, MissingConfiguration, Failed, TimedOut, InvalidResult }
public sealed record CommercePluginDescriptor(string Id, string Name, CommercePluginFamily Family, string Version);
public sealed record CommercePluginPolicy(bool Enabled, int Priority, TimeSpan Timeout);
public sealed record CommercePluginExecution(string PluginId, CommercePluginFamily Family,
    CommercePluginExecutionStatus Status, DateTimeOffset CompletedAtUtc);
public sealed record CommercePluginStatus(CommercePluginDescriptor Plugin, bool Enabled,
    CommercePluginConfiguration Configuration, string Health, DateTimeOffset? LastSuccessAtUtc,
    DateTimeOffset? LastFailureAtUtc, string? LastFailureCode, string HealthScope = "This application instance");
public sealed record CommerceProvenance(string PluginId, string PluginVersion, string? ExternalListingId,
    DateTimeOffset ObservedAtUtc, DateTimeOffset RefreshedAtUtc, string? SourceUrl);
public sealed record SourcedCommerceObservation<T>(T Value, CommerceProvenance Provenance);
public sealed record CommercePluginBatch<T>(IReadOnlyList<SourcedCommerceObservation<T>> Observations,
    IReadOnlyList<CommercePluginExecution> Executions);

// Immutable snapshots: plugins receive identities/evidence, never mutable domain entities or persistence services.
public sealed record CommerceIdentifier(IdentifierType Type, string Value);
public sealed record CanonicalSellableItem(Guid ProductId, Guid ProductVariantId, Guid SizeVariantId, Guid PackTypeId,
    string ProductName, string? BrandName, string? VariantName, string SizeName, int QuantityPerPack,
    PackagingType PackagingType, IReadOnlyList<CommerceIdentifier> Identifiers);
public sealed record CommerceAffiliateProgramme(Guid Id, string Network, string ProgrammeId,
    AffiliateProgrammeStatus Status, bool IsPreferred, bool? DeepLinksAllowed);
public sealed record CanonicalCommerceListing(Guid ListingId, CanonicalSellableItem Item, Guid RetailerId,
    string RetailerName, string? RetailerWebsiteUrl, string ListingUrl, string? ExternalListingId,
    IReadOnlyList<CommerceAffiliateProgramme> AffiliateProgrammes,
    RetailerStatus RetailerStatus = RetailerStatus.Verified,
    RetailerProductDiscoveryStatus ListingStatus = RetailerProductDiscoveryStatus.Verified);
public sealed record CommerceRetailerEvidence(string Name, string? WebsiteUrl, string? EvidenceUrl,
    Guid? CanonicalRetailerId = null);
public sealed record RetailDiscoveryObservation(string ListingUrl, CommerceRetailerEvidence Retailer,
    IReadOnlyList<CommerceIdentifier> ObservedIdentifiers, DateTimeOffset ObservedAtUtc,
    string? ExternalListingId = null, string? SourceUrl = null, string? Title = null,
    int? QuantityPerPack = null, decimal? MatchConfidence = null);
public sealed record PriceObservation(decimal Amount, string Currency, DateTimeOffset ObservedAtUtc,
    string SourceUrl, string? ExternalListingId = null, DateTimeOffset? ValidUntilUtc = null);
public sealed record AvailabilityObservation(RetailerProductAvailability Availability, DateTimeOffset ObservedAtUtc,
    string SourceUrl, string? ExternalListingId = null, DateTimeOffset? ValidUntilUtc = null);
public sealed record AffiliateObservation(string DestinationUrl, string Network, Guid ProgrammeId,
    DateTimeOffset ResolvedAtUtc, string? SourceUrl = null);
public sealed record CommerceAffiliateDestination(string Url, string? Network, bool IsAffiliateBacked,
    CommerceProvenance? Provenance, IReadOnlyList<CommercePluginExecution> Executions);

public interface ICommercePlugin
{
    CommercePluginDescriptor Descriptor { get; }
    CommercePluginConfiguration Configuration { get; }
}
public interface IRetailDiscoveryPlugin : ICommercePlugin
{
    bool CanHandle(CanonicalSellableItem item);
    Task<IReadOnlyList<RetailDiscoveryObservation>> DiscoverAsync(CanonicalSellableItem item, CancellationToken cancellationToken);
}
public interface IPricingPlugin : ICommercePlugin
{
    bool CanHandle(CanonicalCommerceListing listing);
    Task<PriceObservation?> ObservePriceAsync(CanonicalCommerceListing listing, CancellationToken cancellationToken);
}
public interface IAvailabilityPlugin : ICommercePlugin
{
    bool CanHandle(CanonicalCommerceListing listing);
    Task<AvailabilityObservation?> ObserveAvailabilityAsync(CanonicalCommerceListing listing, CancellationToken cancellationToken);
}
public interface IAffiliatePlugin : ICommercePlugin
{
    bool CanHandle(CanonicalCommerceListing listing);
    Task<AffiliateObservation?> ResolveAsync(CanonicalCommerceListing listing, CancellationToken cancellationToken);
}
// Optional affiliate-family capability; not retail listing discovery and not required of other affiliate plugins.
public interface IAffiliateProgrammeDiscoveryPlugin : IAffiliatePlugin
{
    CommercePluginConfiguration ProgrammeDiscoveryConfiguration { get; }
    bool CanDiscoverProgrammes(RetailerAffiliateProgrammeDiscoveryTarget retailer);
    Task<IReadOnlyList<RetailerAffiliateProgrammeDiscoveryCandidate>> DiscoverProgrammesAsync(
        RetailerAffiliateProgrammeDiscoveryTarget retailer, CancellationToken cancellationToken);
}
public interface ICommercePluginRegistry { IReadOnlyList<ICommercePlugin> Plugins { get; } }
public interface ICommercePluginRuntime
{
    Task<CommercePluginPolicy> GetPolicyAsync(string pluginId, CancellationToken cancellationToken);
    void Record(CommercePluginExecution execution);
}
public interface ICommercePluginOrchestrator
{
    Task<CommercePluginBatch<RetailDiscoveryObservation>> DiscoverAsync(CanonicalSellableItem item, CancellationToken cancellationToken = default);
    Task<CommercePluginBatch<PriceObservation>> RefreshPriceAsync(CanonicalCommerceListing listing, CancellationToken cancellationToken = default);
    Task<CommercePluginBatch<AvailabilityObservation>> RefreshAvailabilityAsync(CanonicalCommerceListing listing, CancellationToken cancellationToken = default);
    Task<CommerceAffiliateDestination> ResolveAffiliateAsync(CanonicalCommerceListing listing, CancellationToken cancellationToken = default);
    Task<CommercePluginBatch<RetailerAffiliateProgrammeDiscoveryCandidate>> DiscoverAffiliateProgrammesAsync(
        RetailerAffiliateProgrammeDiscoveryTarget retailer, CancellationToken cancellationToken = default);
}
public interface ICommercePluginManagement
{
    Task<IReadOnlyList<CommercePluginStatus>> GetAsync(AuthenticatedUser actor, CancellationToken cancellationToken = default);
    Task<CommercePluginStatus> SetEnabledAsync(AuthenticatedUser actor, string pluginId, bool enabled, CancellationToken cancellationToken = default);
}
public sealed record CommercePluginEnabledRequest(bool Enabled);
public sealed record CommerceDiscoveryReview(string PluginId, string Code);
public sealed record CanonicalCommerceDiscoveryRun(IReadOnlyList<RetailerProductListingItem> Listings,
    IReadOnlyList<CommerceDiscoveryReview> Review, IReadOnlyList<CommercePluginExecution> Executions,
    IReadOnlyList<SourcedCommerceObservation<RetailDiscoveryObservation>> Observations);
public interface ICanonicalCommerce
{
    Task<CanonicalCommerceDiscoveryRun> DiscoverListingsAsync(Guid packTypeId, CancellationToken cancellationToken = default);
    Task<CommercePluginBatch<PriceObservation>> RefreshPriceAsync(Guid listingId, CancellationToken cancellationToken = default);
    Task<CommercePluginBatch<AvailabilityObservation>> RefreshAvailabilityAsync(Guid listingId, CancellationToken cancellationToken = default);
    Task<CommerceAffiliateDestination> ResolveAffiliateAsync(Guid listingId, CancellationToken cancellationToken = default);
}
