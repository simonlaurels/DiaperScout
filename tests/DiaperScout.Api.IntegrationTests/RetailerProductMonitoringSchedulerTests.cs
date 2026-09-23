using DiaperScout.Application;
using DiaperScout.Domain;
using DiaperScout.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DiaperScout.Api.IntegrationTests;

public sealed class RetailerProductMonitoringSchedulerTests : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _fixture;

    public RetailerProductMonitoringSchedulerTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task RunOnce_MonitorsEligibleListingAndRecordsObservation()
    {
        var listing = await CreateListingAsync("scheduler-record");
        await MarkStaleAsync(listing.Id);

        await using var db = _fixture.CreateDbContext();
        var monitoring = new RetailerProductMonitoringForTests(db);
        var monitor = new TestRetailerProductMonitor(
            new RetailerProductObservationReading(
                DateTimeOffset.UtcNow.AddMinutes(-1),
                10.99m,
                "gbp",
                RetailerProductAvailability.InStock,
                "Test Monitor",
                "https://retailer.example.test/products/scheduler-record"));

        var scheduler = new RetailerProductMonitoringScheduler(
            db,
            monitoring,
            [monitor],
            Options.Create(new RetailerProductMonitoringJobOptions
            {
                Enabled = true,
                IntervalHours = 1,
                BatchSize = 10
            }),
            TimeProvider.System,
            NullLogger<RetailerProductMonitoringScheduler>.Instance);

        var result = await scheduler.RunOnceAsync();

        Assert.Equal(1, result.EligibleListings);
        Assert.Equal(1, result.MonitoredListings);
        Assert.Equal(1, result.ObservationsRecorded);
        Assert.Equal(0, result.UnsupportedListings);
        Assert.Equal(0, result.FailedListings);

        var observation = await db.RetailerProductObservations
            .SingleAsync(value => value.RetailerProductListingId == listing.Id);

        Assert.Equal(10.99m, observation.PriceAmount);
        Assert.Equal("GBP", observation.PriceCurrencyCode);
        Assert.Equal(RetailerProductAvailability.InStock, observation.Availability);
    }

    [Fact]
    public async Task RunOnce_LeavesListingAloneWhenNoMonitorSupportsIt()
    {
        var listing = await CreateListingAsync("scheduler-unsupported");
        await MarkStaleAsync(listing.Id);

        await using var db = _fixture.CreateDbContext();
        var monitoring = new RetailerProductMonitoringForTests(db);

        var scheduler = new RetailerProductMonitoringScheduler(
            db,
            monitoring,
            [],
            Options.Create(new RetailerProductMonitoringJobOptions
            {
                Enabled = true,
                IntervalHours = 1,
                BatchSize = 10
            }),
            TimeProvider.System,
            NullLogger<RetailerProductMonitoringScheduler>.Instance);

        var result = await scheduler.RunOnceAsync();

        Assert.Equal(1, result.EligibleListings);
        Assert.Equal(0, result.MonitoredListings);
        Assert.Equal(0, result.ObservationsRecorded);
        Assert.Equal(1, result.UnsupportedListings);
        Assert.Equal(0, result.FailedListings);

        Assert.False(await db.RetailerProductObservations.AnyAsync(
            value => value.RetailerProductListingId == listing.Id));
    }

    private async Task<RetailerProductListing> CreateListingAsync(string slug)
    {
        await using var db = _fixture.CreateDbContext();
        var listing = new RetailerProductListing(
            _fixture.PackTypeId,
            _fixture.RetailerId,
            $"https://retailer.example.test/products/{slug}",
            "Integration Monitoring Scheduler");
        db.RetailerProductListings.Add(listing);
        await db.SaveChangesAsync();
        return listing;
    }

    private async Task MarkStaleAsync(Guid listingId)
    {
        await using var db = _fixture.CreateDbContext();
        var stale = DateTimeOffset.UtcNow.AddHours(-2);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE diaperscout.retailer_product_listings SET \"LastCheckedAtUtc\" = {stale} WHERE \"Id\" = {listingId}");
    }

    private sealed class TestRetailerProductMonitor(RetailerProductObservationReading reading)
        : IRetailerProductMonitor
    {
        public bool CanMonitor(RetailerProductListing listing) => true;

        public Task<RetailerProductObservationReading> ObserveAsync(
            RetailerProductListing listing,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(reading);
    }

    private sealed class RetailerProductMonitoringForTests(DiaperScout.Infrastructure.Persistence.DiaperScoutDbContext db)
        : IRetailerProductMonitoring
    {
        public async Task<RetailerProductObservationItem> RecordAsync(
            Guid retailerProductListingId,
            RecordRetailerProductObservationRequest request,
            CancellationToken cancellationToken = default)
        {
            var listing = await db.RetailerProductListings.SingleAsync(
                value => value.Id == retailerProductListingId,
                cancellationToken);

            var observation = new RetailerProductObservation(
                retailerProductListingId,
                request.ObservedAtUtc,
                request.PriceAmount,
                request.PriceCurrencyCode,
                request.Availability,
                request.Source,
                request.SourceUrl);

            db.RetailerProductObservations.Add(observation);
            listing.MarkChecked();
            await db.SaveChangesAsync(cancellationToken);

            return new RetailerProductObservationItem(
                observation.Id,
                observation.RetailerProductListingId,
                observation.ObservedAtUtc,
                observation.PriceAmount,
                observation.PriceCurrencyCode,
                observation.Availability,
                observation.Source,
                observation.SourceUrl,
                observation.CreatedAtUtc);
        }

        public Task<RetailerProductObservationItem?> GetLatestAsync(
            Guid retailerProductListingId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
