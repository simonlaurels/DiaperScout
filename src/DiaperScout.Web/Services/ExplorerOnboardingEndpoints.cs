using System.Security.Claims;
using System.Security.Cryptography;
using DiaperScout.Application;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;

namespace DiaperScout.Web.Services;

public sealed record ExplorerOnboardingDetails(string DisplayName, string Email);

public static class ExplorerOnboardingEndpoints
{
    private const string PendingCookie = "DiaperScout.PendingExplorer";
    public static void MapExplorerOnboardingEndpoints(this WebApplication app)
    {
        var pending = app.MapGroup("/join/data").AllowAnonymous().AddEndpointFilter<OnboardingFilter>();
        pending.MapGet("/pending", (HttpContext context) => ReadPending(context) is { } details
            ? Results.Ok(new { email = details.Email }) : Results.NotFound());
        pending.MapPost("/request", async (ExplorerOnboardingDetails details, HttpContext context, IHttpClientFactory clients, CancellationToken ct) =>
            await SendAsync(context, clients, details, ct) ? Results.Ok() : Error("Check your name or nickname and email, and allow a minute between requests. If account creation is closed, please return later."))
            .RequireRateLimiting("onboarding-email");
        pending.MapPost("/resend", async (HttpContext context, IHttpClientFactory clients, CancellationToken ct) =>
            ReadPending(context) is { } details && await SendAsync(context, clients, details, ct)
                ? Results.Ok() : Error("We couldn’t resend the link. Try again, or use a different email."))
            .RequireRateLimiting("onboarding-email");

        var verified = app.MapGroup("/join/data").RequireAuthorization().AddEndpointFilter<OnboardingFilter>()
            .AddEndpointFilter(async (context, next) =>
                context.HttpContext.User.HasClaim(c => c.Type == "explorer_onboarding")
                    ? await next(context) : Results.Json(new { message = "Verify your email to continue.", restart = true }, statusCode: 403));
        verified.MapGet("/state", async (HttpContext context, IHttpClientFactory clients, CancellationToken ct) =>
        {
            var client = clients.CreateClient("PasskeyApi");
            using var accountResponse = await client.GetAsync("/api/v1/me/backpack/account", ct);
            using var keysResponse = await client.GetAsync("/api/v1/account/passkeys/", ct);
            if (!accountResponse.IsSuccessStatusCode || !keysResponse.IsSuccessStatusCode) return Results.StatusCode(503);
            var account = await accountResponse.Content.ReadFromJsonAsync<BackpackAccountInfo>(ct);
            var keys = await keysResponse.Content.ReadFromJsonAsync<PasskeyItem[]>(ct);
            return Results.Ok(new { displayName = account?.DisplayName, email = account?.Email,
                hasPasskey = keys?.Length > 0, stage = context.User.FindFirstValue("explorer_onboarding"),
                recent = AuthenticationSession.IsRecent(context.User) });
        });
        verified.MapPost("/complete", async (OnboardingCompletion request, HttpContext context, IHttpClientFactory clients, CancellationToken ct) =>
        {
            var client = clients.CreateClient("PasskeyApi");
            using var accountResponse = await client.GetAsync("/api/v1/me/backpack/account", ct);
            using var keysResponse = await client.GetAsync("/api/v1/account/passkeys/", ct);
            if (!accountResponse.IsSuccessStatusCode || !keysResponse.IsSuccessStatusCode) return Results.StatusCode(503);
            var account = await accountResponse.Content.ReadFromJsonAsync<BackpackAccountInfo>(ct);
            var keys = await keysResponse.Content.ReadFromJsonAsync<PasskeyItem[]>(ct);
            if (string.IsNullOrWhiteSpace(account?.Email) || string.IsNullOrWhiteSpace(account.DisplayName))
                return Error("Save your name and verify your email before continuing.");
            if (!request.Skip && keys?.Length is not > 0) return Error("Create a passkey or choose Maybe later.");
            var identity = (ClaimsIdentity)context.User.Identity!;
            foreach (var claim in identity.FindAll("explorer_onboarding").ToArray()) identity.RemoveClaim(claim);
            identity.AddClaim(new("explorer_onboarding", request.Skip ? "skipped" : "passkey"));
            var scheme = app.Environment.IsDevelopment() ? "DevelopmentCookie" : "ProductionCookie";
            var ticket = await context.AuthenticateAsync(scheme);
            await context.SignInAsync(scheme, context.User, ticket.Properties);
            context.Response.Cookies.Delete(PendingCookie);
            return Results.Ok();
        }).RequireRateLimiting("passkeys");
    }

    public static async Task<bool> SendAsync(HttpContext context, IHttpClientFactory clients, ExplorerOnboardingDetails details, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(details.DisplayName) || details.DisplayName.Trim().Length > 100 ||
            string.IsNullOrWhiteSpace(details.Email) || details.Email.Trim().Length > 320) return false;
        using var response = await clients.CreateClient("DiaperScoutApi").PostAsJsonAsync("/api/v1/auth/registration-link", details, ct);
        if (!response.IsSuccessStatusCode) return false;
        var protector = Protector(context);
        context.Response.Cookies.Append(PendingCookie, protector.Protect(System.Text.Json.JsonSerializer.Serialize(details), TimeSpan.FromMinutes(30)),
            new CookieOptions { HttpOnly = true, Secure = context.Request.IsHttps || !context.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment(),
                SameSite = SameSiteMode.Lax, Path = "/", MaxAge = TimeSpan.FromMinutes(30), IsEssential = true });
        return true;
    }
    private static ITimeLimitedDataProtector Protector(HttpContext context) => context.RequestServices.GetRequiredService<IDataProtectionProvider>()
        .CreateProtector("ExplorerOnboarding.Pending.v1").ToTimeLimitedDataProtector();
    private static ExplorerOnboardingDetails? ReadPending(HttpContext context)
    {
        if (context.Request.Cookies[PendingCookie] is not { } value) return null;
        try { return System.Text.Json.JsonSerializer.Deserialize<ExplorerOnboardingDetails>(Protector(context).Unprotect(value)); }
        catch (CryptographicException) { return null; }
        catch (System.Text.Json.JsonException) { return null; }
    }
    private static IResult Error(string message) => Results.BadRequest(new { message });
    private sealed record OnboardingCompletion(bool Skip);
    private sealed class OnboardingFilter : IEndpointFilter
    {
        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            var http = context.HttpContext;
            http.Response.Headers.CacheControl = "no-store";
            if (http.Request.ContentLength > 8000) return Results.StatusCode(413);
            if (!HttpMethods.IsGet(http.Request.Method))
            {
                try { await http.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(http); }
                catch (AntiforgeryValidationException) { return Error("Refresh the page and try again."); }
            }
            try { return await next(context); }
            catch (HttpRequestException) { return Results.Json(new { message = "DiaperScout couldn’t connect just now. Please try again." }, statusCode: 503); }
            catch (TaskCanceledException) when (!http.RequestAborted.IsCancellationRequested)
            { return Results.Json(new { message = "That took longer than expected. Please try again." }, statusCode: 503); }
        }
    }
}
