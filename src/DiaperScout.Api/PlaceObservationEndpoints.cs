using DiaperScout.Application;

namespace DiaperScout.Api;

public static class PlaceObservationEndpoints
{
    public static void MapPlaceObservationEndpoints(this WebApplication app)
    {
        var reads = app.MapGroup("/api/v1/places").WithTags("Places and Atlas");
        reads.MapGet("/countries", async (IPlaceObservations places, CancellationToken ct) => Results.Ok(await places.CountriesAsync(ct)));
        reads.MapGet("/", async (string? query, IPlaceObservations places, CancellationToken ct) =>
            await Validate(async () => Results.Ok(await places.SearchAsync(query, ct))));
        reads.MapGet("/atlas", async (IPlaceObservations places, CancellationToken ct) => Results.Ok(await places.AtlasAsync(ct)));
        reads.MapGet("/packs/{id:guid}", async (Guid id, IPlaceObservations places, CancellationToken ct) =>
            await places.PackAsync(id, ct) is { } pack ? Results.Ok(pack) : Results.NotFound());
        reads.MapPost("/", async (CreatePublicShopRequest request, ICurrentExplorer current, IPlaceObservations places, CancellationToken ct) =>
            await current.GetAsync(ct) is { } actor ? await Validate(async () => Results.Ok(await places.CreateShopAsync(actor, request, ct))) : Results.Forbid())
            .RequireAuthorization("Explorer").RequireRateLimiting("contributions");
        app.MapPost("/api/v1/physical-observations", async (CreatePhysicalObservationRequest request,
            ICurrentExplorer current, IPlaceObservations places, CancellationToken ct) =>
            await current.GetAsync(ct) is { } actor ? await Validate(async () => Results.Ok(await places.ObserveAsync(actor, request, ct))) : Results.Forbid())
            .RequireAuthorization("Explorer").RequireRateLimiting("contributions").WithTags("Observations");
        app.MapPost("/api/v1/public-product-proposals", async (PublicProductProposal request,
            ICurrentUser current, IPublicProductContributions proposals, CancellationToken ct) =>
            await current.GetAsync(ct) is { } actor ? await Validate(async () => Results.Ok(await proposals.SubmitAsync(actor, request, ct))) : Results.Forbid())
            .RequireAuthorization("Explorer").RequireRateLimiting("contributions").WithTags("Catalogue Submissions");
        app.MapPost("/api/v1/catalogue-submissions/{id:guid}/resolve-existing-pack", async (Guid id,
            ResolveBarcodeProposal request, ICurrentUser current, IPublicProductContributions proposals, CancellationToken ct) =>
        {
            var actor = await current.GetAsync(ct);
            if (actor is null) return Results.Forbid();
            try { return await Validate(async () => { await proposals.ResolveAsync(actor, id, request, ct); return Results.NoContent(); }); }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
            catch (KeyNotFoundException) { return Results.NotFound(); }
        }).RequireAuthorization("PublishAtlas").WithTags("Catalogue Submissions");
    }

    private static async Task<IResult> Validate(Func<Task<IResult>> action)
    {
        try { return await action(); }
        catch (CatalogueValidationException e) { return Results.ValidationProblem(new Dictionary<string, string[]> { [e.Field] = [e.Message] }); }
    }
}
