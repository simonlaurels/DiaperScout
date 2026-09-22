using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using DiaperScout.Application;
using DiaperScout.Domain;
using Microsoft.Extensions.Options;

namespace DiaperScout.Infrastructure;

public sealed class AwinAffiliateProgrammeDiscoveryOptions
{
    public const string SectionName = "AwinAffiliateDiscovery";

    public bool Enabled { get; set; }
    public string BaseUrl { get; set; } = "https://api.awin.com/";
    public string PublisherId { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string CountryCodes { get; set; } = "GB,US";
}

public sealed class AwinAffiliateProgrammeDiscoveryProvider(
    HttpClient httpClient,
    IOptions<AwinAffiliateProgrammeDiscoveryOptions> options) : IRetailerAffiliateProgrammeDiscoveryProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<RetailerAffiliateProgrammeDiscoveryCandidate>> DiscoverAsync(
        RetailerAffiliateProgrammeDiscoveryTarget retailer,
        CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (!settings.Enabled)
            return [];

        if (string.IsNullOrWhiteSpace(settings.PublisherId))
            throw new InvalidOperationException("Awin affiliate discovery requires a publisher ID.");

        if (string.IsNullOrWhiteSpace(settings.AccessToken))
            throw new InvalidOperationException("Awin affiliate discovery requires an API access token.");

        if (!TryGetHttpUri(retailer.RetailerWebsiteUrl, out var retailerWebsite))
            return [];

        var retailerHost = NormalizeHost(retailerWebsite!.Host);
        if (retailerHost.Length == 0)
            return [];

        var countries = ParseCountryCodes(settings.CountryCodes);
        if (countries.Count == 0)
            throw new InvalidOperationException("Awin affiliate discovery requires at least one country code.");

        ConfigureClient(settings);

        var matches = new Dictionary<long, AwinProgrammeCandidate>();

        foreach (var countryCode in countries)
        {
            foreach (var relationship in new[] { "notjoined", "pending", "joined" })
            {
                var programmes = await GetProgrammesAsync(countryCode, relationship, cancellationToken);

                foreach (var programme in programmes)
                {
                    if (programme.Id <= 0 || !MatchesRetailer(programme, retailerHost))
                        continue;

                    matches[programme.Id] = new AwinProgrammeCandidate(programme, relationship);
                }
            }
        }

        var results = new List<RetailerAffiliateProgrammeDiscoveryCandidate>();

        foreach (var match in matches.Values.OrderBy(value => value.Programme.Id))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var details = await GetProgrammeDetailsAsync(match.Programme.Id, match.Relationship, cancellationToken);
            var programmeInfo = details?.ProgrammeInfo;

            var displayUrl = programmeInfo?.DisplayUrl ?? match.Programme.DisplayUrl;
            var deeplinkEnabled = programmeInfo?.DeeplinkEnabled;
            var membershipStatus = programmeInfo?.MembershipStatus ?? match.Relationship;

            results.Add(new RetailerAffiliateProgrammeDiscoveryCandidate(
                "Awin",
                match.Programme.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
                programmeInfo?.Name ?? match.Programme.Name,
                MapStatus(membershipStatus),
                displayUrl,
                null,
                null,
                null,
                deeplinkEnabled,
                IsApplicationRequired(membershipStatus),
                BuildProgrammeDetailsUrl(match.Programme.Id, match.Relationship)));
        }

