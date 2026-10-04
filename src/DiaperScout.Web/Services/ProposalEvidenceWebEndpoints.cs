namespace DiaperScout.Web.Services;
public static class ProposalEvidenceWebEndpoints
{
    public static void MapProposalEvidenceWebEndpoints(this WebApplication app)
    {
        app.MapGet("/contribute/product/images/{submissionId:guid}/{imageId:guid}", async (Guid submissionId, Guid imageId, HttpContext context, IHttpClientFactory clients, CancellationToken ct) => {
            context.Response.Headers.CacheControl = "no-store";
            try {
                using var request = new HttpRequestMessage(HttpMethod.Get, $"api/v1/public-product-proposals/{submissionId}/images/{imageId}");
                request.Options.Set(ProductionIdentityForwardingHandler.ContributionUser, context.User);
                using var response = await clients.CreateClient("PasskeyApi").SendAsync(request, ct);
                if (!response.IsSuccessStatusCode) return Results.StatusCode((int)response.StatusCode);
                return Results.File(await response.Content.ReadAsByteArrayAsync(ct), response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream");
            } catch (HttpRequestException) { return Results.StatusCode(503); }
        }).RequireAuthorization().RequireRateLimiting("passkeys");
    }
}
