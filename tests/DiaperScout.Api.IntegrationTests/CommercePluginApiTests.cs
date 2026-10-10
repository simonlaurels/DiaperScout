using System.Net;
using System.Net.Http.Json;
using DiaperScout.Application;
using DiaperScout.Commerce.Plugins.Awin;
using DiaperScout.Domain;
using DiaperScout.Infrastructure;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DiaperScout.Api.IntegrationTests;

public sealed class CommercePluginApiTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    internal const string Secret = "test-token-never-expose-7ab8";

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData(PostgreSqlFixture.ExplorerSubject, HttpStatusCode.Forbidden)]
    [InlineData(PostgreSqlFixture.ModeratorSubject, HttpStatusCode.OK)]
    [InlineData(PostgreSqlFixture.AdministratorSubject, HttpStatusCode.OK)]
    public async Task Management_requires_privilege_and_exposes_only_safe_metadata(string? subject, HttpStatusCode expected)
    {
        using var basis = new ObservationApiFactory(fixture);
        using var factory = basis.WithWebHostBuilder(b => b.ConfigureTestServices(s =>
            s.Configure<AwinAffiliateProgrammeDiscoveryOptions>(o => o.AccessToken = Secret)));
        using var client = factory.CreateClient();
        if (subject is not null) client.DefaultRequestHeaders.Add("X-Development-Subject", subject);
        var response = await client.GetAsync("/api/v1/commerce-plugins/");
        Assert.Equal(expected, response.StatusCode);
        Assert.DoesNotContain(Secret, await response.Content.ReadAsStringAsync());
        if (expected == HttpStatusCode.OK)
        {
            var plugin = Assert.Single((await response.Content.ReadFromJsonAsync<List<CommercePluginStatus>>())!);
            Assert.Equal("awin.affiliate", plugin.Plugin.Id);
            Assert.Equal(CommercePluginFamily.Affiliate, plugin.Plugin.Family);
            Assert.Equal(CommercePluginConfiguration.Ready, plugin.Configuration);
            Assert.Equal("This application instance", plugin.HealthScope);
        }
        var write = await client.PutAsJsonAsync("/api/v1/commerce-plugins/awin.affiliate/enabled", new CommercePluginEnabledRequest(true));
        Assert.Equal(expected, write.StatusCode);
    }

    [Fact]
    public async Task Toggle_persists_only_boolean_and_deployment_disable_cannot_be_bypassed()
    {
        using var basis = new ObservationApiFactory(fixture);
        using var client = RetailListingApiTests.Moderator(basis);
        try
        {
            Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync("/api/v1/commerce-plugins/awin.affiliate/enabled", new CommercePluginEnabledRequest(false))).StatusCode);
            Assert.False(Assert.Single((await client.GetFromJsonAsync<List<CommercePluginStatus>>("/api/v1/commerce-plugins/"))!).Enabled);
            await using var db = fixture.CreateDbContext();
            Assert.Equal("False", (await db.PlatformSettings.SingleAsync(x => x.Key == "commerce.plugin.awin.affiliate.enabled")).Value);
            Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync("/api/v1/commerce-plugins/unknown/enabled", new CommercePluginEnabledRequest(true))).StatusCode);
            using var disabled = basis.WithWebHostBuilder(b => b.ConfigureTestServices(s => s.Configure<CommercePluginOptions>(o =>
                o.Plugins["awin.affiliate"] = new() { Enabled = false })));
            using var disabledClient = Moderator(disabled);
            Assert.Equal(HttpStatusCode.BadRequest, (await disabledClient.PutAsJsonAsync("/api/v1/commerce-plugins/awin.affiliate/enabled", new CommercePluginEnabledRequest(true))).StatusCode);
        }
        finally { (await client.PutAsJsonAsync("/api/v1/commerce-plugins/awin.affiliate/enabled", new CommercePluginEnabledRequest(true))).EnsureSuccessStatusCode(); }
    }

    [Fact]
    public async Task Fake_discovery_is_associated_deduplicated_and_reviewed_without_verification_or_catalogue_mutation()
    {
        using var basis = new ObservationApiFactory(fixture);
        using var client = RetailListingApiTests.Moderator(basis);
        var retailer = await RetailListingApiTests.CreateRetailerAsync(client, true);
        var fake = new Discovery(retailer);
        using var factory = basis.WithWebHostBuilder(b => b.ConfigureTestServices(s => s.AddSingleton<ICommercePlugin>(fake)));
        using var scope = factory.Services.CreateScope();
        var commerce = scope.ServiceProvider.GetRequiredService<ICanonicalCommerce>();
        var first = await commerce.DiscoverListingsAsync(fixture.PackTypeId);
        var listing = Assert.Single(first.Listings);
        Assert.Equal(RetailerProductDiscoveryStatus.Discovered, listing.Status);
        Assert.Equal(fake.Descriptor.Id, listing.DiscoveryProvider);
        Assert.Equal("external-test", listing.ExternalListingId);
        Assert.Equal(retailer.WebsiteUrl + "evidence", listing.SourceUrl);
        Assert.Equal(listing.Id, Assert.Single((await commerce.DiscoverListingsAsync(fixture.PackTypeId)).Listings).Id);
        fake.Quantity = 99;
        var mismatch = await commerce.DiscoverListingsAsync(fixture.PackTypeId);
        Assert.Empty(mismatch.Listings);
        Assert.Equal("PackQuantityMismatch", Assert.Single(mismatch.Review).Code);
        fake.Quantity = 12;
        fake.Identifier = "87654321";
        Assert.Equal("IdentifierMismatch", Assert.Single((await commerce.DiscoverListingsAsync(fixture.PackTypeId)).Review).Code);
        fake.Identifier = "12345678";
        fake.Website = "https://unknown-retailer.test";
        Assert.Equal("RetailerNotAssociated", Assert.Single((await commerce.DiscoverListingsAsync(fixture.PackTypeId)).Review).Code);
        await using var db = fixture.CreateDbContext();
        Assert.Equal(1, await db.RetailerProductListings.CountAsync(l => l.RetailerId == retailer.Id));
        Assert.Equal(1, await db.Products.CountAsync(p => p.Id == fixture.ProductId));
    }

    [Fact]
    public async Task WhereToBuy_uses_fake_affiliate_for_manual_exact_pack_and_failure_falls_back_with_safe_health()
    {
        using var basis = new ObservationApiFactory(fixture);
        using var moderator = RetailListingApiTests.Moderator(basis);
        var retailer = await RetailListingApiTests.CreateRetailerAsync(moderator, true);
        var listing = new RetailerProductListing(fixture.PackTypeId, retailer.Id, retailer.WebsiteUrl + "manual", "Manual");
        listing.MarkVerified();
        var programme = new RetailerAffiliateProgramme(retailer.Id, "Test Network", "42", "Test", AffiliateProgrammeStatus.Configured, deepLinksAllowed: true);
        programme.MarkPreferred();
        await using (var db = fixture.CreateDbContext()) { db.AddRange(listing, programme); await db.SaveChangesAsync(); }
        var fake = new Affiliate();
        using var factory = basis.WithWebHostBuilder(b => b.ConfigureTestServices(s => s.AddSingleton<ICommercePlugin>(fake)));
        using var client = Moderator(factory);
        var url = $"/api/v1/products/integration-test-product?packTypeId={fixture.PackTypeId}";
        var offer = Assert.Single((await client.GetFromJsonAsync<CatalogueProductDetails>(url))!.RetailOffers, o => o.Id == listing.Id);
        Assert.True(offer.IsAffiliateBacked);
        Assert.Equal("https://affiliate.test/outbound", offer.DestinationUrl);
        using (var scope = factory.Services.CreateScope())
        {
            var commerce = scope.ServiceProvider.GetRequiredService<ICanonicalCommerce>();
            Assert.Equal("test.affiliate", (await commerce.ResolveAffiliateAsync(listing.Id)).Provenance!.PluginId);
        }
        (await client.PutAsJsonAsync("/api/v1/commerce-plugins/test.affiliate/enabled", new CommercePluginEnabledRequest(false))).EnsureSuccessStatusCode();
        offer = Assert.Single((await client.GetFromJsonAsync<CatalogueProductDetails>(url))!.RetailOffers, o => o.Id == listing.Id);
        Assert.Equal(listing.ListingUrl, offer.DestinationUrl);
        Assert.False(offer.IsAffiliateBacked);
        (await client.PutAsJsonAsync("/api/v1/commerce-plugins/test.affiliate/enabled", new CommercePluginEnabledRequest(true))).EnsureSuccessStatusCode();
        fake.Fail = true;
        offer = Assert.Single((await client.GetFromJsonAsync<CatalogueProductDetails>(url))!.RetailOffers, o => o.Id == listing.Id);
        Assert.Equal(listing.ListingUrl, offer.DestinationUrl);
        var metadataResponse = await client.GetAsync("/api/v1/commerce-plugins/");
        Assert.DoesNotContain(Secret, await metadataResponse.Content.ReadAsStringAsync());
        var health = (await metadataResponse.Content.ReadFromJsonAsync<List<CommercePluginStatus>>())!.Single(p => p.Plugin.Id == "test.affiliate");
        Assert.Equal("Degraded", health.Health);
        Assert.Equal("Failed", health.LastFailureCode);
        Assert.NotNull(health.LastSuccessAtUtc);
        Assert.NotNull(health.LastFailureAtUtc);
        await using var finalDb = fixture.CreateDbContext();
        Assert.Equal("Manual", (await finalDb.RetailerProductListings.SingleAsync(l => l.Id == listing.Id)).DiscoveryProvider);
    }

    internal static HttpClient Moderator(Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Development-Subject", PostgreSqlFixture.ModeratorSubject);
        client.DefaultRequestHeaders.Add("X-Development-Role", "Moderator");
        return client;
    }

    private sealed class Discovery(RetailerManagementItem retailer) : IRetailDiscoveryPlugin
    {
        public int Quantity = 12;
        public string Identifier = "12345678";
        public string Website = retailer.WebsiteUrl!;
        public CommercePluginDescriptor Descriptor => new("test.discovery", "Test discovery", CommercePluginFamily.RetailDiscovery, "1.0");
        public CommercePluginConfiguration Configuration => CommercePluginConfiguration.Ready;
        public bool CanHandle(CanonicalSellableItem item) => true;
        public Task<IReadOnlyList<RetailDiscoveryObservation>> DiscoverAsync(CanonicalSellableItem item, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<RetailDiscoveryObservation>>([new(retailer.WebsiteUrl + "discovered", new(retailer.Name, Website, retailer.WebsiteUrl + "evidence"),
                [new(IdentifierType.Gtin, Identifier)], DateTimeOffset.UtcNow, "external-test", QuantityPerPack: Quantity)]);
    }
    private sealed class Affiliate : IAffiliatePlugin
    {
        public bool Fail;
        public CommercePluginDescriptor Descriptor => new("test.affiliate", "Test Affiliate", CommercePluginFamily.Affiliate, "1.0");
        public CommercePluginConfiguration Configuration => CommercePluginConfiguration.Ready;
        public bool CanHandle(CanonicalCommerceListing listing) => listing.AffiliateProgrammes.Any(p => p.Network == "Test Network");
        public Task<AffiliateObservation?> ResolveAsync(CanonicalCommerceListing listing, CancellationToken ct) => Fail
            ? throw new InvalidOperationException(Secret)
            : Task.FromResult<AffiliateObservation?>(new("https://affiliate.test/outbound", "Test Network", listing.AffiliateProgrammes.Single(p => p.Network == "Test Network").Id, DateTimeOffset.UtcNow));
    }
}
