using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace DiaperScout.Api;

/// <summary>Development-only local authentication. It is never registered outside Development.</summary>
public sealed class DevelopmentHeaderAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Development-Subject", out var suppliedSubject) ||
            string.IsNullOrWhiteSpace(suppliedSubject))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new List<Claim>
        {
            new("sub", suppliedSubject.ToString())
        };

        if (Request.Headers.TryGetValue("X-Development-Role", out var suppliedRole) &&
            (string.Equals(suppliedRole.ToString(), "Moderator", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(suppliedRole.ToString(), "Administrator", StringComparison.OrdinalIgnoreCase)))
        {
            claims.Add(new Claim(ClaimTypes.Role, suppliedRole.ToString()));
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        return Task.FromResult(
            AuthenticateResult.Success(
                new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }
}
