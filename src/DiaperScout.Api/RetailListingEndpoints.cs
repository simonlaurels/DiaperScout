using DiaperScout.Application;

namespace DiaperScout.Api;

internal static class RetailListingEndpoints
{
    public static void MapRetailListingEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/retail-listings").RequireAuthorization("PublishAtlas").WithTags("Retail Listings");
        group.MapGet("/catalogue", (ICurrentUser user, IRetailerDiscovery listings, CancellationToken ct) =>
            RunAsync(user, actor => listings.GetListingCatalogueAsync(actor, ct), ct));
        group.MapGet("/", (Guid? retailerId, ICurrentUser user, IRetailerDiscovery listings, CancellationToken ct) =>
            RunAsync(user, actor => listings.GetListingsAsync(actor, retailerId, ct), ct));
        group.MapPost("/", (ManualRetailerListingRequest request, ICurrentUser user, IRetailerDiscovery listings, CancellationToken ct) =>
            RunAsync(user, actor => listings.CreateManualListingAsync(actor, request, ct), ct));
        group.MapPut("/{id:guid}", (Guid id, RetailerListingUpdate request, ICurrentUser user, IRetailerDiscovery listings, CancellationToken ct) =>
            RunAsync(user, actor => listings.UpdateListingAsync(actor, id, request, ct), ct));
        group.MapPut("/{id:guid}/status", (Guid id, RetailerListingStatusUpdate request, ICurrentUser user, IRetailerDiscovery listings, CancellationToken ct) =>
            RunAsync(user, actor => listings.SetListingStatusAsync(actor, id, request.Status, ct), ct));
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
