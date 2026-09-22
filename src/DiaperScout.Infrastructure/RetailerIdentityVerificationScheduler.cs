using DiaperScout.Domain;
using DiaperScout.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DiaperScout.Infrastructure;

public sealed class RetailerIdentityVerificationJobOptions
{
    public const string SectionName = "RetailerIdentityVerificationJob";

    public bool Enabled { get; set; } = true;
    public int IntervalHours { get; set; } = 24;
    public int BatchSize { get; set; } = 100;
}

public sealed record RetailerIdentityVerificationRunResult(
    int EligibleListings,
    int VerifiedListings,
    int NeedsReviewListings,
    int FailedListings);

public interface IRetailerIdentityVerifier
{
    Task<RetailerIdentityVerificationRunResult> RunOnceAsync(
        CancellationToken cancellationToken = default);
}

public sealed class RetailerIdentityVerifier(
    DiaperScoutDbContext db,
    IOptions<RetailerIdentityVerificationJobOptions> options,
    ILogger<RetailerIdentityVerifier> logger) : IRetailerIdentityVerifier
{
    public async Task<RetailerIdentityVerificationRunResult> RunOnceAsync(
        CancellationToken cancellationToken = default)
    {
        if (!options.Value.Enabled)
            return new RetailerIdentityVerificationRunResult(0, 0, 0, 0);

        var batchSize = Math.Clamp(options.Value.BatchSize, 1, 1000);
        var listings = await (
            from listing in db.RetailerProductListings
            join retailer in db.Retailers on listing.RetailerId equals retailer.Id
            where listing.Status == RetailerProductDiscoveryStatus.Discovered
                  && retailer.Status == RetailerStatus.Discovered
            orderby listing.LastCheckedAtUtc
            select new { Listing = listing, Retailer = retailer })
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        var verified = 0;
        var needsReview = 0;
        var failed = 0;

        foreach (var item in listings)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                if (IsIdentityConsistent(item.Retailer.WebsiteUrl, item.Listing.ListingUrl))
                {
                    item.Listing.MarkVerified();
                    item.Retailer.VerifyIdentity(item.Listing.SourceUrl);
                    verified++;
                }
                else
                {
                    item.Listing.MarkNeedsReview();
                    item.Retailer.MarkNeedsReview();
                    needsReview++;
                }

                await db.SaveChangesAsync(cancellationToken);
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
                    "Automated retailer identity verification failed for listing {ListingId}.",
                    item.Listing.Id);
            }
        }

        return new RetailerIdentityVerificationRunResult(
            listings.Count,
            verified,
            needsReview,
            failed);
    }

    private static bool IsIdentityConsistent(string? websiteUrl, string listingUrl)
    {
        if (!TryGetHttpUri(websiteUrl, out var website)
            || !TryGetHttpUri(listingUrl, out var listing))
            return false;

        var websiteHost = NormalizeHost(website!.Host);
        var listingHost = NormalizeHost(listing!.Host);

        return listingHost == websiteHost
            || listingHost.EndsWith("." + websiteHost, StringComparison.Ordinal);
    }

    private static bool TryGetHttpUri(string? value, out Uri? uri)
    {
        uri = null;
        return !string.IsNullOrWhiteSpace(value)
            && Uri.TryCreate(value.Trim(), UriKind.Absolute, out uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private static string NormalizeHost(string host)
    {
        var normalized = host.Trim().TrimEnd('.').ToLowerInvariant();
        return normalized.StartsWith("www.", StringComparison.Ordinal)
            ? normalized[4..]
            : normalized;
    }
}

public sealed class RetailerIdentityVerificationBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<RetailerIdentityVerificationJobOptions> options,
    ILogger<RetailerIdentityVerificationBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            logger.LogInformation("Retailer identity verification background job is disabled.");
            return;
        }

        var interval = TimeSpan.FromHours(Math.Max(1, options.Value.IntervalHours));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var verifier = scope.ServiceProvider.GetRequiredService<IRetailerIdentityVerifier>();
                var result = await verifier.RunOnceAsync(stoppingToken);

                logger.LogInformation(
                    "Retailer identity verification run completed: {EligibleListings} eligible, {VerifiedListings} verified, {NeedsReviewListings} needing review, {FailedListings} failed.",
                    result.EligibleListings,
                    result.VerifiedListings,
                    result.NeedsReviewListings,
                    result.FailedListings);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Retailer identity verification background run failed.");
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
