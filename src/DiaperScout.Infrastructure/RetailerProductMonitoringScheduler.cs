using DiaperScout.Application;
using DiaperScout.Domain;
using DiaperScout.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DiaperScout.Infrastructure;

public sealed class RetailerProductMonitoringJobOptions
{
    public const string SectionName = "RetailerProductMonitoringJob";

    public bool Enabled { get; set; } = false;
    public int IntervalHours { get; set; } = 6;
    public int BatchSize { get; set; } = 50;
}

public sealed class RetailerProductMonitoringScheduler(
    DiaperScoutDbContext db,
    IRetailerProductMonitoring monitoring,
    IEnumerable<IRetailerProductMonitor> monitors,
    IOptions<RetailerProductMonitoringJobOptions> options,
    TimeProvider timeProvider,
    ILogger<RetailerProductMonitoringScheduler> logger) : IRetailerProductMonitoringScheduler
{
    public async Task<RetailerProductMonitoringRunResult> RunOnceAsync(
        CancellationToken cancellationToken = default)
    {
        if (!options.Value.Enabled)
            return new RetailerProductMonitoringRunResult(0, 0, 0, 0, 0);

        var interval = TimeSpan.FromHours(Math.Max(1, options.Value.IntervalHours));
        var cutoff = timeProvider.GetUtcNow() - interval;
        var batchSize = Math.Clamp(options.Value.BatchSize, 1, 500);
        var availableMonitors = monitors.ToArray();

        var listings = await db.RetailerProductListings
            .Where(listing =>
                listing.Status != RetailerProductDiscoveryStatus.Inactive &&
                listing.LastCheckedAtUtc < cutoff)
            .OrderBy(listing => listing.LastCheckedAtUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        var monitored = 0;
        var recorded = 0;
        var unsupported = 0;
        var failed = 0;

        foreach (var listing in listings)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var monitor = availableMonitors.FirstOrDefault(value => value.CanMonitor(listing));
            if (monitor is null)
            {
                unsupported++;
                continue;
            }

            monitored++;

            try
            {
                var reading = await monitor.ObserveAsync(listing, cancellationToken);

                await monitoring.RecordAsync(
                    listing.Id,
                    new RecordRetailerProductObservationRequest(
                        reading.ObservedAtUtc,
                        reading.PriceAmount,
                        reading.PriceCurrencyCode,
                        reading.Availability,
                        reading.Source,
                        reading.SourceUrl),
                    cancellationToken);

                recorded++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                failed++;
                logger.LogError(
                    exception,
                    "Retailer product monitoring failed for listing {RetailerProductListingId}.",
                    listing.Id);
            }
        }

        return new RetailerProductMonitoringRunResult(
            listings.Count,
            monitored,
            recorded,
            unsupported,
            failed);
    }
}

public sealed class RetailerProductMonitoringBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<RetailerProductMonitoringJobOptions> options,
    ILogger<RetailerProductMonitoringBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            logger.LogInformation("Retailer product monitoring background job is disabled.");
            return;
        }

        var interval = TimeSpan.FromHours(Math.Max(1, options.Value.IntervalHours));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var scheduler = scope.ServiceProvider.GetRequiredService<IRetailerProductMonitoringScheduler>();
                var result = await scheduler.RunOnceAsync(stoppingToken);

                logger.LogInformation(
                    "Retailer product monitoring run completed: {EligibleListings} eligible, {MonitoredListings} monitored, {ObservationsRecorded} observations recorded, {UnsupportedListings} unsupported, {FailedListings} failed.",
                    result.EligibleListings,
                    result.MonitoredListings,
                    result.ObservationsRecorded,
                    result.UnsupportedListings,
                    result.FailedListings);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Retailer product monitoring background run failed.");
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