        return results;
    }

    private async Task<IReadOnlyList<AwinProgramme>> GetProgrammesAsync(
        string countryCode,
        string relationship,
        CancellationToken cancellationToken)
    {
        var url = BuildUrl(
            $"publishers/{Uri.EscapeDataString(options.Value.PublisherId)}/programmes",
            ("countryCode", countryCode),
            ("relationship", relationship));

        using var response = await httpClient.GetAsync(url, cancellationToken);
        await EnsureSuccessAsync(response, "programme discovery", cancellationToken);

        return await response.Content.ReadFromJsonAsync<List<AwinProgramme>>(JsonOptions, cancellationToken)
            ?? [];
    }

    private async Task<AwinProgrammeDetails?> GetProgrammeDetailsAsync(
        long advertiserId,
        string relationship,
        CancellationToken cancellationToken)
    {
        var url = BuildUrl(
            $"publishers/{Uri.EscapeDataString(options.Value.PublisherId)}/programmedetails",
            ("advertiserId", advertiserId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            ("relationship", relationship));

        using var response = await httpClient.GetAsync(url, cancellationToken);
        await EnsureSuccessAsync(response, "programme details discovery", cancellationToken);

        return await response.Content.ReadFromJsonAsync<AwinProgrammeDetails>(JsonOptions, cancellationToken);
    }

    private string BuildProgrammeDetailsUrl(long advertiserId, string relationship) =>
        BuildUrl(
            $"publishers/{Uri.EscapeDataString(options.Value.PublisherId)}/programmedetails",
            ("advertiserId", advertiserId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            ("relationship", relationship));

    private string BuildUrl(string path, params (string Key, string Value)[] query)
    {
        var baseUri = new Uri(new Uri(options.Value.BaseUrl.TrimEnd('/') + "/"), path);
        var queryString = string.Join(
            "&",
            query.Select(value => $"{Uri.EscapeDataString(value.Key)}={Uri.EscapeDataString(value.Value)}"));

        return queryString.Length == 0
            ? baseUri.ToString()
            : $"{baseUri}?{queryString}";
    }

    private void ConfigureClient(AwinAffiliateProgrammeDiscoveryOptions settings)
    {
        httpClient.BaseAddress = null;
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", settings.AccessToken);
        httpClient.DefaultRequestHeaders.Accept.Clear();
        httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        string operation,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new HttpRequestException(
            $"Awin {operation} failed with HTTP {(int)response.StatusCode}: {body}");
    }

    private static bool MatchesRetailer(AwinProgramme programme, string retailerHost)
    {
        if (programme.ValidDomains.Any(domain => SameHost(domain.Domain, retailerHost)))
            return true;

        return TryGetHttpUri(programme.DisplayUrl, out var displayUri)
            && SameHost(displayUri!.Host, retailerHost);
    }

    private static AffiliateProgrammeStatus MapStatus(string? membershipStatus) =>
        membershipStatus?.Trim().ToLowerInvariant() switch
        {
            "joined" => AffiliateProgrammeStatus.Approved,
            "pending" => AffiliateProgrammeStatus.ApplicationSubmitted,
            "notjoined" => AffiliateProgrammeStatus.ApplicationRequired,
            _ => AffiliateProgrammeStatus.ProgrammeAvailable
        };

    private static bool IsApplicationRequired(string? membershipStatus) =>
        string.Equals(membershipStatus?.Trim(), "notjoined", StringComparison.OrdinalIgnoreCase);

    private static List<string> ParseCountryCodes(string value) =>
        value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => value.ToUpperInvariant())
            .Where(value => value.Length == 2)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static bool TryGetHttpUri(string? value, out Uri? uri)
    {
        uri = null;
        return !string.IsNullOrWhiteSpace(value)
            && Uri.TryCreate(value.Trim(), UriKind.Absolute, out uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private static bool SameHost(string first, string second) =>
        NormalizeHost(first) == NormalizeHost(second);

    private static string NormalizeHost(string host)
    {
        var normalized = host.Trim().TrimEnd('.').ToLowerInvariant();
        return normalized.StartsWith("www.", StringComparison.Ordinal)
            ? normalized[4..]
            : normalized;
    }

    private sealed record AwinProgrammeCandidate(
        AwinProgramme Programme,
        string Relationship);

    private sealed record AwinProgramme(
        [property: JsonPropertyName("id")] long Id,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("displayUrl")] string? DisplayUrl,
        [property: JsonPropertyName("validDomains")] IReadOnlyList<AwinDomain> ValidDomains);

    private sealed record AwinDomain(
        [property: JsonPropertyName("domain")] string Domain);

    private sealed record AwinProgrammeDetails(
        [property: JsonPropertyName("programmeInfo")] AwinProgrammeInfo? ProgrammeInfo);

    private sealed record AwinProgrammeInfo(
        [property: JsonPropertyName("id")] long Id,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("displayUrl")] string? DisplayUrl,
        [property: JsonPropertyName("membershipStatus")] string? MembershipStatus,
        [property: JsonPropertyName("deeplinkEnabled")] bool? DeeplinkEnabled);
}
