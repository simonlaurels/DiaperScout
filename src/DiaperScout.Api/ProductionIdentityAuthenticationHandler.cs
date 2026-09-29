using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace DiaperScout.Api;

public sealed class ProductionIdentityAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    private const string HeaderName = "X-DiaperScout-Identity";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out var suppliedValue) ||
            string.IsNullOrWhiteSpace(suppliedValue))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var secret = configuration["Authentication:Production:InternalSecret"];

        if (string.IsNullOrWhiteSpace(secret))
        {
            return Task.FromResult(
                AuthenticateResult.Fail("Production authentication secret is not configured."));
        }

        var parts = suppliedValue.ToString().Split('.', 2);

        if (parts.Length != 2)
        {
            return Task.FromResult(
                AuthenticateResult.Fail("Invalid production identity assertion."));
        }

        try
        {
            var payloadBytes = Base64UrlDecode(parts[0]);
            var suppliedSignature = Base64UrlDecode(parts[1]);

            var expectedSignature = HMACSHA256.HashData(
                Encoding.UTF8.GetBytes(secret),
                payloadBytes);

            if (!CryptographicOperations.FixedTimeEquals(
                    suppliedSignature,
                    expectedSignature))
            {
                return Task.FromResult(
                    AuthenticateResult.Fail("Invalid production identity signature."));
            }

            var payload = Encoding.UTF8.GetString(payloadBytes);
            Logger.LogWarning(
                "Production identity assertion payload field count: {FieldCount}",
                payload.Split('|').Length);
            var fields = payload.Split('|');

            if (fields.Length < 4)
            {
                return Task.FromResult(
                    AuthenticateResult.Fail("Invalid production identity payload."));
            }

            var userId = fields[0];
            var subject = fields[1];
            var expiresAtUnix = fields[2];
            var roles = fields[3];

            if (!Guid.TryParse(userId, out var parsedUserId))
            {
                return Task.FromResult(
                    AuthenticateResult.Fail("Invalid production identity user ID."));
            }

            if (string.IsNullOrWhiteSpace(subject))
            {
                return Task.FromResult(
                    AuthenticateResult.Fail("Invalid production identity subject."));
            }

            if (!long.TryParse(expiresAtUnix, out var expiry))
            {
                return Task.FromResult(
                    AuthenticateResult.Fail("Invalid production identity expiry."));
            }

            if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expiry)
            {
                return Task.FromResult(
                    AuthenticateResult.Fail("Production identity assertion has expired."));
            }

            var claims = new List<Claim>
            {
                new("sub", subject),
                new(ClaimTypes.NameIdentifier, parsedUserId.ToString()),
                new(ClaimTypes.Name, subject)
            };

            foreach (var role in roles.Split(
                         ',',
                         StringSplitOptions.RemoveEmptyEntries |
                         StringSplitOptions.TrimEntries))
            {
                if (string.Equals(role, "Moderator", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(role, "Administrator", StringComparison.OrdinalIgnoreCase))
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }
            }

            var identity = new ClaimsIdentity(
                claims,
                Scheme.Name);

            return Task.FromResult(
                AuthenticateResult.Success(
                    new AuthenticationTicket(
                        new ClaimsPrincipal(identity),
                        Scheme.Name)));
        }
        catch (FormatException)
        {
            return Task.FromResult(
                AuthenticateResult.Fail("Invalid production identity encoding."));
        }
    }

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value
            .Replace('-', '+')
            .Replace('_', '/');

        padded = padded.PadRight(
            padded.Length + ((4 - padded.Length % 4) % 4),
            '=');

        return Convert.FromBase64String(padded);
    }
}
