using DiaperScout.Domain;

namespace DiaperScout.Application;

public sealed record UserManagementItem(
    Guid Id,
    string Email,
    string DisplayName,
    UserAccountStatus Status,
    DateTimeOffset JoinedAtUtc,
    IReadOnlyList<PrivilegedRole> Roles);

public sealed record RegistrationSettingsItem(
    bool RegistrationEnabled);

public sealed record SetRegistrationEnabledRequest(
    bool Enabled);

public interface IUserManagement
{
    Task<IReadOnlyList<UserManagementItem>> GetUsersAsync(
        AuthenticatedUser actor,
        CancellationToken cancellationToken = default);

    Task<RegistrationSettingsItem> GetRegistrationSettingsAsync(
        AuthenticatedUser actor,
        CancellationToken cancellationToken = default);

    Task<RegistrationSettingsItem> SetRegistrationEnabledAsync(
        AuthenticatedUser actor,
        bool enabled,
        CancellationToken cancellationToken = default);
}