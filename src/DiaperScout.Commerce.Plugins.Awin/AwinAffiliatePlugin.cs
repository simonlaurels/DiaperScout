using System.Globalization;
using DiaperScout.Application;
using DiaperScout.Domain;
using Microsoft.Extensions.Options;

namespace DiaperScout.Commerce.Plugins.Awin;

public sealed class AwinAffiliatePlugin(IOptions<AwinAffiliateProgrammeDiscoveryOptions> options,
    AwinAffiliateProgrammeDiscoveryProvider programmeDiscovery, TimeProvider clock) : IAffiliateProgrammeDiscoveryPlugin
{
    public CommercePluginDescriptor Descriptor { get; } = new("awin.affiliate", "Awin Affiliate", CommercePluginFamily.Affiliate, "1.0.0");
    // Deep-link resolution needs the public publisher identifier, not the programme-discovery API token.
    public CommercePluginConfiguration Configuration => Numeric(options.Value.PublisherId)
        ? CommercePluginConfiguration.Ready : CommercePluginConfiguration.MissingConfiguration;
    public CommercePluginConfiguration ProgrammeDiscoveryConfiguration => !options.Value.Enabled
        ? CommercePluginConfiguration.DisabledByConfiguration
        : Configuration == CommercePluginConfiguration.Ready && !string.IsNullOrWhiteSpace(options.Value.AccessToken)
            && Uri.TryCreate(options.Value.BaseUrl, UriKind.Absolute, out var uri) && uri.Scheme == "https"
            && !string.IsNullOrWhiteSpace(options.Value.CountryCodes)
                ? CommercePluginConfiguration.Ready : CommercePluginConfiguration.MissingConfiguration;

    public bool CanHandle(CanonicalCommerceListing listing) => EligibleProgramme(listing) is not null;
    public Task<AffiliateObservation?> ResolveAsync(CanonicalCommerceListing listing, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var programme = EligibleProgramme(listing);
        if (Configuration != CommercePluginConfiguration.Ready || programme is null)
            return Task.FromResult<AffiliateObservation?>(null);
        var destination = Uri.EscapeDataString(listing.ListingUrl);
        var advertiser = long.Parse(programme.ProgrammeId, NumberStyles.None, CultureInfo.InvariantCulture);
        var publisher = long.Parse(options.Value.PublisherId, NumberStyles.None, CultureInfo.InvariantCulture);
        var url = $"https://www.awin1.com/cread.php?awinmid={advertiser}&awinaffid={publisher}&ued={destination}";
        return Task.FromResult<AffiliateObservation?>(new(url, "Awin", programme.Id, clock.GetUtcNow(), listing.ListingUrl));
    }
    public bool CanDiscoverProgrammes(RetailerAffiliateProgrammeDiscoveryTarget retailer) =>
        Uri.TryCreate(retailer.RetailerWebsiteUrl, UriKind.Absolute, out var uri) && (uri.Scheme == "http" || uri.Scheme == "https");
    public Task<IReadOnlyList<RetailerAffiliateProgrammeDiscoveryCandidate>> DiscoverProgrammesAsync(
        RetailerAffiliateProgrammeDiscoveryTarget retailer, CancellationToken cancellationToken) => programmeDiscovery.DiscoverAsync(retailer, cancellationToken);

    private static CommerceAffiliateProgramme? EligibleProgramme(CanonicalCommerceListing listing) => listing.AffiliateProgrammes
        .Where(p => p.IsPreferred && p.Status == AffiliateProgrammeStatus.Configured
            && p.Network.Equals("Awin", StringComparison.OrdinalIgnoreCase) && p.DeepLinksAllowed != false && Numeric(p.ProgrammeId))
        .OrderBy(p => p.Id).FirstOrDefault();
    private static bool Numeric(string value) => long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out _);
}
