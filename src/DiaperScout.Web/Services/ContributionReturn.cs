using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography;
namespace DiaperScout.Web.Services;
public static class ContributionReturn
{
    private const string CookieName = "DiaperScout.ContributionReturn";
    public static string? Validate(string? url)
    {
        if (string.IsNullOrEmpty(url) || url.Length > 1024 || url.Contains('\\') || url.Any(char.IsControl) || url.StartsWith("//")) return null;
        var path = url.Split('?')[0];
        return path is "/observations/new" or "/contribute/product" ? url : null;
    }
    public static void Remember(HttpContext context)
    {
        var target = Validate(context.Request.Query["returnUrl"]);
        if (target is null) return;
        var protector = context.RequestServices.GetRequiredService<IDataProtectionProvider>().CreateProtector("ContributionReturn.v1").ToTimeLimitedDataProtector();
        context.Response.Cookies.Append(CookieName, protector.Protect(target, TimeSpan.FromMinutes(30)), new CookieOptions
        { HttpOnly = true, Secure = context.Request.IsHttps || !context.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment(), SameSite = SameSiteMode.Lax, Path = "/", MaxAge = TimeSpan.FromMinutes(30), IsEssential = true });
    }
    public static string Consume(HttpContext context, string fallback = "/")
    {
        var value = context.Request.Cookies[CookieName]; context.Response.Cookies.Delete(CookieName);
        if (string.IsNullOrEmpty(value)) return fallback;
        try
        {
            var protector = context.RequestServices.GetRequiredService<IDataProtectionProvider>().CreateProtector("ContributionReturn.v1").ToTimeLimitedDataProtector();
            return Validate(protector.Unprotect(value)) ?? fallback;
        }
        catch (CryptographicException) { return fallback; }
    }
}
