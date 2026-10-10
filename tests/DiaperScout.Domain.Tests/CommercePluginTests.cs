using DiaperScout.Application;
using DiaperScout.Commerce.Plugins.Awin;
using DiaperScout.Domain;
using DiaperScout.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace DiaperScout.Domain.Tests;

public sealed class CommercePluginTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly CanonicalSellableItem Item = new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
        "Product", "Brand", "Blue", "Medium", 12, PackagingType.Bag, [new(IdentifierType.Gtin, "12345678")]);
    private static readonly CommerceAffiliateProgramme Programme = new(Guid.NewGuid(), "Fake", "42", AffiliateProgrammeStatus.Configured, true, true);
    private static readonly CanonicalCommerceListing Listing = new(Guid.NewGuid(), Item, Guid.NewGuid(), "Retailer",
        "https://retailer.test", "https://retailer.test/item?size=M&pack=12", "external-42", [Programme]);

    [Fact]
    public void Registration_discovers_separate_families_and_is_idempotent()
    {
        var services = new ServiceCollection();
        services.AddCommercePlugin<Discovery>().AddCommercePlugin<Pricing>().AddCommercePlugin<Availability>().AddCommercePlugin<Affiliate>().AddCommercePlugin<Affiliate>();
        using var provider = services.BuildServiceProvider();
        var registry = new CommercePluginRegistry(provider.GetServices<ICommercePlugin>());
        Assert.Equal(4, registry.Plugins.Count);
        Assert.Equal(4, registry.Plugins.Select(p => p.Descriptor.Family).Distinct().Count());
        Assert.Equal(new[] { "fake.affiliate", "fake.availability", "fake.discovery", "fake.pricing" }, registry.Plugins.Select(p => p.Descriptor.Id));
    }

    [Fact]
    public void Registry_rejects_duplicate_identity_wrong_family_and_monolithic_plugins()
    {
        Assert.Throws<InvalidOperationException>(() => new CommercePluginRegistry([new Affiliate(), new Affiliate()]));
        Assert.Throws<InvalidOperationException>(() => new CommercePluginRegistry([new Affiliate { Family = CommercePluginFamily.Pricing }]));
        Assert.Throws<InvalidOperationException>(() => new CommercePluginRegistry([new Monolithic()]));
        Assert.Throws<InvalidOperationException>(() => new CommercePluginRegistry([new Affiliate { Id = "Invalid ID" }]));
    }

    [Fact]
    public async Task Fake_plugins_supply_all_four_families_with_application_stamped_provenance()
    {
        var orchestration = Create(new Discovery(), new Pricing(), new Availability(), new Affiliate());
        var discovered = Assert.Single((await orchestration.DiscoverAsync(Item)).Observations);
        Assert.Equal("fake.discovery", discovered.Provenance.PluginId);
        Assert.Equal("1.2.3", discovered.Provenance.PluginVersion);
        Assert.Equal(Now, discovered.Provenance.ObservedAtUtc);
        Assert.True(discovered.Provenance.RefreshedAtUtc >= Now);
        Assert.Equal("external-42", discovered.Provenance.ExternalListingId);
        Assert.Equal("https://retailer.test/evidence", discovered.Provenance.SourceUrl);
        Assert.Equal(12, discovered.Value.QuantityPerPack);
        var price = Assert.Single((await orchestration.RefreshPriceAsync(Listing)).Observations);
        Assert.Equal(19.99m, price.Value.Amount);
        Assert.Equal("GBP", price.Value.Currency);
        Assert.Equal(Listing.ExternalListingId, price.Provenance.ExternalListingId);
        Assert.Equal(RetailerProductAvailability.Limited, Assert.Single((await orchestration.RefreshAvailabilityAsync(Listing)).Observations).Value.Availability);
        var affiliate = await orchestration.ResolveAffiliateAsync(Listing);
        Assert.True(affiliate.IsAffiliateBacked);
        Assert.Equal("https://affiliate.test/go", affiliate.Url);
        Assert.Equal("fake.affiliate", affiliate.Provenance!.PluginId);
    }

    [Theory]
    [InlineData(CommercePluginExecutionStatus.Disabled)]
    [InlineData(CommercePluginExecutionStatus.MissingConfiguration)]
    [InlineData(CommercePluginExecutionStatus.Unsupported)]
    public async Task Ineligible_plugins_are_reported_without_invocation(CommercePluginExecutionStatus expected)
    {
        var plugin = new Affiliate { Supported = expected != CommercePluginExecutionStatus.Unsupported,
            Configuration = expected == CommercePluginExecutionStatus.MissingConfiguration ? CommercePluginConfiguration.MissingConfiguration : CommercePluginConfiguration.Ready };
        var runtime = new Runtime { Enabled = expected != CommercePluginExecutionStatus.Disabled };
        var result = await Create(runtime, plugin).ResolveAffiliateAsync(Listing);
        Assert.Equal(expected, Assert.Single(result.Executions).Status);
        Assert.Equal(Listing.ListingUrl, result.Url);
        Assert.Equal(0, plugin.Calls);
        Assert.Equal(result.Executions, runtime.Executions);
    }

    [Fact]
    public async Task Failures_are_isolated_and_selection_is_priority_then_stable_identity()
    {
        var broken = new Affiliate { Id = "fake.a", Throw = true };
        var winner = new Affiliate { Id = "fake.b" };
        var never = new Affiliate { Id = "fake.c" };
        var runtime = new Runtime();
        var result = await Create(runtime, never, winner, broken).ResolveAffiliateAsync(Listing);
        Assert.True(result.IsAffiliateBacked);
        Assert.Equal(new[] { CommercePluginExecutionStatus.Failed, CommercePluginExecutionStatus.Succeeded }, result.Executions.Select(e => e.Status));
        Assert.Equal(0, never.Calls);
        Assert.DoesNotContain("secret", System.Text.Json.JsonSerializer.Serialize(result.Executions));
        runtime.Priorities[never.Id] = -1;
        result = await Create(runtime, winner, never).ResolveAffiliateAsync(Listing);
        Assert.Equal(never.Id, result.Provenance!.PluginId);
    }

    [Fact]
    public async Task Noncooperative_timeout_falls_through_and_caller_cancellation_propagates()
    {
        var slow = new Affiliate { Id = "fake.a", Delay = true };
        var result = await Create(new Runtime { Timeout = TimeSpan.FromMilliseconds(20) }, slow, new Affiliate()).ResolveAffiliateAsync(Listing);
        Assert.Equal(CommercePluginExecutionStatus.TimedOut, result.Executions[0].Status);
        Assert.True(result.IsAffiliateBacked);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Create(slow).ResolveAffiliateAsync(Listing, cancellation.Token));
    }

    [Fact]
    public async Task Invalid_results_cannot_enter_normalised_batches()
    {
        var invalid = new Affiliate { Id = "fake.a", Destination = "javascript:alert(1)" };
        var result = await Create(invalid, new Affiliate()).ResolveAffiliateAsync(Listing);
        Assert.Equal(CommercePluginExecutionStatus.InvalidResult, result.Executions[0].Status);
        Assert.True(result.IsAffiliateBacked);
        Assert.Empty((await Create(new Pricing { Amount = -1 }).RefreshPriceAsync(Listing)).Observations);
        Assert.Empty((await Create(new Pricing { Currency = "gbp" }).RefreshPriceAsync(Listing)).Observations);
        Assert.Empty((await Create(new Availability { State = (RetailerProductAvailability)999 }).RefreshAvailabilityAsync(Listing)).Observations);
        Assert.Empty((await Create(new Discovery { Url = "https://user:password@retailer.test/item" }).DiscoverAsync(Item)).Observations);
    }

    [Fact]
    public async Task Preferred_verified_relationship_is_required_and_no_provider_preserves_ordinary_url()
    {
        foreach (var context in new[] { Listing with { AffiliateProgrammes = [] }, Listing with { AffiliateProgrammes = [Programme with { IsPreferred = false }] },
            Listing with { AffiliateProgrammes = [Programme with { DeepLinksAllowed = false }] }, Listing with { ListingStatus = RetailerProductDiscoveryStatus.Discovered },
            Listing with { RetailerStatus = RetailerStatus.Discovered } })
        {
            var result = await Create(new Affiliate()).ResolveAffiliateAsync(context);
            Assert.False(result.IsAffiliateBacked);
            Assert.Equal(Listing.ListingUrl, result.Url);
        }
        Assert.Equal(Listing.ListingUrl, (await Create().ResolveAffiliateAsync(Listing)).Url);
    }

    [Theory]
    [InlineData("999", "123", true, true, true)]
    [InlineData("000999", "000123", true, true, true)]
    [InlineData("999", "123", false, true, false)]
    [InlineData("999", "123", true, false, false)]
    [InlineData("", "123", true, true, false)]
    [InlineData("999", "abc", true, true, false)]
    public async Task Awin_preserves_preference_numeric_validation_and_encoded_destination(string publisher, string advertiser, bool preferred, bool deepLinks, bool expected)
    {
        var options = Options.Create(new AwinAffiliateProgrammeDiscoveryOptions { PublisherId = publisher, Enabled = false, AccessToken = "" });
        var plugin = new AwinAffiliatePlugin(options, new AwinAffiliateProgrammeDiscoveryProvider(new HttpClient(), options), TimeProvider.System);
        var listing = Listing with { AffiliateProgrammes = [Programme with { Network = "aWiN", ProgrammeId = advertiser, IsPreferred = preferred, DeepLinksAllowed = deepLinks }] };
        var result = await Create(plugin).ResolveAffiliateAsync(listing);
        Assert.Equal(expected, result.IsAffiliateBacked);
        if (expected) Assert.Equal("https://www.awin1.com/cread.php?awinmid=123&awinaffid=999&ued=" + Uri.EscapeDataString(Listing.ListingUrl), result.Url);
        else Assert.Equal(Listing.ListingUrl, result.Url);
        Assert.Equal(CommercePluginConfiguration.DisabledByConfiguration, plugin.ProgrammeDiscoveryConfiguration);
    }

    [Fact]
    public async Task Multiple_pricing_plugins_return_valid_observations_despite_a_failed_provider()
    {
        var batch = await Create(new Pricing { Id = "fake.a", Throw = true }, new Pricing { Id = "fake.b" }, new Pricing { Id = "fake.c" }).RefreshPriceAsync(Listing);
        Assert.Equal(2, batch.Observations.Count);
        Assert.Equal(new[] { "fake.b", "fake.c" }, batch.Observations.Select(o => o.Provenance.PluginId));
        Assert.Equal(CommercePluginExecutionStatus.Failed, batch.Executions[0].Status);
    }

    [Fact]
    public async Task Cancellation_during_an_active_call_propagates_without_becoming_a_provider_failure()
    {
        using var cancellation = new CancellationTokenSource();
        var runtime = new Runtime();
        var task = Create(runtime, new Affiliate { Delay = true }).ResolveAffiliateAsync(Listing, cancellation.Token);
        cancellation.CancelAfter(20);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        Assert.Empty(runtime.Executions);
    }

    [Fact]
    public async Task Expired_future_and_malformed_evidence_are_rejected()
    {
        foreach (var plugin in new[] {
            new Pricing { Expiry = Now.AddSeconds(-1) }, new Pricing { Observed = Now.AddHours(1) },
            new Pricing { Source = "file:///credentials" }, new Pricing { ExternalId = new string('x', 301) } })
        {
            var batch = await Create(plugin).RefreshPriceAsync(Listing);
            Assert.Empty(batch.Observations);
            Assert.Equal(CommercePluginExecutionStatus.InvalidResult, Assert.Single(batch.Executions).Status);
        }
    }

    [Fact]
    public async Task Optional_programme_discovery_uses_its_own_configuration_and_isolates_failures()
    {
        var target = new RetailerAffiliateProgrammeDiscoveryTarget(Listing.RetailerId, Listing.RetailerName, Listing.RetailerWebsiteUrl);
        var batch = await Create(new ProgrammeDiscovery { Id = "fake.a", Throw = true }, new ProgrammeDiscovery { Id = "fake.b" }, new Affiliate()).DiscoverAffiliateProgrammesAsync(target);
        Assert.Equal(2, batch.Executions.Count);
        Assert.Equal(CommercePluginExecutionStatus.Failed, batch.Executions[0].Status);
        Assert.Equal("fake.b", Assert.Single(batch.Observations).Provenance.PluginId);
        batch = await Create(new ProgrammeDiscovery { ProgrammeDiscoveryConfiguration = CommercePluginConfiguration.MissingConfiguration }).DiscoverAffiliateProgrammesAsync(target);
        Assert.Equal(CommercePluginExecutionStatus.MissingConfiguration, Assert.Single(batch.Executions).Status);
    }

    public sealed class ProgrammeDiscovery : Affiliate, IAffiliateProgrammeDiscoveryPlugin
    {
        public CommercePluginConfiguration ProgrammeDiscoveryConfiguration { get; set; } = CommercePluginConfiguration.Ready;
        public bool CanDiscoverProgrammes(RetailerAffiliateProgrammeDiscoveryTarget target) => true;
        public Task<IReadOnlyList<RetailerAffiliateProgrammeDiscoveryCandidate>> DiscoverProgrammesAsync(RetailerAffiliateProgrammeDiscoveryTarget target, CancellationToken ct) => Throw
            ? throw new InvalidOperationException("secret")
            : Task.FromResult<IReadOnlyList<RetailerAffiliateProgrammeDiscoveryCandidate>>([new("Fake", "42", "Test programme", AffiliateProgrammeStatus.ProgrammeAvailable,
                null, null, null, 30, true, true, "https://affiliate.test/evidence")]);
    }

    private static CommercePluginOrchestrator Create(params ICommercePlugin[] plugins) => Create(new Runtime(), plugins);
    private static CommercePluginOrchestrator Create(Runtime runtime, params ICommercePlugin[] plugins) => new(new CommercePluginRegistry(plugins), runtime, TimeProvider.System);
    private sealed class Runtime : ICommercePluginRuntime
    {
        public bool Enabled = true;
        public TimeSpan Timeout = TimeSpan.FromSeconds(1);
        public Dictionary<string, int> Priorities = [];
        public List<CommercePluginExecution> Executions = [];
        public Task<CommercePluginPolicy> GetPolicyAsync(string id, CancellationToken ct) => Task.FromResult(new CommercePluginPolicy(Enabled, Priorities.GetValueOrDefault(id), Timeout));
        public void Record(CommercePluginExecution execution) => Executions.Add(execution);
    }
    public class Affiliate : IAffiliatePlugin
    {
        public string Id { get; set; } = "fake.affiliate";
        public CommercePluginFamily Family { get; set; } = CommercePluginFamily.Affiliate;
        public CommercePluginDescriptor Descriptor => new(Id, "Fake Affiliate", Family, "1.2.3");
        public CommercePluginConfiguration Configuration { get; set; } = CommercePluginConfiguration.Ready;
        public bool Supported = true, Throw, Delay;
        public int Calls;
        public string Destination = "https://affiliate.test/go";
        public bool CanHandle(CanonicalCommerceListing listing) => Supported;
        public async Task<AffiliateObservation?> ResolveAsync(CanonicalCommerceListing listing, CancellationToken ct)
        {
            Calls++;
            if (Throw) throw new InvalidOperationException("secret credentials must never escape");
            if (Delay) await Task.Delay(200);
            return new(Destination, "Fake", Programme.Id, Now, Listing.ListingUrl);
        }
    }
    public sealed class Monolithic : Affiliate, IPricingPlugin
    { public Task<PriceObservation?> ObservePriceAsync(CanonicalCommerceListing listing, CancellationToken ct) => Task.FromResult<PriceObservation?>(null); }
    public sealed class Pricing : IPricingPlugin
    {
        public string Id = "fake.pricing";
        public bool Throw;
        public DateTimeOffset Observed = Now;
        public DateTimeOffset? Expiry;
        public string Source = Listing.ListingUrl;
        public string? ExternalId;
        public CommercePluginDescriptor Descriptor => new(Id, "Fake Pricing", CommercePluginFamily.Pricing, "1.2.3");
        public CommercePluginConfiguration Configuration => CommercePluginConfiguration.Ready;
        public decimal Amount = 19.99m;
        public string Currency = "GBP";
        public bool CanHandle(CanonicalCommerceListing listing) => true;
        public Task<PriceObservation?> ObservePriceAsync(CanonicalCommerceListing listing, CancellationToken ct) => Throw ? throw new InvalidOperationException("secret") : Task.FromResult<PriceObservation?>(new(Amount, Currency, Observed, Source, ExternalId, Expiry));
    }
    public sealed class Availability : IAvailabilityPlugin
    {
        public CommercePluginDescriptor Descriptor => new("fake.availability", "Fake Availability", CommercePluginFamily.Availability, "1.2.3");
        public CommercePluginConfiguration Configuration => CommercePluginConfiguration.Ready;
        public RetailerProductAvailability State = RetailerProductAvailability.Limited;
        public bool CanHandle(CanonicalCommerceListing listing) => true;
        public Task<AvailabilityObservation?> ObserveAvailabilityAsync(CanonicalCommerceListing listing, CancellationToken ct) => Task.FromResult<AvailabilityObservation?>(new(State, Now, Listing.ListingUrl));
    }
    public sealed class Discovery : IRetailDiscoveryPlugin
    {
        public CommercePluginDescriptor Descriptor => new("fake.discovery", "Fake Discovery", CommercePluginFamily.RetailDiscovery, "1.2.3");
        public CommercePluginConfiguration Configuration => CommercePluginConfiguration.Ready;
        public string Url = "https://retailer.test/item";
        public bool CanHandle(CanonicalSellableItem item) => true;
        public Task<IReadOnlyList<RetailDiscoveryObservation>> DiscoverAsync(CanonicalSellableItem item, CancellationToken ct) => Task.FromResult<IReadOnlyList<RetailDiscoveryObservation>>(
            [new(Url, new("Retailer", "https://retailer.test", "https://retailer.test/evidence"), item.Identifiers, Now, "external-42", QuantityPerPack: 12)]);
    }
}
