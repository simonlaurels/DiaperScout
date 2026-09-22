using System.Net;
using System.Net.Http.Json;
using DiaperScout.Application;
using DiaperScout.Domain;
using Xunit;

namespace DiaperScout.Api.IntegrationTests;

public sealed class RetailerDiscoveryApiTests : IClassFixture<PostgreSqlFixture>, IDisposable
{
    private readonly PostgreSqlFixture _fixture;
    private readonly ObservationApiFactory _factory;

    public RetailerDiscoveryApiTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
        _factory = new ObservationApiFactory(fixture);
    }

    [Fact]
    public async Task RetailerDiscovery_RecordsListingAgainstCanonicalGtin()
    {
        using var client = AuthenticatedClient(PostgreSqlFixture.ModeratorSubject);

        var response = await client.PostAsJsonAsync(
            "/api/v1/retailer-discovery",
            new RetailerDiscoveryResult(
                "12345678",
                "Integration Test Retailer",
                "integration-test-retailer",
                "https://retailer.example.test",
                "https://retailer.example.test/products/integration-test",
                "Integration Discovery",
                "https://discovery.example.test/search/12345678",
                "listing-001"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var listing = await response.Content.ReadFromJsonAsync<RetailerProductListingItem>();
        Assert.NotNull(listing);
        Assert.Equal(_fixture.PackTypeId, listing.PackTypeId);
        Assert.Equal(_fixture.RetailerId, listing.RetailerId);
        Assert.Equal("Integration Test Retailer", listing.RetailerName);
        Assert.Equal(RetailerStatus.Discovered, listing.RetailerStatus);
        Assert.Equal("https://retailer.example.test/products/integration-test", listing.ListingUrl);
        Assert.Equal(RetailerProductDiscoveryStatus.Discovered, listing.Status);
        Assert.Equal("Integration Discovery", listing.DiscoveryProvider);
        Assert.Equal("listing-001", listing.ExternalListingId);
    }

    [Fact]
    public async Task RetailerDiscovery_RepeatedResultDoesNotCreateDuplicateListing()
    {
        using var client = AuthenticatedClient(PostgreSqlFixture.ModeratorSubject);
        var request = new RetailerDiscoveryResult(
            "12345678",
            "Integration Test Retailer",
            "integration-test-retailer",
            "https://retailer.example.test",
            "https://retailer.example.test/products/repeat",
            "Integration Discovery",
            null,
            "listing-repeat");

        var first = await client.PostAsJsonAsync("/api/v1/retailer-discovery", request);
        var second = await client.PostAsJsonAsync("/api/v1/retailer-discovery", request with { SourceUrl = "https://discovery.example.test/recheck" });

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        var firstListing = await first.Content.ReadFromJsonAsync<RetailerProductListingItem>();
        var secondListing = await second.Content.ReadFromJsonAsync<RetailerProductListingItem>();
        Assert.NotNull(firstListing);
        Assert.NotNull(secondListing);
        Assert.Equal(firstListing.Id, secondListing.Id);
        Assert.Equal("https://discovery.example.test/recheck", secondListing.SourceUrl);

        var get = await client.GetAsync("/api/v1/retailer-discovery/12345678");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        var listings = await get.Content.ReadFromJsonAsync<IReadOnlyList<RetailerProductListingItem>>();
        Assert.NotNull(listings);
        Assert.Single(listings!, value => value.ListingUrl == "https://retailer.example.test/products/repeat");
    }

    [Fact]
    public async Task RetailerDiscovery_CreatesPreviouslyUnknownRetailer()
    {
        using var client = AuthenticatedClient(PostgreSqlFixture.ModeratorSubject);

        var response = await client.PostAsJsonAsync(
            "/api/v1/retailer-discovery",
            new RetailerDiscoveryResult(
                "12345678",
                "Discovered Integration Retailer",
                "discovered-integration-retailer",
                "https://new-retailer.example.test",
                "https://new-retailer.example.test/product/12345678",
                "Integration Discovery",
                null,
                null));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var listing = await response.Content.ReadFromJsonAsync<RetailerProductListingItem>();
        Assert.NotNull(listing);
        Assert.NotEqual(_fixture.RetailerId, listing.RetailerId);
        Assert.Equal("Discovered Integration Retailer", listing.RetailerName);
        Assert.Equal(RetailerStatus.Discovered, listing.RetailerStatus);

        var retailers = await client.GetAsync("/api/v1/retailer-management?q=discovered-integration-retailer");
        Assert.Equal(HttpStatusCode.OK, retailers.StatusCode);
        var discovered = await retailers.Content.ReadFromJsonAsync<IReadOnlyList<RetailerManagementItem>>();
        Assert.NotNull(discovered);
        Assert.Contains(discovered!, value => value.Id == listing.RetailerId);
    }

    [Fact]
    public async Task RetailerDiscovery_UnknownGtinReturnsNotFound()
    {
        using var client = AuthenticatedClient(PostgreSqlFixture.ModeratorSubject);

        var response = await client.PostAsJsonAsync(
            "/api/v1/retailer-discovery",
            new RetailerDiscoveryResult(
                "99999999",
                "Integration Test Retailer",
                "integration-test-retailer",
                "https://retailer.example.test",
                "https://retailer.example.test/products/missing",
                "Integration Discovery",
                null,
                null));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private HttpClient AuthenticatedClient(string subject)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Development-Subject", subject);

        if (subject == PostgreSqlFixture.ModeratorSubject)
            client.DefaultRequestHeaders.Add("X-Development-Role", "Moderator");
        else if (subject == PostgreSqlFixture.AdministratorSubject)
            client.DefaultRequestHeaders.Add("X-Development-Role", "Administrator");

        return client;
    }

    public void Dispose() => _factory.Dispose();
}
