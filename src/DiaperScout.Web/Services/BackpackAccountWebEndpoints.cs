using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using DiaperScout.Application;
using Microsoft.AspNetCore.Antiforgery;

namespace DiaperScout.Web.Services;

public static class BackpackAccountWebEndpoints
{
    public static void MapBackpackAccountWebEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/backpack/data").RequireAuthorization().RequireRateLimiting("passkeys")
            .AddEndpointFilter(async (context, next) =>
            {
                var http = context.HttpContext;
                http.Response.Headers.CacheControl = "no-store";
                if (http.Request.ContentLength > 40000) return Results.StatusCode(413);
                if (!HttpMethods.IsGet(http.Request.Method))
                {
                    try { await http.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(http); }
                    catch (AntiforgeryValidationException) { return Results.BadRequest(new { message = "Refresh the page and try again." }); }
                }
                try { return await next(context); }
                catch (HttpRequestException) { return Results.StatusCode(503); }
            });
        group.MapGet("/context", (HttpContext context) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            var owner = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            return Results.Ok(new { ownerTag = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(owner))) });
        });
        group.MapGet("/account", async (HttpContext context, IHttpClientFactory clients, CancellationToken ct) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            using var response = await clients.CreateClient("PasskeyApi").GetAsync("/api/v1/me/backpack/account", ct);
            return response.IsSuccessStatusCode ? Results.Ok(await response.Content.ReadFromJsonAsync<BackpackAccountInfo>(ct)) : Results.StatusCode((int)response.StatusCode);
        });
        group.MapGet("/discoveries", async (HttpContext context, IHttpClientFactory clients, CancellationToken ct) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            using var response = await clients.CreateClient("PasskeyApi").GetAsync("/api/v1/me/backpack/discoveries", ct);
            return response.IsSuccessStatusCode ? Results.Ok(await response.Content.ReadFromJsonAsync<PersonalDiscovery[]>(ct)) : Results.StatusCode((int)response.StatusCode);
        });
        group.MapGet("/drafts", async (HttpContext context, IHttpClientFactory clients, CancellationToken ct) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            using var response = await clients.CreateClient("PasskeyApi").GetAsync("/api/v1/me/backpack/drafts", ct);
            return response.IsSuccessStatusCode ? Results.Ok(await response.Content.ReadFromJsonAsync<PersonalCatalogueDrafts>(ct)) : Results.StatusCode((int)response.StatusCode);
        });
        group.MapPost("/name", async (UpdateExplorerName request, HttpContext context, IHttpClientFactory clients, CancellationToken ct) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            using var response = await clients.CreateClient("PasskeyApi").PostAsJsonAsync("/api/v1/me/backpack/name", request, ct);
            if (response.IsSuccessStatusCode) return Results.NoContent();
            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                return Results.Content(await response.Content.ReadAsStringAsync(ct), "application/problem+json", statusCode: 400);
            return Results.StatusCode((int)response.StatusCode);
        });
    }
}
