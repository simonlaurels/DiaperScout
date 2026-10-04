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
        reads.MapPost("/nearby", async (NearbyPlaceRequest request, HttpResponse response, IPlaceObservations places, CancellationToken ct) => {
            response.Headers.CacheControl = "no-store"; return await Validate(async () => Results.Ok(await places.NearbyAsync(request, ct)));
        }).RequireRateLimiting("contributions");
        reads.MapGet("/packs/{id:guid}", async (Guid id, IPlaceObservations places, CancellationToken ct) =>
            await places.PackAsync(id, ct) is { } pack ? Results.Ok(pack) : Results.NotFound());
        reads.MapPost("/", async (CreatePublicShopRequest request, ICurrentExplorer current, IPlaceObservations places, CancellationToken ct) =>
            await current.GetAsync(ct) is { } actor ? await Validate(async () => Results.Ok(await places.CreateShopAsync(actor, request, ct))) : Results.Forbid())
            .RequireAuthorization("Explorer").RequireRateLimiting("contributions");
        reads.MapPost("/{id:guid}/category", async (Guid id, UpdatePlaceCategoryRequest request, ICurrentUser current, IPlaceObservations places, CancellationToken ct) =>
        {
            var actor = await current.GetAsync(ct); if (actor is null) return Results.Forbid();
            try { return await Validate(async () => Results.Ok(await places.UpdateCategoryAsync(actor, id, request, ct))); }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
            catch (KeyNotFoundException) { return Results.NotFound(); }
        }).RequireAuthorization("PublishAtlas");
        app.MapPost("/api/v1/physical-observations", async (CreatePhysicalObservationRequest request,
            ICurrentExplorer current, IPlaceObservations places, CancellationToken ct) =>
            await current.GetAsync(ct) is { } actor ? await Validate(async () => Results.Ok(await places.ObserveAsync(actor, request, ct))) : Results.Forbid())
            .RequireAuthorization("Explorer").RequireRateLimiting("contributions").WithTags("Observations");
        app.MapPost("/api/v1/public-product-proposals", async (PublicProductProposal request,
            ICurrentUser current, IPublicProductContributions proposals, CancellationToken ct) =>
            await current.GetAsync(ct) is { } actor ? await Validate(async () => Results.Ok(await proposals.SubmitAsync(actor, request, ct))) : Results.Forbid())
            .RequireAuthorization("Explorer").RequireRateLimiting("contributions").WithTags("Catalogue Submissions");
        app.MapPost("/api/v1/public-product-proposals/draft", async (BeginPublicProductProposal request, ICurrentUser current, IPublicProductContributions proposals, CancellationToken ct) =>
            await current.GetAsync(ct) is { } actor ? await Validate(async () => Results.Ok(await proposals.BeginAsync(actor, request.Gtin, request.ContributionId, ct))) : Results.Forbid())
            .RequireAuthorization("Explorer").RequireRateLimiting("contributions");
        app.MapGet("/api/v1/public-product-proposals/{id:guid}", async (Guid id, HttpResponse response, ICurrentUser current, IPublicProductContributions proposals, CancellationToken ct) => {
            response.Headers.CacheControl = "no-store"; var actor = await current.GetAsync(ct);
            if (actor is null) return Results.Forbid();
            return await proposals.IdentityAsync(actor, id, ct) is { } identity ? Results.Ok(identity) : Results.NotFound();
        }).RequireAuthorization("Explorer");
        app.MapPost("/api/v1/public-product-proposals/{id:guid}/discovery", async (Guid id, PendingPhysicalDiscovery request, ICurrentUser current, IPublicProductContributions proposals, CancellationToken ct) => {
            var actor = await current.GetAsync(ct); if (actor is null) return Results.Forbid();
            try { return await Validate(async () => { await proposals.AttachDiscoveryAsync(actor, id, request, ct); return Results.NoContent(); }); }
            catch (KeyNotFoundException) { return Results.NotFound(); }
        }).RequireAuthorization("Explorer").RequireRateLimiting("contributions");
        app.MapPost("/api/v1/public-product-proposals/{id:guid}/images", async (Guid id, HttpRequest request, ICurrentUser current, IPublicProductContributions proposals, CancellationToken ct) => {
            var actor = await current.GetAsync(ct); if (actor is null) return Results.Forbid();
            if (!request.HasFormContentType) return Results.BadRequest();
            var form = await request.ReadFormAsync(ct); var file = form.Files.GetFile("file");
            if (file is null || !Enum.TryParse<DiaperScout.Domain.CatalogueSubmissionImageRole>(form["role"], out var role) || !Guid.TryParse(form["uploadId"], out var uploadId)) return Results.BadRequest();
            try { return await Validate(async () => { await using var content = file.OpenReadStream(); return Results.Ok(await proposals.AddEvidenceAsync(actor, id, uploadId,
                new(role, Path.GetFileName(file.FileName), file.ContentType, file.Length, content, DiaperScout.Domain.CatalogueImageSourceType.UserCommunity, null, null, DiaperScout.Domain.CatalogueImagePermissionStatus.Unknown, null), ct)); }); }
            catch (KeyNotFoundException) { return Results.NotFound(); }
        }).RequireAuthorization("Explorer").RequireRateLimiting("contributions");
        app.MapGet("/api/v1/public-product-proposals/{id:guid}/images", async (Guid id, HttpResponse response, ICurrentUser current, IPublicProductContributions proposals, CancellationToken ct) => {
            response.Headers.CacheControl = "no-store"; var actor = await current.GetAsync(ct);
            return actor is null ? Results.Forbid() : Results.Ok(await proposals.ImagesAsync(actor, id, ct));
        }).RequireAuthorization("Explorer");
        app.MapGet("/api/v1/public-product-proposals/{id:guid}/images/{imageId:guid}", async (Guid id, Guid imageId, HttpResponse response, ICurrentUser current, IPublicProductContributions proposals, CancellationToken ct) => {
            response.Headers.CacheControl = "no-store";
            var actor = await current.GetAsync(ct); if (actor is null) return Results.Forbid();
            return await proposals.EvidenceAsync(actor, id, imageId, ct) is { } image ? Results.File(image.Content, image.ContentType) : Results.NotFound();
        }).RequireAuthorization("Explorer");
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
public sealed record BeginPublicProductProposal(string Gtin, Guid ContributionId);
