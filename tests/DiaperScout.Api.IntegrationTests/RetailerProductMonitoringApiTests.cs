using System.Net;
using System.Net.Http.Json;
using DiaperScout.Application;
using DiaperScout.Domain;
using Xunit;

namespace DiaperScout.Api.IntegrationTests;

public sealed class RetailerProductMonitoringApiTests : IClassFixture<PostgreSqlFixture>, IDisposable
{
    private readonly PostgreSqlFixture _fixture;
    private readonly ObservationApiFactory _factory;

    public RetailerProductMonitoringApiTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
        _factory = new ObservationApiFactory(fixture);
    }

    [Fact]
    public async Task RecordObservation_PersistsPriceAvailabilityAndUpdatesListingCheckTime()
    {
        var listing = await CreateListingAsync("monitoring-record");
        var before = listing.LastCheckedAtUtc;
        using var client = AuthenticatedClient(PostgreSqlFixture.ModeratorSubject);

        var observedAt = DateTimeOffset.UtcNow.AddMinutes(-5);
        var response = await client.PostAsJsonAsync(
            $"/api/v1/retailer-listings/{listing.Id}/observations",
            new RecordRetailerProductObservationRequest(
                observedAt,
                12.99m,
                "gbp",
                RetailerProductAvailability.InStock,
                "Integration Monitoring",
                "https://retailer.example.test/products/monitoring-record"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var observation = await response.Content.ReadFromJsonAsync<RetailerProductObservationItem>();
        Assert.NotNull(observation);
        Assert.Equal(listing.Id, observation.RetailerProductListingId);
        Assert.Equal(observedAt, observation.ObservedAtUtc);
        Assert.Equal(12.99m, observation.PriceAmount);
        Assert.Equal("GBP", observation.PriceCurrencyCode);
        Assert.Equal(RetailerProductAvailability.InStock, observation.Availability);
        Assert.Equal("Integration Monitoring", observation.Source);

        await using var db = _fixture.CreateDbContext();
        var savedListing = await db.RetailerProductListings.FindAsync(listing.Id);
        Assert.NotNull(savedListing);
        Assert.True(savedListing!.LastCheckedAtUtc >= before);
        Assert.True(savedListing.LastCheckedAtUtc >= observation.CreatedAtUtc);
    }

    [Fact]
    public async Task GetLatestObservation_ReturnsMostRecentObservation()
    {
        var listing = await CreateListingAsync("monitoring-latest");
        using var client = AuthenticatedClient(PostgreSqlFixture.ModeratorSubject);

        var older = await client.PostAsJsonAsync(
            $"/api/v1/retailer-listings/{listing.Id}/observations",
            new RecordRetailerProductObservationRequest(
                DateTimeOffset.UtcNow.AddHours(-2),
                14.99m,
                "GBP",
                RetailerProductAvailability.InStock,
                "Integration Monitoring",
                null));
        var newer = await client.PostAsJsonAsync(
            $"/api/v1/retailer-listings/{listing.Id}/observations",
            new RecordRetailerProductObservationRequest(
                DateTimeOffset.UtcNow.AddMinutes(-10),
                12.99m,
                "GBP",
                RetailerProductAvailability.OutOfStock,
                "Integration Monitoring",
                null));

        Assert.Equal(HttpStatusCode.OK, older.StatusCode);
        Assert.Equal(HttpStatusCode.OK, newer.StatusCode);

        var response = await client.GetAsync(
            $"/api/v1/retailer-listings/{listing.Id}/observations/latest");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var observation = await response.Content.ReadFromJsonAsync<RetailerProductObservationItem>();
        Assert.NotNull(observation);
        Assert.Equal(12.99m, observation.PriceAmount);
        Assert.Equal(RetailerProductAvailability.OutOfStock, observation.Availability);
    }

    [Fact]
    public async Task GetLatestObservation_WithoutObservationsReturnsNotFound()
    {
        var listing = await CreateListingAsync("monitoring-empty");
        using var client = AuthenticatedClient(PostgreSqlFixture.ModeratorSubject);

        var response = await client.GetAsync(
            $"/api/v1/retailer-listings/{listing.Id}/observations/latest");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RecordObservation_UnknownListingReturnsNotFound()
    {
        using var client = AuthenticatedClient(PostgreSqlFixture.ModeratorSubject);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/retailer-listings/{Guid.NewGuid()}/observations",
            new RecordRetailerProductObservationRequest(
                DateTimeOffset.UtcNow,
                9.99m,
                "GBP",
                RetailerProductAvailability.InStock,
                "Integration Monitoring",
                null));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<RetailerProductListing> CreateListingAsync(string slug)
    {
        await using var db = _fixture.CreateDbContext();
        var listing = new RetailerProductListing(
            _fixture.PackTypeId,
            _fixture.RetailerId,
            $"https://retailer.example.test/products/{slug}",
            "Integration Monitoring");
        db.RetailerProductListings.Add(listing);
        await db.SaveChangesAsync();
        return listing;
    }

    private HttpClient AuthenticatedClient(string subject)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Development-Subject", subject);
        client.DefaultRequestHeaders.Add("X-Development-Role", "Moderator");
        return client;
    }

    public void Dispose() => _factory.Dispose();
}
