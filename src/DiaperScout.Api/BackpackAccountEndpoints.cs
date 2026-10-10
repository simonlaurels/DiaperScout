using DiaperScout.Application;

namespace DiaperScout.Api;

public static class BackpackAccountEndpoints
{
    public static void MapBackpackAccountEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/me/backpack").RequireAuthorization("Explorer").WithTags("Backpack");
        group.MapGet("/account", async (HttpContext context, ICurrentUser current, IBackpackAccount accounts, CancellationToken ct) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            return await current.GetAsync(ct) is { } actor ? Results.Ok(await accounts.AccountAsync(actor, ct)) : Results.Forbid();
        });
        group.MapGet("/discoveries", async (HttpContext context, ICurrentUser current, IBackpackAccount accounts, CancellationToken ct) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            return await current.GetAsync(ct) is { } actor ? Results.Ok(await accounts.DiscoveriesAsync(actor, ct)) : Results.Forbid();
        });
        group.MapGet("/drafts", async (HttpContext context, ICurrentUser current, IBackpackAccount accounts, CancellationToken ct) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            return await current.GetAsync(ct) is { } actor ? Results.Ok(await accounts.DraftsAsync(actor, ct)) : Results.Forbid();
        });
        group.MapPost("/name", async (UpdateExplorerName request, ICurrentUser current, IBackpackAccount accounts, CancellationToken ct) =>
        {
            if (await current.GetAsync(ct) is not { } actor) return Results.Forbid();
            try { await accounts.UpdateNameAsync(actor, request, ct); return Results.NoContent(); }
            catch (CatalogueValidationException e) { return Results.ValidationProblem(new Dictionary<string,string[]> { [e.Field] = [e.Message] }); }
        }).RequireRateLimiting("contributions");
    }
}
