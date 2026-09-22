using DiaperScout.Application;
using DiaperScout.Domain;
using DiaperScout.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DiaperScout.Infrastructure;

public sealed class RetailerAffiliateProgrammeDiscoveryJobOptions
{
    public const string SectionName = "RetailerAffiliateProgrammeDiscoveryJob";

    public bool Enabled { get; set; } = true;
    public int IntervalHours { get; set; } = 24;
    public int BatchSize { get; set; } = 25;
}

public sealed class RetailerAffiliateProgrammeDiscovery(
    DiaperScoutDbContext db,
    IRetailerAffiliateProgrammeDiscoveryProvider provider) : IRetailerAffiliateProgrammeDiscovery
{
    public async Task<IReadOnlyList<RetailerAffiliateProgrammeItem>> DiscoverAndRecordAsync(
        Guid retailerId,
        CancellationToken cancellationToken = default)
    {
        var retailer = await db.Retailers
            .SingleOrDefaultAsync(value => value.Id == retailerId, cancellationToken)
            ?? throw new KeyNotFoundException();

        if (retailer.Status != RetailerStatus.Verified)
            throw new CatalogueValidationException(
                "retailer",
                "The retailer must be verified before affiliate programme discovery can run.");

        var target = new RetailerAffiliateProgrammeDiscoveryTarget(
            retailer.Id,
            retailer.Name,
            retailer.WebsiteUrl);

        var candidates = await provider.DiscoverAsync(target, cancellationToken);

        var results = new List<RetailerAffiliateProgrammeItem>();

        foreach (var candidate in candidates
            .GroupBy(
                value => $"{value.Network.Trim().ToUpperInvariant()}\u001f{value.ProgrammeId.Trim().ToUpperInvariant()}")
            .Select(group => group.First()))
        {
            RetailerAffiliateProgramme? programme = null;
            try
            {
                programme = await db.RetailerAffiliateProgrammes
                    .SingleOrDefaultAsync(
                        value => value.RetailerId == retailerId
                            && value.Network == candidate.Network.Trim()
                            && value.ProgrammeId == candidate.ProgrammeId.Trim(),
                        cancellationToken);

                if (programme is null)
                {
                    programme = new RetailerAffiliateProgramme(
                        retailerId,
                        candidate.Network,
                        candidate.ProgrammeId,
                        candidate.ProgrammeName,
                        candidate.Status,
                        candidate.ProgrammeUrl,
                        candidate.TermsUrl,
                        candidate.ReferralTerms,
                        candidate.CookieDurationDays,
                        candidate.DeepLinksAllowed,
                        candidate.ApplicationRequired,
                        candidate.SourceUrl);
                    db.RetailerAffiliateProgrammes.Add(programme);
                }
                else
                {
                    programme.UpdateDiscovery(
                        candidate.ProgrammeName,
                        candidate.Status,
                        candidate.ProgrammeUrl,
                        candidate.TermsUrl,
                        candidate.ReferralTerms,
                        candidate.CookieDurationDays,
                        candidate.DeepLinksAllowed,
                        candidate.ApplicationRequired,
                        candidate.SourceUrl);
                }
            }
            catch (ArgumentException exception)
            {
                throw new CatalogueValidationException(
                    exception.ParamName ?? "affiliate",
                    exception.Message);
            }

            results.Add(ToItem(programme));
        }

        await db.SaveChangesAsync(cancellationToken);
        return results;
    }

    private static RetailerAffiliateProgrammeItem ToItem(RetailerAffiliateProgramme value) =>
        new(
            value.Id,
            value.RetailerId,
            value.Network,
            value.ProgrammeId,
            value.ProgrammeName,
            value.Status,
            value.ProgrammeUrl,
            value.TermsUrl,
            value.ReferralTerms,
            value.CookieDurationDays,
            value.DeepLinksAllowed,
            value.ApplicationRequired,
            value.SourceUrl,
            value.DiscoveredAtUtc,
            value.LastCheckedAtUtc,
            value.IsPreferred,
            value.PreferredAtUtc);
}

public sealed class RetailerAffiliateProgrammeDiscoveryScheduler(
    DiaperScoutDbContext db,
    IRetailerAffiliateProgrammeDiscovery discovery,
    IOptions<RetailerAffiliateProgrammeDiscoveryJobOptions> options,
    TimeProvider timeProvider,
    ILogger<RetailerAffiliateProgrammeDiscoveryScheduler> logger) : IRetailerAffiliateProgrammeDiscoveryScheduler
{
    public async Task<RetailerAffiliateProgrammeDiscoveryRunResult> RunOnceAsync(
        CancellationToken cancellationToken = default)
    {
        if (!options.Value.Enabled)
            return new RetailerAffiliateProgrammeDiscoveryRunResult(0, 0, 0, 0);

        var interval = TimeSpan.FromHours(Math.Max(1, options.Value.IntervalHours));
        var cutoff = timeProvider.GetUtcNow() - interval;
        var batchSize = Math.Clamp(options.Value.BatchSize, 1, 500);

        var lastChecks = await db.RetailerAffiliateProgrammes
            .AsNoTracking()
            .GroupBy(value => value.RetailerId)
            .Select(group => new
            {
                RetailerId = group.Key,
                LastCheckedAtUtc = group.Max(value => (DateTimeOffset?)value.LastCheckedAtUtc)
            })
            .ToDictionaryAsync(value => value.RetailerId, value => value.LastCheckedAtUtc, cancellationToken);

        var verifiedRetailers = await db.Retailers
            .AsNoTracking()
            .Where(value => value.Status == RetailerStatus.Verified)
            .OrderBy(value => value.UpdatedAtUtc)
            .Select(value => value.Id)
            .ToListAsync(cancellationToken);

        var retailers = verifiedRetailers
            .Where(retailerId =>
                !lastChecks.TryGetValue(retailerId, out var lastCheckedAtUtc)
                || lastCheckedAtUtc is null
                || lastCheckedAtUtc < cutoff)
            .Take(batchSize)
            .Select(retailerId => new { Id = retailerId })
            .ToList();

        var succeeded = 0;
        var discovered = 0;
        var failed = 0;

        foreach (var retailer in retailers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var programmes = await discovery.DiscoverAndRecordAsync(retailer.Id, cancellationToken);
                succeeded++;
                discovered += programmes.Count;
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
                    "Automated affiliate programme discovery failed for retailer {RetailerId}.",
                    retailer.Id);
            }
        }

        return new RetailerAffiliateProgrammeDiscoveryRunResult(
            retailers.Count,
            succeeded,
            discovered,
            failed);
    }
}

public sealed class RetailerAffiliateProgrammeDiscoveryBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<RetailerAffiliateProgrammeDiscoveryJobOptions> options,
    ILogger<RetailerAffiliateProgrammeDiscoveryBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            logger.LogInformation("Retailer affiliate programme discovery background job is disabled.");
            return;
        }

        var interval = TimeSpan.FromHours(Math.Max(1, options.Value.IntervalHours));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var scheduler = scope.ServiceProvider
                    .GetRequiredService<IRetailerAffiliateProgrammeDiscoveryScheduler>();
                var result = await scheduler.RunOnceAsync(stoppingToken);

                logger.LogInformation(
                    "Retailer affiliate programme discovery run completed: {EligibleRetailers} eligible, {SucceededRetailers} succeeded, {DiscoveredProgrammes} programmes discovered, {FailedRetailers} failed.",
                    result.EligibleRetailers,
                    result.SucceededRetailers,
                    result.DiscoveredProgrammes,
                    result.FailedRetailers);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Retailer affiliate programme discovery background run failed.");
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
