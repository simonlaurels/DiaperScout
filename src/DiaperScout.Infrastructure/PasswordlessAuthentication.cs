using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using DiaperScout.Application;
using DiaperScout.Domain;
using DiaperScout.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Resend;

namespace DiaperScout.Infrastructure;

internal sealed class PasswordlessAuthentication(
    DiaperScoutDbContext db,
    IResend resend,
    TimeProvider timeProvider,
    IConfiguration configuration) : IPasswordlessAuthentication
{
    private const int DefaultMagicLinkLifetimeMinutes = 15;
    private const int TokenByteLength = 32;
    private const string RegistrationEnabledSettingKey = "registration.enabled";

    private readonly int magicLinkLifetimeMinutes = GetLifetimeMinutes(configuration);
    private readonly string baseUrl = GetRequiredSetting(
        configuration,
        "Authentication:MagicLink:BaseUrl");
    private readonly string fromEmail = GetRequiredSetting(
        configuration,
        "Resend:FromEmail");

    public async Task RequestRegistrationLinkAsync(
        string email,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(email);
        var normalizedDisplayName = NormalizeDisplayName(displayName);

        if (!await IsRegistrationEnabledAsync(cancellationToken))
        {
            throw new InvalidOperationException(
                "Registrations are currently closed. Please check back later.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({normalizedEmail}, 0))", cancellationToken);
        var existingUser = await (
            from userEmail in db.UserEmails.AsNoTracking()
            join user in db.Users.AsNoTracking() on userEmail.UserId equals user.Id
            where userEmail.Email == normalizedEmail select user).SingleOrDefaultAsync(cancellationToken);
        if (existingUser is not null && existingUser.Status != UserAccountStatus.Active) return;
        var now = timeProvider.GetUtcNow();
        var expiresAtUtc = now.AddMinutes(magicLinkLifetimeMinutes);
        var pendingRegistration = await db.PendingRegistrations.SingleOrDefaultAsync(p => p.Email == normalizedEmail, cancellationToken);
        if (pendingRegistration is not null && pendingRegistration.ExpiresAtUtc > now && pendingRegistration.CreatedAtUtc > now.AddSeconds(-60))
        {
            if (pendingRegistration.DisplayName != normalizedDisplayName)
                throw new ArgumentException("Wait a minute before changing these details and requesting another link.", nameof(displayName));
            return;
        }
        if (pendingRegistration is null)
        {
            pendingRegistration = new PendingRegistration(normalizedEmail, normalizedDisplayName, now, expiresAtUtc);
            db.PendingRegistrations.Add(pendingRegistration);
        }
        else
        {
            // A resend supersedes unused older links, so a link can never acquire a later
            // request's pending nickname. Both resend and consumption hold the email lock.
            await db.PendingRegistrationTokens.Where(t => t.PendingRegistrationId == pendingRegistration.Id && t.UsedAtUtc == null)
                .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.UsedAtUtc, now), cancellationToken);
            pendingRegistration.Renew(normalizedDisplayName, now, expiresAtUtc);
        }

        var token = CreateToken();
        var tokenHash = HashToken(token);

        var pendingRegistrationToken = new PendingRegistrationToken(
            pendingRegistration.Id,
            tokenHash,
            expiresAtUtc);

        db.PendingRegistrationTokens.Add(pendingRegistrationToken);

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var link = BuildMagicLink(token) + "&flow=explorer";
        var encodedLink = HtmlEncoder.Default.Encode(link);

        var message = new EmailMessage
        {
            From = fromEmail,
            Subject = "Join DiaperScout",
            HtmlBody = $"""
                <p>Someone requested to create a DiaperScout account using this email address.</p>
                <p><a href="{encodedLink}">Join DiaperScout</a></p>
                <p>This link expires in {magicLinkLifetimeMinutes} minutes and can only be used once.</p>
                <p>If you did not request this, you can safely ignore this email.</p>
                """
        };

        message.To.Add(normalizedEmail);

        try
        {
            var delivery = await resend.EmailSendAsync(message, cancellationToken);
            if (!delivery.Success) throw new HttpRequestException("The verification email could not be sent.");
        }
        catch (Exception e) when (e is ResendException or HttpRequestException or OperationCanceledException)
        {
            // A failed send must not make a retry claim that an email was delivered. Keep
            // the same pending record, but retire the undelivered token and allow a resend.
            await db.PendingRegistrations.Where(p => p.Id == pendingRegistration.Id && p.CreatedAtUtc == now)
                .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.ExpiresAtUtc, now), CancellationToken.None);
            await db.PendingRegistrationTokens.Where(t => t.Id == pendingRegistrationToken.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.UsedAtUtc, now), CancellationToken.None);
            throw;
        }
    }

    public async Task RequestSignInLinkAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(email);

        var userEmail = await db.UserEmails
            .SingleOrDefaultAsync(
                item => item.Email == normalizedEmail,
                cancellationToken);

        if (userEmail is null)
            return;

        var user = await db.Users
            .SingleAsync(
                item => item.Id == userEmail.UserId,
                cancellationToken);

        if (user.Status != UserAccountStatus.Active)
            return;

        var token = CreateToken();
        var tokenHash = HashToken(token);
        var now = timeProvider.GetUtcNow();
        var expiresAtUtc = now.AddMinutes(magicLinkLifetimeMinutes);

        db.MagicLinkTokens.Add(
            new MagicLinkToken(
                userEmail.Id,
                tokenHash,
                expiresAtUtc));

        await db.SaveChangesAsync(cancellationToken);

        var link = BuildMagicLink(token);
        var encodedLink = HtmlEncoder.Default.Encode(link);

        var message = new EmailMessage
        {
            From = fromEmail,
            Subject = "Sign in to DiaperScout",
            HtmlBody = $"""
                <p>Someone requested a sign-in link for DiaperScout.</p>
                <p><a href="{encodedLink}">Sign in to DiaperScout</a></p>
                <p>This link expires in {magicLinkLifetimeMinutes} minutes and can only be used once.</p>
                <p>If you did not request this, you can safely ignore this email.</p>
                """
        };

        message.To.Add(normalizedEmail);

        await resend.EmailSendAsync(message);
    }

    public async Task<PasswordlessAuthenticationResult?> ConsumeMagicLinkAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var tokenHash = HashToken(token);
        var now = timeProvider.GetUtcNow();

        var consumedExistingAccountToken = await db.MagicLinkTokens
            .Where(item =>
                item.TokenHash == tokenHash &&
                item.UsedAtUtc == null &&
                item.ExpiresAtUtc > now)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    item => item.UsedAtUtc,
                    now),
                cancellationToken);

        if (consumedExistingAccountToken == 1)
        {
            return await ConsumeExistingAccountMagicLinkAsync(
                tokenHash,
                cancellationToken);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var email = await (from t in db.PendingRegistrationTokens.AsNoTracking()
            join p in db.PendingRegistrations.AsNoTracking() on t.PendingRegistrationId equals p.Id
            where t.TokenHash == tokenHash select p.Email).SingleOrDefaultAsync(cancellationToken);
        if (email is null) return null;
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({email}, 0))", cancellationToken);
        var consumedRegistrationToken = await db.PendingRegistrationTokens
            .Where(item => item.TokenHash == tokenHash && item.UsedAtUtc == null && item.ExpiresAtUtc > now)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.UsedAtUtc, now), cancellationToken);
        if (consumedRegistrationToken != 1) return null;
        var result = await CompleteRegistrationAsync(tokenHash, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    private async Task<PasswordlessAuthenticationResult?> ConsumeExistingAccountMagicLinkAsync(
        string tokenHash,
        CancellationToken cancellationToken)
    {
        var identity = await (
            from magicLink in db.MagicLinkTokens.AsNoTracking()
            join userEmail in db.UserEmails.AsNoTracking()
                on magicLink.UserEmailId equals userEmail.Id
            join user in db.Users.AsNoTracking()
                on userEmail.UserId equals user.Id
            where magicLink.TokenHash == tokenHash
            select new
            {
                user.Id,
                user.Subject,
                user.Status
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (identity is null || identity.Status != UserAccountStatus.Active)
            return null;

        var roles = await db.PrivilegedRoleAssignments
            .AsNoTracking()
            .Where(value => value.UserId == identity.Id && value.RevokedAtUtc == null)
            .Select(value => value.Role)
            .ToListAsync(cancellationToken);

        return new PasswordlessAuthenticationResult(
            identity.Id,
            identity.Subject,
            roles);
    }

    private async Task<PasswordlessAuthenticationResult?> CompleteRegistrationAsync(
        string tokenHash,
        CancellationToken cancellationToken)
    {
        var pending = await (
            from registrationToken in db.PendingRegistrationTokens.AsNoTracking()
            join registration in db.PendingRegistrations.AsNoTracking()
                on registrationToken.PendingRegistrationId equals registration.Id
            where registrationToken.TokenHash == tokenHash
            select new
            {
                registration.Id,
                registration.Email,
                registration.DisplayName,
                registration.ExpiresAtUtc
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (pending is null)
            return null;

        var existingUser = await (
            from existingUserEmail in db.UserEmails.AsNoTracking()
            join existingAccount in db.Users.AsNoTracking()
                on existingUserEmail.UserId equals existingAccount.Id
            where existingUserEmail.Email == pending.Email
            select existingAccount)
            .SingleOrDefaultAsync(cancellationToken);

        var user = existingUser ?? new User(Guid.NewGuid().ToString("N"));
        if (user.Status != UserAccountStatus.Active) return null;

        if (existingUser is null)
        {
            db.Users.Add(user);
            db.UserEmails.Add(new UserEmail(user.Id, pending.Email));
            await db.SaveChangesAsync(cancellationToken);
        }
        // Preserve an existing profile. A conflicting public nickname is resolved by its
        // authenticated owner on continuation, never by fabricating a replacement name.
        try { await ExplorerProfileLifecycle.SetNameAsync(db, user.Id, pending.DisplayName, false, cancellationToken); }
        catch (CatalogueValidationException) { }
        var roles = await db.PrivilegedRoleAssignments.AsNoTracking()
            .Where(r => r.UserId == user.Id && r.RevokedAtUtc == null).Select(r => r.Role).ToListAsync(cancellationToken);
        return new PasswordlessAuthenticationResult(
            user.Id,
            user.Subject,
            roles,
            ContinueOnboarding: true);
    }

    private async Task<bool> IsRegistrationEnabledAsync(
        CancellationToken cancellationToken)
    {
        var setting = await db.PlatformSettings
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Key == RegistrationEnabledSettingKey,
                cancellationToken);

        return setting is null ||
               !bool.TryParse(setting.Value, out var enabled) ||
               enabled;
    }

    private string BuildMagicLink(string token)
    {
        var separator = baseUrl.Contains(
            '?',
            StringComparison.Ordinal)
            ? "&"
            : "?";

        return $"{baseUrl}{separator}token={Uri.EscapeDataString(token)}";
    }

    private static string NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException(
                "An email address is required.",
                nameof(email));

        var normalized = email.Trim().ToLowerInvariant();

        if (normalized.Length > 320 || !LooksLikeEmail(normalized))
            throw new ArgumentException(
                "A valid email address is required.",
                nameof(email));

        return normalized;
    }

    private static string NormalizeDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException(
                "Enter your name or nickname.",
                nameof(displayName));

        var normalized = displayName.Trim();

        if (normalized.Length > 100)
            throw new ArgumentException(
                "Your name or nickname must be 100 characters or fewer.",
                nameof(displayName));

        return normalized;
    }

    private static bool LooksLikeEmail(string email)
    {
        var atIndex = email.IndexOf('@');

        return atIndex > 0 &&
               atIndex < email.Length - 1 &&
               email.IndexOf('@', atIndex + 1) == -1 &&
               email.Contains('.', StringComparison.Ordinal);
    }

    private static string CreateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(TokenByteLength);

        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    private static string HashToken(string token)
    {
        var hash = SHA256.HashData(
            Encoding.UTF8.GetBytes(token));

        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static int GetLifetimeMinutes(IConfiguration configuration)
    {
        var configured = configuration.GetValue<int?>(
            "Authentication:MagicLink:LifetimeMinutes");

        return configured is > 0
            ? configured.Value
            : DefaultMagicLinkLifetimeMinutes;
    }

    private static string GetRequiredSetting(
        IConfiguration configuration,
        string key)
    {
        var value = configuration[key];

        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException(
                $"Configuration '{key}' is required.");

        return value.Trim();
    }
}
