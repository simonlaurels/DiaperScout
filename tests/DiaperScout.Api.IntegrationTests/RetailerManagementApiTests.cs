using System.Net;
using System.Net.Http.Json;
using DiaperScout.Application;
using DiaperScout.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DiaperScout.Api.IntegrationTests;

public sealed class RetailerManagementApiTests : IClassFixture<PostgreSqlFixture>, IDisposable
{
    private readonly PostgreSqlFixture _fixture;
    private readonly ObservationApiFactory _factory;

    public RetailerManagementApiTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
        _factory = new ObservationApiFactory(fixture);
    }

    [Fact]
    public async Task RetailerManagement_ReturnsCanonicalRetailers()
    {
        using var client = AuthenticatedClient(PostgreSqlFixture.ModeratorSubject);

        var response = await client.GetAsync("/api/v1/retailer-management");

        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.True(
            response.IsSuccessStatusCode,
            $"Expected successful response but received {(int)response.StatusCode} {response.StatusCode}. Body: {responseBody}");

        var retailers = await response.Content.ReadFromJsonAsync<IReadOnlyList<RetailerManagementItem>>();
        Assert.NotNull(retailers);

        var fixtureRetailer = Assert.Single(
            retailers,
            retailer => retailer.Id == _fixture.RetailerId);

        Assert.Equal("Integration Test Retailer", fixtureRetailer.Name);
        Assert.Equal("integration-test-retailer", fixtureRetailer.Slug);
        Assert.Equal(RetailerStatus.Discovered, fixtureRetailer.Status);
        Assert.NotEqual(default, fixtureRetailer.CreatedAtUtc);
        Assert.NotEqual(default, fixtureRetailer.UpdatedAtUtc);
    }

    [Fact]
    public async Task RetailerManagement_CanCreateAndUpdateIdentity()
    {
        using var client = AuthenticatedClient(PostgreSqlFixture.ModeratorSubject);

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/retailer-management",
            new CreateRetailerManagement(
                "New Integration Retailer",
                "new-integration-retailer",
                "https://retailer.example"));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<RetailerManagementItem>();
        Assert.NotNull(created);
        Assert.Equal(RetailerStatus.Discovered, created.Status);

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/v1/retailer-management/{created.Id}/identity",
            new UpdateRetailerIdentity(
                "Updated Integration Retailer",
                "updated-integration-retailer",
                "https://updated.example"));

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updated = await updateResponse.Content.ReadFromJsonAsync<RetailerManagementItem>();
        Assert.NotNull(updated);
        Assert.Equal("Updated Integration Retailer", updated.Name);
        Assert.Equal("updated-integration-retailer", updated.Slug);
        Assert.Equal("https://updated.example/", updated.WebsiteUrl);
        Assert.Equal(RetailerStatus.Discovered, updated.Status);
    }


    [Fact]
    public async Task RetailerManagement_CanVerifyIdentityAndRetainEvidence()
    {
        using var client = AuthenticatedClient(PostgreSqlFixture.ModeratorSubject);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/retailer-management/{_fixture.RetailerId}/identity-verification",
            new RetailerIdentityVerificationRequest(
                "Integration Test Retailer",
                "https://retailer.example.test/source",
                "https://retailer.example.test/product/123",
                RetailerIdentityVerificationOutcome.Verified,
                "Matched retailer name, website and discovered listing."));

        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.True(
            response.IsSuccessStatusCode,
            $"Expected successful response but received {(int)response.StatusCode} {response.StatusCode}. Body: {responseBody}");

        var verification = await response.Content.ReadFromJsonAsync<RetailerIdentityVerificationItem>();
        Assert.NotNull(verification);
        Assert.Equal(RetailerIdentityVerificationOutcome.Verified, verification.Outcome);
        Assert.True(verification.Checks.WebsiteUrlValid);
        Assert.True(verification.Checks.SourceUrlValid);
        Assert.True(verification.Checks.ListingUrlValid);
        Assert.True(verification.Checks.NameMatches);
        Assert.True(verification.Checks.DomainMatches);

        var retailers = await client.GetAsync("/api/v1/retailer-management?status=Verified");
        Assert.Equal(HttpStatusCode.OK, retailers.StatusCode);
        var verified = await retailers.Content.ReadFromJsonAsync<IReadOnlyList<RetailerManagementItem>>();
        Assert.Contains(verified!, value => value.Id == _fixture.RetailerId && value.Status == RetailerStatus.Verified);

        var history = await client.GetAsync($"/api/v1/retailer-management/{_fixture.RetailerId}/identity-verifications");
        Assert.Equal(HttpStatusCode.OK, history.StatusCode);
        var records = await history.Content.ReadFromJsonAsync<IReadOnlyList<RetailerIdentityVerificationItem>>();
        var saved = Assert.Single(records!);
        Assert.Equal("Matched retailer name, website and discovered listing.", saved.Notes);
    }

    [Fact]
    public async Task RetailerManagement_VerificationRejectsFailedChecksAndAllowsNeedsReview()
    {
        using var client = AuthenticatedClient(PostgreSqlFixture.ModeratorSubject);

        var rejected = await client.PostAsJsonAsync(
            $"/api/v1/retailer-management/{_fixture.RetailerId}/identity-verification",
            new RetailerIdentityVerificationRequest(
                "Different Retailer",
                "https://other.example.test/source",
                "https://other.example.test/product/123",
                RetailerIdentityVerificationOutcome.Verified,
                null));

        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);

        var review = await client.PostAsJsonAsync(
            $"/api/v1/retailer-management/{_fixture.RetailerId}/identity-verification",
            new RetailerIdentityVerificationRequest(
                "Different Retailer",
                "https://other.example.test/source",
                "https://other.example.test/product/123",
                RetailerIdentityVerificationOutcome.NeedsReview,
                "Evidence does not match the canonical retailer."));

        Assert.Equal(HttpStatusCode.OK, review.StatusCode);
        var verification = await review.Content.ReadFromJsonAsync<RetailerIdentityVerificationItem>();
        Assert.NotNull(verification);
        Assert.Equal(RetailerIdentityVerificationOutcome.NeedsReview, verification.Outcome);

        var retailers = await client.GetAsync($"/api/v1/retailer-management?status={RetailerStatus.NeedsReview}");
        var needsReview = await retailers.Content.ReadFromJsonAsync<IReadOnlyList<RetailerManagementItem>>();
        Assert.Contains(needsReview!, value => value.Id == _fixture.RetailerId);
    }

    [Fact]
    public async Task RetailerManagement_ForExplorer_ReturnsForbidden()
    {
        using var client = AuthenticatedClient(PostgreSqlFixture.ExplorerSubject);

        var response = await client.GetAsync("/api/v1/retailer-management");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
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
