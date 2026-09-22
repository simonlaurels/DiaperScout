using DiaperScout.Domain;

namespace DiaperScout.Application;

public sealed record RetailerAffiliateProgrammeDiscoveryTarget(
    Guid RetailerId,
    string RetailerName,
    string? RetailerWebsiteUrl);

public sealed record RetailerAffiliateProgrammeDiscoveryCandidate(
    string Network,
    string ProgrammeId,
    string ProgrammeName,
    AffiliateProgrammeStatus Status,
    string? ProgrammeUrl,
    string? TermsUrl,
    string? ReferralTerms,
    int? CookieDurationDays,
    bool? DeepLinksAllowed,
    bool ApplicationRequired,
    string? SourceUrl);

public interface IRetailerAffiliateProgrammeDiscoveryProvider
{
    Task<IReadOnlyList<RetailerAffiliateProgrammeDiscoveryCandidate>> DiscoverAsync(
        RetailerAffiliateProgrammeDiscoveryTarget retailer,
        CancellationToken cancellationToken = default);
}

public interface IRetailerAffiliateProgrammeDiscovery
{
    Task<IReadOnlyList<RetailerAffiliateProgrammeItem>> DiscoverAndRecordAsync(
        Guid retailerId,
        CancellationToken cancellationToken = default);
}

public sealed record RetailerAffiliateProgrammeDiscoveryRunResult(
    int EligibleRetailers,
    int SucceededRetailers,
    int DiscoveredProgrammes,
    int FailedRetailers);

public interface IRetailerAffiliateProgrammeDiscoveryScheduler
{
    Task<RetailerAffiliateProgrammeDiscoveryRunResult> RunOnceAsync(
        CancellationToken cancellationToken = default);
}
