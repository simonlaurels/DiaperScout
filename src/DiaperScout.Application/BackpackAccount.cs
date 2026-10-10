namespace DiaperScout.Application;

public sealed record BackpackAccountInfo(string? DisplayName, string? Email);
public sealed record UpdateExplorerName(string DisplayName);
public sealed record PersonalDiscovery(string Kind, string Name, string Status, DateTimeOffset OccurredAtUtc, string? Url);
public sealed record PersonalCatalogueDraft(string Name, string Url, DateTimeOffset UpdatedAtUtc);
public sealed record PersonalCatalogueDrafts(int TotalCount, IReadOnlyList<PersonalCatalogueDraft> Items);
public interface IBackpackAccount
{
    Task<BackpackAccountInfo> AccountAsync(AuthenticatedUser actor, CancellationToken ct = default);
    Task UpdateNameAsync(AuthenticatedUser actor, UpdateExplorerName request, CancellationToken ct = default);
    Task<IReadOnlyList<PersonalDiscovery>> DiscoveriesAsync(AuthenticatedUser actor, CancellationToken ct = default);
    Task<PersonalCatalogueDrafts> DraftsAsync(AuthenticatedUser actor, CancellationToken ct = default);
}
