using System.Security.Claims;
using DiaperScout.Application;
using Microsoft.AspNetCore.Authentication;

namespace DiaperScout.Web.Services;

public static class AuthenticationSession
{
    public static Task SignInAsync(HttpContext context, PasswordlessAuthenticationResult authentication, IHostEnvironment environment)
    {
        var scheme = environment.IsDevelopment() ? "DevelopmentCookie" : "ProductionCookie";
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, authentication.UserId.ToString()),
            new(ClaimTypes.Name, authentication.Subject),
            new("sub", authentication.Subject),
            new("auth_time", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture))
        };
        if (authentication.ContinueOnboarding) claims.Add(new Claim("explorer_onboarding", "verified"));
        claims.AddRange(authentication.Roles.Select(role => new Claim(ClaimTypes.Role, role.ToString())));
        return context.SignInAsync(scheme, new ClaimsPrincipal(new ClaimsIdentity(claims, scheme)),
            new AuthenticationProperties { IsPersistent = true });
    }

    public static bool IsRecent(ClaimsPrincipal user) =>
        long.TryParse(user.FindFirstValue("auth_time"), out var seconds)
        && seconds <= DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        && seconds > DateTimeOffset.UtcNow.AddMinutes(-15).ToUnixTimeSeconds();
}
