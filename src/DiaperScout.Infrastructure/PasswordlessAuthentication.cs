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

        var existingUser = await (
            from userEmail in db.UserEmails.AsNoTracking()
            join user in db.Users.AsNoTracking()
                on userEmail.UserId equals user.Id
            where userEmail.Email == normalizedEmail
            select user)
            .SingleOrDefaultAsync(cancellationToken);

        if (existingUser is not null)
        {
            throw new InvalidOperationException(
                "An account already exists for this email address. Please sign in instead.");
        }

        var now = timeProvider.GetUtcNow();
        var expiresAtUtc = now.AddMinutes(magicLinkLifetimeMinutes);

        var pendingRegistration = new PendingRegistration(
            normalizedEmail,
            normalizedDisplayName,
            now,
            expiresAtUtc);

        var token = CreateToken();
        var tokenHash = HashToken(token);

        var pendingRegistrationToken = new PendingRegistrationToken(
            pendingRegistration.Id,
            tokenHash,
            expiresAtUtc);

        db.PendingRegistrations.Add(pendingRegistration);
        db.PendingRegistrationTokens.Add(pendingRegistrationToken);

        await db.SaveChangesAsync(cancellationToken);

        var link = BuildMagicLink(token);
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

        await resend.EmailSendAsync(message);
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

        var consumedRegistrationToken = await db.PendingRegistrationTokens
            .Where(item =>
                item.TokenHash == tokenHash &&
                item.UsedAtUtc == null &&
                item.ExpiresAtUtc > now)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    item => item.UsedAtUtc,
                    now),
                cancellationToken);

        if (consumedRegistrationToken != 1)
            return null;

        return await CompleteRegistrationAsync(
            tokenHash,
            cancellationToken);
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

        return new PasswordlessAuthenticationResult(
            identity.Id,
            identity.Subject);
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

        if (existingUser is not null)
            return null;

        await using var transaction =
            await db.Database.BeginTransactionAsync(cancellationToken);

        var user = new User(Guid.NewGuid().ToString("N"));

        var userEmail = new UserEmail(
            user.Id,
            pending.Email);

        var explorerProfile = new ExplorerProfile(
            user.Id,
            pending.DisplayName);

        var backpack = new Backpack(explorerProfile.Id);

        db.Users.Add(user);
        db.UserEmails.Add(userEmail);
        db.ExplorerProfiles.Add(explorerProfile);
        db.Backpacks.Add(backpack);

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new PasswordlessAuthenticationResult(
            user.Id,
            user.Subject);
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
                "An Explorer Display Name is required.",
                nameof(displayName));

        var normalized = displayName.Trim();

        if (normalized.Length > 100)
            throw new ArgumentException(
                "The Explorer Display Name must be 100 characters or fewer.",
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
