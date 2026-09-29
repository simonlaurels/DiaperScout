using DiaperScout.Application;
using DiaperScout.Domain;
using DiaperScout.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DiaperScout.Infrastructure;

internal sealed class UserManagement(DiaperScoutDbContext db) : IUserManagement
{
    private const string RegistrationEnabledKey = "registration.enabled";

    public async Task<IReadOnlyList<UserManagementItem>> GetUsersAsync(AuthenticatedUser actor, CancellationToken cancellationToken = default)
    {
        await RequireAdministratorAsync(actor, cancellationToken);

        var users = await (
            from user in db.Users.AsNoTracking()
            join email in db.UserEmails.AsNoTracking() on user.Id equals email.UserId
            // An authenticated account can have privileged roles before it has an Explorer profile.
            join explorerProfile in db.ExplorerProfiles.AsNoTracking() on user.Id equals explorerProfile.UserId into profiles
            from profile in profiles.DefaultIfEmpty()
            select new
            {
                user.Id,
                email.Email,
                DisplayName = profile == null ? string.Empty : profile.DisplayName,
                user.Status,
                JoinedAtUtc = email.CreatedAtUtc
            })
            .OrderBy(value => value.DisplayName)
            .ThenBy(value => value.Email)
            .ToListAsync(cancellationToken);

        var userIds = users.Select(value => value.Id).ToArray();
        var roles = await db.PrivilegedRoleAssignments
            .AsNoTracking()
            .Where(value => userIds.Contains(value.UserId) && value.RevokedAtUtc == null)
            .Select(value => new { value.UserId, value.Role })
            .ToListAsync(cancellationToken);

        return users.Select(user => new UserManagementItem(
            user.Id,
            user.Email,
            user.DisplayName,
            user.Status,
            user.JoinedAtUtc,
            roles.Where(role => role.UserId == user.Id).Select(role => role.Role).OrderBy(role => role).ToList()))
            .ToList();
    }

    public async Task<RegistrationSettingsItem> GetRegistrationSettingsAsync(AuthenticatedUser actor, CancellationToken cancellationToken = default)
    {
        await RequireAdministratorAsync(actor, cancellationToken);
        var setting = await db.PlatformSettings.AsNoTracking().SingleOrDefaultAsync(value => value.Key == RegistrationEnabledKey, cancellationToken);
        return new RegistrationSettingsItem(setting is null || !bool.TryParse(setting.Value, out var enabled) ? true : enabled);
    }

    public async Task<RegistrationSettingsItem> SetRegistrationEnabledAsync(AuthenticatedUser actor, bool enabled, CancellationToken cancellationToken = default)
    {
        await RequireAdministratorAsync(actor, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var setting = await db.PlatformSettings.SingleOrDefaultAsync(value => value.Key == RegistrationEnabledKey, cancellationToken);

        if (setting is null)
            db.PlatformSettings.Add(new PlatformSetting(RegistrationEnabledKey, enabled.ToString(), now));
        else
            setting.UpdateValue(enabled.ToString(), now);

        await db.SaveChangesAsync(cancellationToken);
        return new RegistrationSettingsItem(enabled);
    }

    private async Task RequireAdministratorAsync(AuthenticatedUser actor, CancellationToken cancellationToken)
    {
        if (!await db.PrivilegedRoleAssignments.AnyAsync(
                assignment => assignment.UserId == actor.UserId && assignment.Role == PrivilegedRole.Administrator && assignment.RevokedAtUtc == null,
                cancellationToken))
            throw new UnauthorizedAccessException("Only an assigned Administrator may manage users.");
    }
}
