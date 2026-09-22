using DiaperScout.Domain;
using DiaperScout.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace DiaperScout.Api.IntegrationTests;

public sealed class RetailerIdentityVerificationSchedulerTests : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _fixture;

    public RetailerIdentityVerificationSchedulerTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task RunOnceAsync_VerifiesDiscoveredListingWhenDomainsMatch()
    {
        var retailerId = await CreateRetailerAsync("https://retailer.example.test");

        await using (var db = _fixture.CreateDbContext())
        {
            db.RetailerProductListings.Add(new RetailerProductListing(
                _fixture.PackTypeId,
                retailerId,
                "https://retailer.example.test/products/test",
                "Identity Test",
                "https://source.example.test/search"));
            await db.SaveChangesAsync();
        }

        var verifier = CreateVerifier();
        var result = await verifier.RunOnceAsync();

        Assert.Equal(1, result.EligibleListings);
        Assert.Equal(1, result.VerifiedListings);
        Assert.Equal(0, result.NeedsReviewListings);
        Assert.Equal(0, result.FailedListings);

        await using var verifyDb = _fixture.CreateDbContext();
        var listing = await verifyDb.RetailerProductListings
            .Where(x => x.RetailerId == retailerId)
            .SingleAsync();
        var retailer = await verifyDb.Retailers.FindAsync(retailerId);
        Assert.Equal(RetailerProductDiscoveryStatus.Verified, listing.Status);
        Assert.Equal(RetailerStatus.Verified, retailer!.Status);
    }

    [Fact]
    public async Task RunOnceAsync_MarksListingAndRetailerNeedsReviewWhenDomainsDiffer()
    {
        var retailerId = await CreateRetailerAsync("https://retailer.example.test");

        await using (var db = _fixture.CreateDbContext())
        {
            db.RetailerProductListings.Add(new RetailerProductListing(
                _fixture.PackTypeId,
                retailerId,
                "https://other.example.test/products/test",
                "Identity Test",
                "https://source.example.test/search"));
            await db.SaveChangesAsync();
        }

        var verifier = CreateVerifier();
        var result = await verifier.RunOnceAsync();

        Assert.Equal(1, result.EligibleListings);
        Assert.Equal(0, result.VerifiedListings);
        Assert.Equal(1, result.NeedsReviewListings);
        Assert.Equal(0, result.FailedListings);

        await using var verifyDb = _fixture.CreateDbContext();
        var listing = await verifyDb.RetailerProductListings
            .Where(x => x.RetailerId == retailerId)
            .SingleAsync();
        var retailer = await verifyDb.Retailers.FindAsync(retailerId);
        Assert.Equal(RetailerProductDiscoveryStatus.NeedsReview, listing.Status);
        Assert.Equal(RetailerStatus.NeedsReview, retailer!.Status);
    }

    private RetailerIdentityVerifier CreateVerifier() =>
        new(
            _fixture.CreateDbContext(),
            Options.Create(new RetailerIdentityVerificationJobOptions
            {
                Enabled = true,
                IntervalHours = 24,
                BatchSize = 25
            }),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<RetailerIdentityVerifier>.Instance);

    private async Task<Guid> CreateRetailerAsync(string websiteUrl)
    {
        await using var db = _fixture.CreateDbContext();

        var uniqueId = Guid.NewGuid().ToString("N");
        var retailer = new Retailer(
            $"Integration Test Retailer {uniqueId}",
            $"integration-test-retailer-{uniqueId}",
            websiteUrl);

        db.Retailers.Add(retailer);
        await db.SaveChangesAsync();

        return retailer.Id;
    }
}
