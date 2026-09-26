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

    public async Task RequestMagicLinkAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(email);

        var userEmail = await db.UserEmails
            .SingleOrDefaultAsync(
                item => item.Email == normalizedEmail,
                cancellationToken);

        User user;

        if (userEmail is null)
        {
            user = new User(Guid.NewGuid().ToString("N"));
            userEmail = new UserEmail(user.Id, normalizedEmail);

            db.Users.Add(user);
            db.UserEmails.Add(userEmail);

            await db.SaveChangesAsync(cancellationToken);
        }
        else
        {
            user = await db.Users
                .SingleAsync(
                    item => item.Id == userEmail.UserId,
                    cancellationToken);
        }

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

        var consumed = await db.MagicLinkTokens
            .Where(item =>
                item.TokenHash == tokenHash &&
                item.UsedAtUtc == null &&
                item.ExpiresAtUtc > now)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    item => item.UsedAtUtc,
                    now),
                cancellationToken);

        if (consumed != 1)
            return null;

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
