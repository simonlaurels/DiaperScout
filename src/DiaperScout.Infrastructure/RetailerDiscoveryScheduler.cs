using DiaperScout.Application;
using DiaperScout.Domain;
using DiaperScout.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DiaperScout.Infrastructure;

public sealed class RetailerDiscoveryJobOptions
{
    public const string SectionName = "RetailerDiscoveryJob";

    public bool Enabled { get; set; } = true;
    public int IntervalHours { get; set; } = 24;
    public int BatchSize { get; set; } = 25;
}

public sealed class RetailerDiscoveryScheduler(
    DiaperScoutDbContext db,
    IRetailerDiscovery discovery,
    IOptions<RetailerDiscoveryJobOptions> options,
    TimeProvider timeProvider,
    ILogger<RetailerDiscoveryScheduler> logger) : IRetailerDiscoveryScheduler
{
    public async Task<RetailerDiscoveryRunResult> RunOnceAsync(
        CancellationToken cancellationToken = default)
    {
        if (!options.Value.Enabled)
            return new RetailerDiscoveryRunResult(0, 0, 0, 0);

        var interval = TimeSpan.FromHours(Math.Max(1, options.Value.IntervalHours));
        var cutoff = timeProvider.GetUtcNow() - interval;
        var batchSize = Math.Clamp(options.Value.BatchSize, 1, 500);

        var gtinRows = await (
            from identifier in db.ProductIdentifiers.AsNoTracking()
            join packType in db.PackTypes.AsNoTracking() on identifier.PackTypeId equals packType.Id
            join sizeVariant in db.SizeVariants.AsNoTracking() on packType.SizeVariantId equals sizeVariant.Id
            join productVariant in db.ProductVariants.AsNoTracking() on sizeVariant.ProductVariantId equals productVariant.Id
            join product in db.Products.AsNoTracking() on productVariant.ProductId equals product.Id
            where identifier.Type == IdentifierType.Gtin
                  && product.Status == ProductStatus.Current
            select new
            {
                identifier.Value,
                identifier.PackTypeId
            })
            .Distinct()
            .ToListAsync(cancellationToken);

        if (gtinRows.Count == 0)
            return new RetailerDiscoveryRunResult(0, 0, 0, 0);

        var packTypeIds = gtinRows
            .Select(row => row.PackTypeId)
            .Distinct()
            .ToArray();

        var lastChecks = await db.RetailerProductListings
            .AsNoTracking()
            .Where(listing => packTypeIds.Contains(listing.PackTypeId))
            .GroupBy(listing => listing.PackTypeId)
            .Select(group => new
            {
                PackTypeId = group.Key,
                LastCheckedAtUtc = group.Max(listing => (DateTimeOffset?)listing.LastCheckedAtUtc)
            })
            .ToDictionaryAsync(value => value.PackTypeId, value => value.LastCheckedAtUtc, cancellationToken);

        var eligible = gtinRows
            .Select(row => new
            {
                row.Value,
                LastCheckedAtUtc = lastChecks.GetValueOrDefault(row.PackTypeId)
            })
            .Where(row => row.LastCheckedAtUtc is null || row.LastCheckedAtUtc < cutoff)
            .GroupBy(row => row.Value, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(row => row.LastCheckedAtUtc ?? DateTimeOffset.MinValue)
            .Take(batchSize)
            .ToArray();

        var succeeded = 0;
        var discoveredListings = 0;
        var failed = 0;

        foreach (var candidate in eligible)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var listings = await discovery.DiscoverAndRecordAsync(candidate.Value, cancellationToken);
                succeeded++;
                discoveredListings += listings.Count;
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
                    "Retailer discovery failed for GTIN {Gtin}.",
                    candidate.Value);
            }
        }

        return new RetailerDiscoveryRunResult(
            eligible.Length,
            succeeded,
            discoveredListings,
            failed);
    }
}

public sealed class RetailerDiscoveryBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<RetailerDiscoveryJobOptions> options,
    ILogger<RetailerDiscoveryBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            logger.LogInformation("Retailer discovery background job is disabled.");
            return;
        }

        var interval = TimeSpan.FromHours(Math.Max(1, options.Value.IntervalHours));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var scheduler = scope.ServiceProvider.GetRequiredService<IRetailerDiscoveryScheduler>();
                var result = await scheduler.RunOnceAsync(stoppingToken);

                logger.LogInformation(
                    "Retailer discovery run completed: {EligibleGtins} eligible, {SucceededGtins} succeeded, {DiscoveredListings} listings discovered, {FailedGtins} failed.",
                    result.EligibleGtins,
                    result.SucceededGtins,
                    result.DiscoveredListings,
                    result.FailedGtins);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Retailer discovery background run failed.");
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
