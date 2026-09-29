using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.RateLimiting;
using DiaperScout.Application;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.RateLimiting;

namespace DiaperScout.Web.Services;

public static class PasskeyWebEndpoints
{
    private const string BindingCookie = "DiaperScout.PasskeyFlow";
    public static void AddPasskeyWebServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");
        services.AddHttpClient("PasskeyApi", client => client.BaseAddress = new Uri(configuration["Api:BaseUrl"] ?? "https+http://api"))
            .AddHttpMessageHandler<DevelopmentSubjectForwardingHandler>()
            .AddHttpMessageHandler<ProductionIdentityForwardingHandler>().AddServiceDiscovery();
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("passkeys", context => RateLimitPartition.GetFixedWindowLimiter(
                context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                    ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
                { PermitLimit = 60, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
        });
    }

    public static void MapPasskeyWebEndpoints(this WebApplication app)
    {
        var login = app.MapGroup("/signin/passkey").AllowAnonymous().RequireRateLimiting("passkeys").AddEndpointFilter<PasskeyRequestFilter>();
        login.MapPost("/options", (HttpContext context, IHttpClientFactory clients, CancellationToken ct) =>
            Begin(context, clients, false, ct));
        login.MapPost("/verify", async (BrowserPasskeyResponse request, HttpContext context, IHttpClientFactory clients, CancellationToken ct) =>
        {
            var binding = context.Request.Cookies[BindingCookie];
            if (string.IsNullOrEmpty(binding)) return Failure();
            using var response = await clients.CreateClient("PasskeyApi").PostAsJsonAsync("/api/v1/auth/passkeys/verify",
                new CompletePasskeyRequest(request.RequestId, binding, request.Credential), ct);
            context.Response.Cookies.Delete(BindingCookie, CookieOptions(context));
            if (!response.IsSuccessStatusCode) return Failure();
            var authentication = await response.Content.ReadFromJsonAsync<PasswordlessAuthenticationResult>(ct);
            if (authentication is null) return Failure();
            await AuthenticationSession.SignInAsync(context, authentication, app.Environment);
            return Results.Ok(new { redirect = "/" });
        });

        var account = app.MapGroup("/account/passkeys").RequireAuthorization().RequireRateLimiting("passkeys").AddEndpointFilter<PasskeyRequestFilter>();
        account.MapGet("/list", async (IHttpClientFactory clients, CancellationToken ct) =>
        {
            using var response = await clients.CreateClient("PasskeyApi").GetAsync("/api/v1/account/passkeys/", ct);
            return response.IsSuccessStatusCode
                ? Results.Ok(await response.Content.ReadFromJsonAsync<PasskeyItem[]>(ct)) : Failure();
        });
        account.MapPost("/options", (HttpContext context, IHttpClientFactory clients, CancellationToken ct) =>
            Begin(context, clients, true, ct));
        account.MapPost("/verify", async (BrowserPasskeyResponse request, HttpContext context, IHttpClientFactory clients, CancellationToken ct) =>
        {
            if (!AuthenticationSession.IsRecent(context.User)) return Reauthenticate();
            var binding = context.Request.Cookies[BindingCookie];
            if (string.IsNullOrEmpty(binding)) return Failure();
            using var response = await clients.CreateClient("PasskeyApi").PostAsJsonAsync("/api/v1/account/passkeys/verify",
                new CompletePasskeyRequest(request.RequestId, binding, request.Credential, request.Name), ct);
            context.Response.Cookies.Delete(BindingCookie, CookieOptions(context));
            return response.IsSuccessStatusCode ? Results.Ok() : Failure();
        });
        account.MapDelete("/{id:guid}", async (Guid id, HttpContext context, IHttpClientFactory clients, CancellationToken ct) =>
        {
            if (!AuthenticationSession.IsRecent(context.User)) return Reauthenticate();
            using var response = await clients.CreateClient("PasskeyApi").DeleteAsync($"/api/v1/account/passkeys/{id}", ct);
            return response.IsSuccessStatusCode ? Results.NoContent() : Failure();
        });
    }

    private static async Task<IResult> Begin(HttpContext context, IHttpClientFactory clients, bool registration, CancellationToken ct)
    {
        if (registration && !AuthenticationSession.IsRecent(context.User)) return Reauthenticate();
        var binding = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        context.Response.Cookies.Append(BindingCookie, binding, CookieOptions(context));
        using var response = await clients.CreateClient("PasskeyApi").PostAsJsonAsync(
            registration ? "/api/v1/account/passkeys/options" : "/api/v1/auth/passkeys/options", new BeginPasskeyRequest(binding), ct);
        return response.IsSuccessStatusCode ? Results.Ok(await response.Content.ReadFromJsonAsync<PasskeyOptions>(ct)) : Failure();
    }

    private static CookieOptions CookieOptions(HttpContext context) => new()
    {
        HttpOnly = true, Secure = context.Request.IsHttps || !context.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment(),
        SameSite = SameSiteMode.Strict, Path = "/", MaxAge = TimeSpan.FromMinutes(5), IsEssential = true
    };
    private static IResult Failure() => Results.BadRequest(new { message = "The passkey request could not be completed. Please try again or use an email sign-in link." });
    private static IResult Reauthenticate() => Results.Json(new { message = "Please sign in again before adding or removing a passkey." }, statusCode: 401);

    private sealed class PasskeyRequestFilter : IEndpointFilter
    {
        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            var http = context.HttpContext;
            http.Response.Headers.CacheControl = "no-store";
            if (http.Request.ContentLength > 40000) return Results.StatusCode(413);
            if (!HttpMethods.IsGet(http.Request.Method))
            {
                try { await http.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(http); }
                catch (AntiforgeryValidationException) { return Results.BadRequest(new { message = "Please refresh the page and try again." }); }
            }
            try { return await next(context); }
            catch (HttpRequestException) { return Results.Json(new { message = "The sign-in service is temporarily unavailable. Please try again." }, statusCode: 503); }
        }
    }
}

public sealed record BrowserPasskeyResponse(Guid RequestId, JsonElement Credential, string? Name = null);
