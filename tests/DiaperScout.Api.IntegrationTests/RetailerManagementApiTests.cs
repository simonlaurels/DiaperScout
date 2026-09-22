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

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/retailer-management",
            new CreateRetailerManagement(
                "Canonical Retailer Listing Test",
                $"canonical-retailer-listing-test-{Guid.NewGuid():N}",
                "https://canonical-retailer-listing.example"));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<RetailerManagementItem>();
        Assert.NotNull(created);

        var response = await client.GetAsync("/api/v1/retailer-management");

        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.True(
            response.IsSuccessStatusCode,
            $"Expected successful response but received {(int)response.StatusCode} {response.StatusCode}. Body: {responseBody}");

        var retailers = await response.Content.ReadFromJsonAsync<IReadOnlyList<RetailerManagementItem>>();
        Assert.NotNull(retailers);

        var retailer = Assert.Single(
            retailers,
            value => value.Id == created!.Id);

        Assert.Equal("Canonical Retailer Listing Test", retailer.Name);
        Assert.Equal(created.Slug, retailer.Slug);
        Assert.Equal(RetailerStatus.Discovered, retailer.Status);
        Assert.NotEqual(default, retailer.CreatedAtUtc);
        Assert.NotEqual(default, retailer.UpdatedAtUtc);
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

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/retailer-management",
            new CreateRetailerManagement(
                "Identity Verification Test Retailer",
                "identity-verification-test-retailer",
                "https://identity-verification.example"));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var retailer = await createResponse.Content.ReadFromJsonAsync<RetailerManagementItem>();
        Assert.NotNull(retailer);

        var retailerId = retailer!.Id;

        var response = await client.PostAsJsonAsync(
            $"/api/v1/retailer-management/{retailerId}/identity-verification",
            new RetailerIdentityVerificationRequest(
                "Identity Verification Test Retailer",
                "https://identity-verification.example/source",
                "https://identity-verification.example/product/123",
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
        Assert.Contains(verified!, value => value.Id == retailerId && value.Status == RetailerStatus.Verified);

        var history = await client.GetAsync($"/api/v1/retailer-management/{retailerId}/identity-verifications");
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
    public async Task RetailerManagement_CanRecordAndSelectAffiliateProgramme()
    {
        using var client = AuthenticatedClient(PostgreSqlFixture.ModeratorSubject);

        var verification = await client.PostAsJsonAsync(
            $"/api/v1/retailer-management/{_fixture.RetailerId}/identity-verification",
            new RetailerIdentityVerificationRequest(
                "Integration Test Retailer",
                "https://retailer.example.test/source",
                "https://retailer.example.test/product/123",
                RetailerIdentityVerificationOutcome.Verified,
                null));

        Assert.Equal(HttpStatusCode.OK, verification.StatusCode);

        var discovery = await client.PostAsJsonAsync(
            $"/api/v1/retailer-management/{_fixture.RetailerId}/affiliate-programmes",
            new RetailerAffiliateProgrammeDiscoveryRequest(
                "Integration Network",
                "programme-001",
                "Integration Retailer Programme",
                AffiliateProgrammeStatus.ProgrammeAvailable,
                "https://network.example.test/programmes/001",
                "https://network.example.test/programmes/001/terms",
                "10% commission; 30-day cookie.",
                30,
                true,
                false,
                "https://network.example.test/search"));

        Assert.Equal(HttpStatusCode.OK, discovery.StatusCode);
        var discovered = await discovery.Content.ReadFromJsonAsync<RetailerAffiliateProgrammeItem>();
        Assert.NotNull(discovered);
        Assert.False(discovered.IsPreferred);
        Assert.Equal("Integration Network", discovered.Network);
        Assert.Equal("programme-001", discovered.ProgrammeId);
        Assert.Equal(AffiliateProgrammeStatus.ProgrammeAvailable, discovered.Status);

        var secondDiscovery = await client.PostAsJsonAsync(
            $"/api/v1/retailer-management/{_fixture.RetailerId}/affiliate-programmes",
            new RetailerAffiliateProgrammeDiscoveryRequest(
                "Second Network",
                "programme-002",
                "Second Retailer Programme",
                AffiliateProgrammeStatus.ApplicationRequired,
                "https://second.example.test/programmes/002",
                null,
                "12% commission; application required.",
                45,
                false,
                true,
                "https://second.example.test/search"));

        Assert.Equal(HttpStatusCode.OK, secondDiscovery.StatusCode);
        var second = await secondDiscovery.Content.ReadFromJsonAsync<RetailerAffiliateProgrammeItem>();
        Assert.NotNull(second);

        var selectedResponse = await client.PostAsync(
            $"/api/v1/retailer-management/{_fixture.RetailerId}/affiliate-programmes/{second!.Id}/select",
            null);

        Assert.Equal(HttpStatusCode.OK, selectedResponse.StatusCode);
        var selected = await selectedResponse.Content.ReadFromJsonAsync<RetailerAffiliateProgrammeItem>();
        Assert.NotNull(selected);
        Assert.True(selected.IsPreferred);

        var programmesResponse = await client.GetAsync(
            $"/api/v1/retailer-management/{_fixture.RetailerId}/affiliate-programmes");

        Assert.Equal(HttpStatusCode.OK, programmesResponse.StatusCode);
        var programmes = await programmesResponse.Content.ReadFromJsonAsync<IReadOnlyList<RetailerAffiliateProgrammeItem>>();
        Assert.NotNull(programmes);
        Assert.Contains(programmes!, value => value.Id == second.Id && value.IsPreferred);
        Assert.Contains(programmes!, value => value.Id == discovered.Id && !value.IsPreferred);
    }

    [Fact]
    public async Task RetailerManagement_CanUpdateAffiliateProgrammeLifecycleStatus()
    {
        using var client = AuthenticatedClient(PostgreSqlFixture.ModeratorSubject);

        await client.PostAsJsonAsync(
            $"/api/v1/retailer-management/{_fixture.RetailerId}/identity-verification",
            new RetailerIdentityVerificationRequest(
                "Integration Test Retailer",
                "https://retailer.example.test/source",
                "https://retailer.example.test/product/123",
                RetailerIdentityVerificationOutcome.Verified,
                null));

        var discovery = await client.PostAsJsonAsync(
            $"/api/v1/retailer-management/{_fixture.RetailerId}/affiliate-programmes",
            new RetailerAffiliateProgrammeDiscoveryRequest(
                "Lifecycle Network",
                "lifecycle-001",
                "Lifecycle Programme",
                AffiliateProgrammeStatus.ApplicationRequired,
                "https://network.example.test/programmes/lifecycle-001",
                null,
                "10% commission.",
                30,
                true,
                true,
                "https://network.example.test/search"));

        var discovered = await discovery.Content.ReadFromJsonAsync<RetailerAffiliateProgrammeItem>();
        Assert.NotNull(discovered);

        var submitted = await client.PutAsJsonAsync(
            $"/api/v1/retailer-management/{_fixture.RetailerId}/affiliate-programmes/{discovered!.Id}/status",
            new RetailerAffiliateProgrammeStatusUpdateRequest(AffiliateProgrammeStatus.ApplicationSubmitted));

        Assert.Equal(HttpStatusCode.OK, submitted.StatusCode);
        var submittedItem = await submitted.Content.ReadFromJsonAsync<RetailerAffiliateProgrammeItem>();
        Assert.Equal(AffiliateProgrammeStatus.ApplicationSubmitted, submittedItem!.Status);

        var approved = await client.PutAsJsonAsync(
            $"/api/v1/retailer-management/{_fixture.RetailerId}/affiliate-programmes/{discovered.Id}/status",
            new RetailerAffiliateProgrammeStatusUpdateRequest(AffiliateProgrammeStatus.Approved));

        Assert.Equal(HttpStatusCode.OK, approved.StatusCode);

        var configured = await client.PutAsJsonAsync(
            $"/api/v1/retailer-management/{_fixture.RetailerId}/affiliate-programmes/{discovered.Id}/status",
            new RetailerAffiliateProgrammeStatusUpdateRequest(AffiliateProgrammeStatus.Configured));

        Assert.Equal(HttpStatusCode.OK, configured.StatusCode);
        var configuredItem = await configured.Content.ReadFromJsonAsync<RetailerAffiliateProgrammeItem>();
        Assert.Equal(AffiliateProgrammeStatus.Configured, configuredItem!.Status);
    }

    [Fact]
    public async Task RetailerManagement_CannotConfigureAffiliateProgrammeBeforeApproval()
    {
        using var client = AuthenticatedClient(PostgreSqlFixture.ModeratorSubject);

        await client.PostAsJsonAsync(
            $"/api/v1/retailer-management/{_fixture.RetailerId}/identity-verification",
            new RetailerIdentityVerificationRequest(
                "Integration Test Retailer",
                "https://retailer.example.test/source",
                "https://retailer.example.test/product/123",
                RetailerIdentityVerificationOutcome.Verified,
                null));

        var discovery = await client.PostAsJsonAsync(
            $"/api/v1/retailer-management/{_fixture.RetailerId}/affiliate-programmes",
            new RetailerAffiliateProgrammeDiscoveryRequest(
                "Lifecycle Network",
                "lifecycle-002",
                "Lifecycle Programme",
                AffiliateProgrammeStatus.ProgrammeAvailable,
                null, null, null, null, null, false, null));

        var discovered = await discovery.Content.ReadFromJsonAsync<RetailerAffiliateProgrammeItem>();
        Assert.NotNull(discovered);

        var response = await client.PutAsJsonAsync(
            $"/api/v1/retailer-management/{_fixture.RetailerId}/affiliate-programmes/{discovered!.Id}/status",
            new RetailerAffiliateProgrammeStatusUpdateRequest(AffiliateProgrammeStatus.Configured));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RetailerManagement_AffiliateDiscoveryDoesNotDowngradeManagedProgrammeStatus()
    {
        using var client = AuthenticatedClient(PostgreSqlFixture.ModeratorSubject);

        await client.PostAsJsonAsync(
            $"/api/v1/retailer-management/{_fixture.RetailerId}/identity-verification",
            new RetailerIdentityVerificationRequest(
                "Integration Test Retailer",
                "https://retailer.example.test/source",
                "https://retailer.example.test/product/123",
                RetailerIdentityVerificationOutcome.Verified,
                null));

        var discovery = await client.PostAsJsonAsync(
            $"/api/v1/retailer-management/{_fixture.RetailerId}/affiliate-programmes",
            new RetailerAffiliateProgrammeDiscoveryRequest(
                "Lifecycle Network",
                "lifecycle-003",
                "Lifecycle Programme",
                AffiliateProgrammeStatus.ApplicationRequired,
                null, null, null, null, null, true, null));

        var discovered = await discovery.Content.ReadFromJsonAsync<RetailerAffiliateProgrammeItem>();
        Assert.NotNull(discovered);

        var approved = await client.PutAsJsonAsync(
            $"/api/v1/retailer-management/{_fixture.RetailerId}/affiliate-programmes/{discovered!.Id}/status",
            new RetailerAffiliateProgrammeStatusUpdateRequest(AffiliateProgrammeStatus.Approved));

        Assert.Equal(HttpStatusCode.OK, approved.StatusCode);

        var rediscovery = await client.PostAsJsonAsync(
            $"/api/v1/retailer-management/{_fixture.RetailerId}/affiliate-programmes",
            new RetailerAffiliateProgrammeDiscoveryRequest(
                "Lifecycle Network",
                "lifecycle-003",
                "Lifecycle Programme",
                AffiliateProgrammeStatus.ApplicationRequired,
                null, null, null, null, null, true, null));

        Assert.Equal(HttpStatusCode.OK, rediscovery.StatusCode);
        var rediscovered = await rediscovery.Content.ReadFromJsonAsync<RetailerAffiliateProgrammeItem>();
        Assert.Equal(AffiliateProgrammeStatus.Approved, rediscovered!.Status);
    }

    [Fact]
    public async Task RetailerManagement_AffiliateDiscoveryRequiresVerifiedRetailer()
    {
        using var client = AuthenticatedClient(PostgreSqlFixture.ModeratorSubject);

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/retailer-management",
            new CreateRetailerManagement(
                "Affiliate Discovery Unverified Retailer",
                "affiliate-discovery-unverified",
                "https://affiliate-unverified.example"));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var retailer = await createResponse.Content.ReadFromJsonAsync<RetailerManagementItem>();
        Assert.NotNull(retailer);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/retailer-management/{retailer!.Id}/affiliate-programmes",
            new RetailerAffiliateProgrammeDiscoveryRequest(
                "Integration Network",
                "programme-unverified",
                "Should Not Be Recorded",
                AffiliateProgrammeStatus.ProgrammeAvailable,
                null,
                null,
                null,
                null,
                null,
                false,
                null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
