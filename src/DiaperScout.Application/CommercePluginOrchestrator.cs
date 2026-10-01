using System.Text.RegularExpressions;
using DiaperScout.Domain;

namespace DiaperScout.Application;

public sealed class CommercePluginOrchestrator(ICommercePluginRegistry registry, ICommercePluginRuntime runtime,
    TimeProvider clock) : ICommercePluginOrchestrator
{
    public Task<CommercePluginBatch<RetailDiscoveryObservation>> DiscoverAsync(CanonicalSellableItem item, CancellationToken cancellationToken = default) =>
        RunAsync<IRetailDiscoveryPlugin, RetailDiscoveryObservation>(p => p.CanHandle(item),
            (p, ct) => p.DiscoverAsync(item, ct),
            o => HttpUrl(o.ListingUrl) && OptionalUrl(o.SourceUrl) && OptionalUrl(o.Retailer.WebsiteUrl)
                && OptionalUrl(o.Retailer.EvidenceUrl) && !string.IsNullOrWhiteSpace(o.Retailer.Name)
                && o.Retailer.Name.Length <= 200 && TimeValid(o.ObservedAtUtc) && OptionalText(o.ExternalListingId, 300)
                && OptionalText(o.Title, 1000) && (o.QuantityPerPack is null or > 0)
                && (o.MatchConfidence is null or >= 0 and <= 1) && o.ObservedIdentifiers.Count <= 20
                && o.ObservedIdentifiers.All(i => Enum.IsDefined(i.Type) && !string.IsNullOrWhiteSpace(i.Value) && i.Value.Length <= 200),
            o => (o.ObservedAtUtc, o.ExternalListingId, o.SourceUrl ?? o.Retailer.EvidenceUrl ?? o.ListingUrl), cancellationToken);

    public Task<CommercePluginBatch<PriceObservation>> RefreshPriceAsync(CanonicalCommerceListing listing, CancellationToken cancellationToken = default) =>
        RunAsync<IPricingPlugin, PriceObservation>(p => p.CanHandle(listing),
            async (p, ct) => One(await p.ObservePriceAsync(listing, ct)),
            o => o.Amount >= 0 && Regex.IsMatch(o.Currency, "^[A-Z]{3}$") && TimeValid(o.ObservedAtUtc)
                && HttpUrl(o.SourceUrl) && OptionalText(o.ExternalListingId, 300) && ExpiryValid(o.ObservedAtUtc, o.ValidUntilUtc),
            o => (o.ObservedAtUtc, o.ExternalListingId ?? listing.ExternalListingId, o.SourceUrl), cancellationToken);

    public Task<CommercePluginBatch<AvailabilityObservation>> RefreshAvailabilityAsync(CanonicalCommerceListing listing, CancellationToken cancellationToken = default) =>
        RunAsync<IAvailabilityPlugin, AvailabilityObservation>(p => p.CanHandle(listing),
            async (p, ct) => One(await p.ObserveAvailabilityAsync(listing, ct)),
            o => Enum.IsDefined(o.Availability) && TimeValid(o.ObservedAtUtc) && HttpUrl(o.SourceUrl)
                && OptionalText(o.ExternalListingId, 300) && ExpiryValid(o.ObservedAtUtc, o.ValidUntilUtc),
            o => (o.ObservedAtUtc, o.ExternalListingId ?? listing.ExternalListingId, o.SourceUrl), cancellationToken);

    public async Task<CommerceAffiliateDestination> ResolveAffiliateAsync(CanonicalCommerceListing listing, CancellationToken cancellationToken = default)
    {
        if (listing.RetailerStatus != RetailerStatus.Verified || listing.ListingStatus != RetailerProductDiscoveryStatus.Verified)
            return new(listing.ListingUrl, null, false, null, []);
        var batch = await RunAsync<IAffiliatePlugin, AffiliateObservation>(p => p.CanHandle(listing),
            async (p, ct) => One(await p.ResolveAsync(listing, ct)),
            o => HttpUrl(o.DestinationUrl) && !string.IsNullOrWhiteSpace(o.Network) && o.Network.Length <= 100
                && OptionalUrl(o.SourceUrl) && TimeValid(o.ResolvedAtUtc)
                && listing.AffiliateProgrammes.Any(a => a.Id == o.ProgrammeId && a.IsPreferred
                    && a.Status == AffiliateProgrammeStatus.Configured && a.DeepLinksAllowed != false
                    && string.Equals(a.Network, o.Network, StringComparison.OrdinalIgnoreCase)),
            o => (o.ResolvedAtUtc, listing.ExternalListingId, o.SourceUrl ?? listing.ListingUrl), cancellationToken, stopOnFirst: true);
        var best = batch.Observations.FirstOrDefault();
        return best is null
            ? new(listing.ListingUrl, null, false, null, batch.Executions)
            : new(best.Value.DestinationUrl, best.Value.Network, true, best.Provenance, batch.Executions);
    }

    public Task<CommercePluginBatch<RetailerAffiliateProgrammeDiscoveryCandidate>> DiscoverAffiliateProgrammesAsync(
        RetailerAffiliateProgrammeDiscoveryTarget retailer, CancellationToken cancellationToken = default) =>
        RunAsync<IAffiliateProgrammeDiscoveryPlugin, RetailerAffiliateProgrammeDiscoveryCandidate>(p => p.CanDiscoverProgrammes(retailer),
            (p, ct) => p.DiscoverProgrammesAsync(retailer, ct),
            o => !string.IsNullOrWhiteSpace(o.Network) && o.Network.Length <= 100
                && !string.IsNullOrWhiteSpace(o.ProgrammeId) && o.ProgrammeId.Length <= 200
                && !string.IsNullOrWhiteSpace(o.ProgrammeName) && o.ProgrammeName.Length <= 300 && Enum.IsDefined(o.Status)
                && OptionalUrl(o.SourceUrl) && OptionalUrl(o.ProgrammeUrl) && OptionalUrl(o.TermsUrl)
                && OptionalText(o.ReferralTerms, 4000) && (o.CookieDurationDays is null or >= 0),
            o => (clock.GetUtcNow(), o.ProgrammeId, o.SourceUrl), cancellationToken,
            configuration: p => p.ProgrammeDiscoveryConfiguration);

    private async Task<CommercePluginBatch<T>> RunAsync<TPlugin, T>(Func<TPlugin, bool> supports,
        Func<TPlugin, CancellationToken, Task<IReadOnlyList<T>>> invoke, Func<T, bool> valid,
        Func<T, (DateTimeOffset Observed, string? ExternalId, string? Source)> evidence, CancellationToken ct,
        bool stopOnFirst = false, Func<TPlugin, CommercePluginConfiguration>? configuration = null)
        where TPlugin : ICommercePlugin
    {
        var installed = new List<(TPlugin Plugin, CommercePluginPolicy Policy)>();
        foreach (var plugin in registry.Plugins.OfType<TPlugin>())
        {
            ct.ThrowIfCancellationRequested();
            installed.Add((plugin, await runtime.GetPolicyAsync(plugin.Descriptor.Id, ct)));
        }
        var results = new List<SourcedCommerceObservation<T>>();
        var executions = new List<CommercePluginExecution>();
        foreach (var (plugin, policy) in installed.OrderBy(p => p.Policy.Priority).ThenBy(p => p.Plugin.Descriptor.Id, StringComparer.Ordinal))
        {
            ct.ThrowIfCancellationRequested();
            CommercePluginExecutionStatus status;
            try
            {
                var configured = policy.Enabled ? (configuration is null ? plugin.Configuration : configuration(plugin)) : CommercePluginConfiguration.DisabledByConfiguration;
                if (!policy.Enabled || configured == CommercePluginConfiguration.DisabledByConfiguration)
                    status = CommercePluginExecutionStatus.Disabled;
                else if (configured != CommercePluginConfiguration.Ready)
                    status = CommercePluginExecutionStatus.MissingConfiguration;
                else if (!supports(plugin)) status = CommercePluginExecutionStatus.Unsupported;
                else
                {
                    var timeout = TimeSpan.FromMilliseconds(Math.Clamp(policy.Timeout.TotalMilliseconds, 10, 120000));
                    using var deadline = new CancellationTokenSource(timeout, clock);
                    using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, deadline.Token);
                    // Bound even a plugin that fails to return promptly or ignores cancellation. Plugins must still cooperate to release their own resources.
                    var values = await Task.Run(() => invoke(plugin, linked.Token), CancellationToken.None).WaitAsync(timeout, clock, ct);
                    ct.ThrowIfCancellationRequested();
                    if (values is null || values.Count > 500 || values.Any(o => o is null || !valid(o)))
                        status = CommercePluginExecutionStatus.InvalidResult;
                    else
                    {
                        foreach (var value in values)
                        {
                            var e = evidence(value);
                            results.Add(new(value, new(plugin.Descriptor.Id, plugin.Descriptor.Version,
                                e.ExternalId, e.Observed, clock.GetUtcNow(), e.Source)));
                        }
                        status = CommercePluginExecutionStatus.Succeeded;
                    }
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (OperationCanceledException) { status = CommercePluginExecutionStatus.TimedOut; }
            catch (TimeoutException) { status = CommercePluginExecutionStatus.TimedOut; }
            catch (Exception) { status = CommercePluginExecutionStatus.Failed; }
            var execution = new CommercePluginExecution(plugin.Descriptor.Id, plugin.Descriptor.Family, status, clock.GetUtcNow());
            executions.Add(execution);
            // Runtime records safe status codes only, never provider exceptions, request headers, URLs, or settings.
            runtime.Record(execution);
            if (stopOnFirst && results.Count > 0) break;
        }
        return new(results, executions);
    }

    private static IReadOnlyList<T> One<T>(T? value) where T : class => value is null ? [] : [value];
    private bool TimeValid(DateTimeOffset timestamp) => timestamp != default && timestamp <= clock.GetUtcNow().AddMinutes(5);
    private bool ExpiryValid(DateTimeOffset observed, DateTimeOffset? expiry) => expiry is null || (expiry >= observed && expiry > clock.GetUtcNow());
    private static bool OptionalText(string? value, int max) => value is null || value.Length <= max;
    public static bool HttpUrl(string? url) => Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && (uri.Scheme == "http" || uri.Scheme == "https") && string.IsNullOrEmpty(uri.UserInfo) && url.Length <= 2000;
    private static bool OptionalUrl(string? url) => url is null || HttpUrl(url);
}
