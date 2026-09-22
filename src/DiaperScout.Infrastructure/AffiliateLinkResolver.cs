using System.Globalization;
using DiaperScout.Domain;
using Microsoft.Extensions.Options;

namespace DiaperScout.Infrastructure;

public sealed record AffiliateLinkResolution(
    string Url,
    string? Network,
    bool IsAffiliateBacked);

public interface IAffiliateLinkResolver
{
    AffiliateLinkResolution Resolve(string listingUrl, RetailerAffiliateProgramme? programme);
}

public sealed class AwinAffiliateLinkResolver(
    IOptions<AwinAffiliateProgrammeDiscoveryOptions> options) : IAffiliateLinkResolver
{
    public AffiliateLinkResolution Resolve(string listingUrl, RetailerAffiliateProgramme? programme)
    {
        if (programme is null
            || programme.Status != AffiliateProgrammeStatus.Configured
            || !programme.IsPreferred
            || !string.Equals(programme.Network, "Awin", StringComparison.OrdinalIgnoreCase)
            || programme.DeepLinksAllowed == false
            || !long.TryParse(programme.ProgrammeId, NumberStyles.None, CultureInfo.InvariantCulture, out var advertiserId)
            || !long.TryParse(options.Value.PublisherId, NumberStyles.None, CultureInfo.InvariantCulture, out var publisherId))
        {
            return new AffiliateLinkResolution(listingUrl, null, false);
        }

        var encodedDestination = Uri.EscapeDataString(listingUrl);
        var url = $"https://www.awin1.com/cread.php?awinmid={advertiserId}&awinaffid={publisherId}&ued={encodedDestination}";
        return new AffiliateLinkResolution(url, "Awin", true);
    }
}
