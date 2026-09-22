using DiaperScout.Application;
using DiaperScout.Domain;
using DiaperScout.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DiaperScout.Api.IntegrationTests;

public sealed class RetailerAffiliateProgrammeDiscoverySchedulerTests : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _fixture;

    public RetailerAffiliateProgrammeDiscoverySchedulerTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task RunOnceAsync_DiscoversEligibleVerifiedRetailer()
    {
        await CleanupAsync();
        var retailerId = await CreateRetailerAsync(RetailerStatus.Verified);
        var discovery = new RecordingDiscovery();
        discovery.Results.Add(new RetailerAffiliateProgrammeItem(
            Guid.NewGuid(),
            retailerId,
            "Awin",
            "12345",
            "Integration Affiliate Programme",
            AffiliateProgrammeStatus.ApplicationRequired,
            "https://retailer.example.test",
            null,
            null,
            null,
            true,
            true,
            "https://api.awin.com/publishers/1/programmedetails",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            false,
            null));

        var scheduler = CreateScheduler(discovery);
        var result = await scheduler.RunOnceAsync();

        Assert.Equal(1, result.EligibleRetailers);
        Assert.Equal(1, result.SucceededRetailers);
        Assert.Equal(1, result.DiscoveredProgrammes);
        Assert.Equal(0, result.FailedRetailers);
        Assert.Single(discovery.RetailerIds);
        Assert.Equal(retailerId, discovery.RetailerIds[0]);
    }

    [Fact]
    public async Task RunOnceAsync_SkipsUnverifiedRetailer()
    {
        await CleanupAsync();
        var retailerId = await CreateRetailerAsync(RetailerStatus.Discovered);
        var discovery = new RecordingDiscovery();
        var scheduler = CreateScheduler(discovery);

        var result = await scheduler.RunOnceAsync();

        Assert.Equal(0, result.EligibleRetailers);
        Assert.Equal(0, result.SucceededRetailers);
        Assert.Equal(0, result.DiscoveredProgrammes);
        Assert.Equal(0, result.FailedRetailers);
        Assert.Empty(discovery.RetailerIds);

    }

    [Fact]
    public async Task RunOnceAsync_SkipsRetailerCheckedWithinInterval()
    {
        await CleanupAsync();
        var retailerId = await CreateRetailerAsync(RetailerStatus.Verified);

        await using (var db = _fixture.CreateDbContext())
        {
            db.RetailerAffiliateProgrammes.Add(new RetailerAffiliateProgramme(
                retailerId,
                "Awin",
                "recent-programme",
                "Recent Programme",
                AffiliateProgrammeStatus.ApplicationRequired,
                "https://retailer.example.test"));
            await db.SaveChangesAsync();
        }

        var discovery = new RecordingDiscovery();
        var scheduler = CreateScheduler(discovery);

        var result = await scheduler.RunOnceAsync();

        Assert.Equal(0, result.EligibleRetailers);
        Assert.Empty(discovery.RetailerIds);
    }

    [Fact]
    public async Task RunOnceAsync_ContinuesWhenOneRetailerFails()
    {
        await CleanupAsync();
        var failingRetailerId = await CreateRetailerAsync(RetailerStatus.Verified);
        var successfulRetailerId = await CreateRetailerAsync(RetailerStatus.Verified);

        var discovery = new RecordingDiscovery
        {
            FailingRetailerId = failingRetailerId
        };
        discovery.Results.Add(new RetailerAffiliateProgrammeItem(
            Guid.NewGuid(),
            successfulRetailerId,
            "Awin",
            "67890",
            "Successful Programme",
            AffiliateProgrammeStatus.ApplicationRequired,
            "https://another.example.test",
            null,
            null,
            null,
            true,
            true,
            "https://api.awin.com/publishers/1/programmedetails",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            false,
            null));

        var scheduler = CreateScheduler(discovery);
        var result = await scheduler.RunOnceAsync();

        Assert.Equal(2, result.EligibleRetailers);
        Assert.Equal(1, result.SucceededRetailers);
        Assert.Equal(1, result.DiscoveredProgrammes);
        Assert.Equal(1, result.FailedRetailers);
        Assert.Contains(successfulRetailerId, discovery.RetailerIds);
    }

    private RetailerAffiliateProgrammeDiscoveryScheduler CreateScheduler(RecordingDiscovery discovery) =>
        new(
            _fixture.CreateDbContext(),
            discovery,
            Options.Create(new RetailerAffiliateProgrammeDiscoveryJobOptions
            {
                Enabled = true,
                IntervalHours = 24,
                BatchSize = 25
            }),
            TimeProvider.System,
            NullLogger<RetailerAffiliateProgrammeDiscoveryScheduler>.Instance);

    private async Task CleanupAsync()
    {
        await using var db = _fixture.CreateDbContext();
        var retailerIds = await db.Retailers
            .Where(value => value.Slug.StartsWith("affiliate-scheduler-retailer-"))
            .Select(value => value.Id)
            .ToListAsync();

        if (retailerIds.Count == 0)
            return;

        db.RetailerAffiliateProgrammes.RemoveRange(
            db.RetailerAffiliateProgrammes.Where(value => retailerIds.Contains(value.RetailerId)));
        db.Retailers.RemoveRange(
            db.Retailers.Where(value => retailerIds.Contains(value.Id)));
        await db.SaveChangesAsync();
    }

    private async Task<Guid> CreateRetailerAsync(RetailerStatus status)
    {
        await using var db = _fixture.CreateDbContext();

        var retailer = new Retailer(
            $"Affiliate Scheduler Retailer {Guid.NewGuid():N}",
            $"affiliate-scheduler-retailer-{Guid.NewGuid():N}",
            "https://retailer.example.test");

        if (status == RetailerStatus.Verified)
            retailer.VerifyIdentity("https://retailer.example.test");

        if (status == RetailerStatus.NeedsReview)
            retailer.MarkNeedsReview();

        db.Retailers.Add(retailer);
        await db.SaveChangesAsync();
        return retailer.Id;
    }

    private sealed class RecordingDiscovery : IRetailerAffiliateProgrammeDiscovery
    {
        public List<Guid> RetailerIds { get; } = [];
        public List<RetailerAffiliateProgrammeItem> Results { get; } = [];
        public Guid? FailingRetailerId { get; init; }

        public Task<IReadOnlyList<RetailerAffiliateProgrammeItem>> DiscoverAndRecordAsync(
            Guid retailerId,
            CancellationToken cancellationToken = default)
        {
            RetailerIds.Add(retailerId);

            if (retailerId == FailingRetailerId)
                throw new InvalidOperationException("Expected integration test failure.");

            return Task.FromResult<IReadOnlyList<RetailerAffiliateProgrammeItem>>(
                Results.Where(value => value.RetailerId == retailerId).ToList());
        }
    }
}
