using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Components.Authorization;

namespace DiaperScout.Web.Services;

public sealed class ProductionIdentityForwardingHandler(
    IHttpContextAccessor httpContextAccessor,
    AuthenticationStateProvider authenticationStateProvider,
    IConfiguration configuration)
    : DelegatingHandler
{
    private const string HeaderName = "X-DiaperScout-Identity";
    private const int AssertionLifetimeSeconds = 60;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync();

        if (user?.Identity?.IsAuthenticated == true)
        {
            var secret = configuration["Authentication:Production:InternalSecret"];

            if (!string.IsNullOrWhiteSpace(secret))
            {
                var userId =
                    user.FindFirstValue(ClaimTypes.NameIdentifier) ??
                    user.FindFirstValue("sub");

                var subject =
                    user.FindFirstValue("sub") ??
                    user.FindFirstValue(ClaimTypes.Name);

                if (Guid.TryParse(userId, out var parsedUserId) &&
                    !string.IsNullOrWhiteSpace(subject))
                {
                    var roles = user
                        .FindAll(ClaimTypes.Role)
                        .Select(claim => claim.Value)
                        .Where(role =>
                            string.Equals(role, "Moderator", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(role, "Administrator", StringComparison.OrdinalIgnoreCase))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(role => role, StringComparer.OrdinalIgnoreCase);

                    var expiry = DateTimeOffset.UtcNow
                        .AddSeconds(AssertionLifetimeSeconds)
                        .ToUnixTimeSeconds();

                    var payload =
                        $"{parsedUserId}|{subject}|{expiry}|{string.Join(",", roles)}";

                    Console.WriteLine(
                        $"Production identity assertion field count: {payload.Split('|').Length}; subject present: {!string.IsNullOrWhiteSpace(subject)}; user id valid: {parsedUserId != Guid.Empty}");

                    var payloadBytes = Encoding.UTF8.GetBytes(payload);

                    var signature = HMACSHA256.HashData(
                        Encoding.UTF8.GetBytes(secret),
                        payloadBytes);

                    var assertion =
                        $"{Base64UrlEncode(payloadBytes)}.{Base64UrlEncode(signature)}";

                    request.Headers.Remove(HeaderName);
                    request.Headers.TryAddWithoutValidation(
                        HeaderName,
                        assertion);
                }
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }

    private async Task<ClaimsPrincipal?> GetCurrentUserAsync()
    {
        var httpContextUser = httpContextAccessor.HttpContext?.User;

        if (httpContextUser?.Identity?.IsAuthenticated == true)
        {
            return httpContextUser;
        }

        try
        {
            var authenticationState =
                await authenticationStateProvider.GetAuthenticationStateAsync();

            return authenticationState.User;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static string Base64UrlEncode(byte[] value) =>
        Convert.ToBase64String(value)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
