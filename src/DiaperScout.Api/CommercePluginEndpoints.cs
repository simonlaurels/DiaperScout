using DiaperScout.Application;

namespace DiaperScout.Api;

internal static class CommercePluginEndpoints
{
    public static void MapCommercePluginEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/commerce-plugins").RequireAuthorization("PublishAtlas").WithTags("Commerce Integrations");
        group.MapGet("/", (ICurrentUser user, ICommercePluginManagement plugins, CancellationToken ct) =>
            RunAsync(user, actor => plugins.GetAsync(actor, ct), ct));
        group.MapPut("/{id}/enabled", (string id, CommercePluginEnabledRequest request, ICurrentUser user,
            ICommercePluginManagement plugins, CancellationToken ct) => RunAsync(user, actor => plugins.SetEnabledAsync(actor, id, request.Enabled, ct), ct));
    }
    private static async Task<IResult> RunAsync<T>(ICurrentUser user, Func<AuthenticatedUser, Task<T>> action, CancellationToken ct)
    {
        var actor = await user.GetAsync(ct);
        if (actor is null) return Results.Forbid();
        try { return Results.Ok(await action(actor)); }
        catch (UnauthorizedAccessException) { return Results.Forbid(); }
        catch (KeyNotFoundException) { return Results.NotFound(); }
        catch (CatalogueValidationException exception) { return Results.ValidationProblem(new Dictionary<string, string[]> { [exception.Field] = [exception.Message] }); }
    }
}
