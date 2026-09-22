using DiaperScout.Application;
using DiaperScout.Domain;
using DiaperScout.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace DiaperScout.Api.IntegrationTests;

public sealed class RetailerDiscoverySchedulerTests : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _fixture;

    public RetailerDiscoverySchedulerTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task RunOnceAsync_DiscoversEligibleCurrentGtin()
    {
        await ClearListingsAsync();
        var discovery = new RecordingDiscovery();
        var scheduler = CreateScheduler(discovery);

        var result = await scheduler.RunOnceAsync();

        Assert.Equal(1, result.EligibleGtins);
        Assert.Equal(1, result.SucceededGtins);
        Assert.Equal(0, result.DiscoveredListings);
        Assert.Equal(0, result.FailedGtins);
        Assert.Single(discovery.Gtins);
        Assert.Equal("12345678", discovery.Gtins[0]);
    }

    [Fact]
    public async Task RunOnceAsync_SkipsGtinCheckedWithinInterval()
    {
        await ClearListingsAsync();
        await using (var db = _fixture.CreateDbContext())
        {
            db.RetailerProductListings.Add(
                new RetailerProductListing(
                    _fixture.PackTypeId,
                    _fixture.RetailerId,
                    "https://retailer.example.test/products/scheduler-check",
                    "Scheduler Test"));
            await db.SaveChangesAsync();
        }

        var discovery = new RecordingDiscovery();
        var scheduler = CreateScheduler(discovery);

        var result = await scheduler.RunOnceAsync();

        Assert.Equal(0, result.EligibleGtins);
        Assert.Equal(0, result.SucceededGtins);
        Assert.Empty(discovery.Gtins);
    }

    private RetailerDiscoveryScheduler CreateScheduler(RecordingDiscovery discovery) =>
        new(
            _fixture.CreateDbContext(),
            discovery,
            Options.Create(new RetailerDiscoveryJobOptions
            {
                Enabled = true,
                IntervalHours = 24,
                BatchSize = 25
            }),
            TimeProvider.System,
            NullLogger<RetailerDiscoveryScheduler>.Instance);

    private async Task ClearListingsAsync()
    {
        await using var db = _fixture.CreateDbContext();
        db.RetailerProductListings.RemoveRange(db.RetailerProductListings);
        await db.SaveChangesAsync();
    }

    private sealed class RecordingDiscovery : IRetailerDiscovery
    {
        public List<string> Gtins { get; } = [];

        public Task<IReadOnlyList<RetailerProductListingItem>> DiscoverAndRecordAsync(
            string gtin,
            CancellationToken cancellationToken = default)
        {
            Gtins.Add(gtin);
            return Task.FromResult<IReadOnlyList<RetailerProductListingItem>>([]);
        }

        public Task<RetailerProductListingItem> RecordAsync(
            RetailerDiscoveryResult result,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<RetailerProductListingItem>> GetForGtinAsync(
            string gtin,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
