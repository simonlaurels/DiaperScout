using DiaperScout.Application;
using DiaperScout.Domain;
using System.Net;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Forms;
namespace DiaperScout.Web.Services;
public sealed class PlaceObservationClient(HttpClient client, AuthenticationStateProvider authentication)
{
    public Task<IReadOnlyList<PlaceCountry>> CountriesAsync() => ReadAsync<PlaceCountry>("api/v1/places/countries");
    public Task<IReadOnlyList<PlaceItem>> SearchAsync(string? query) => ReadAsync<PlaceItem>("api/v1/places/?query=" + Uri.EscapeDataString(query ?? ""));
    public async Task<IReadOnlyList<NearbyPlace>> NearbyAsync(NearbyPlaceRequest request) {
        using var message = await CreatePostAsync("api/v1/places/nearby", request);
        using var response = await client.SendAsync(message); await CheckAsync(response);
        return await response.Content.ReadFromJsonAsync<NearbyPlace[]>() ?? [];
    }
    public Task<PublicProductProposalReceipt> BeginProposalAsync(string gtin, Guid contributionId) => PostAsync<PublicProductProposalReceipt>("api/v1/public-product-proposals/draft", new {gtin, contributionId});
    public async Task AttachDiscoveryAsync(Guid id, PendingPhysicalDiscovery request) {
        using var message = await CreatePostAsync($"api/v1/public-product-proposals/{id}/discovery", request);
        using var response = await client.SendAsync(message); await CheckAsync(response);
    }
    private async Task<HttpResponseMessage> PrivateGetAsync(string path) {
        using var message = new HttpRequestMessage(HttpMethod.Get, path);
        message.Options.Set(ProductionIdentityForwardingHandler.ContributionUser, (await authentication.GetAuthenticationStateAsync()).User);
        var response = await client.SendAsync(message);
        try { await CheckAsync(response); return response; }
        catch { response.Dispose(); throw; }
    }
    public async Task<PublicProposalIdentity?> ProposalIdentityAsync(Guid id) {
        using var response = await PrivateGetAsync($"api/v1/public-product-proposals/{id}"); return await response.Content.ReadFromJsonAsync<PublicProposalIdentity>();
    }
    public async Task<IReadOnlyList<CatalogueSubmissionImageReceipt>> ProposalImagesAsync(Guid id) {
        using var response = await PrivateGetAsync($"api/v1/public-product-proposals/{id}/images"); return await response.Content.ReadFromJsonAsync<CatalogueSubmissionImageReceipt[]>() ?? [];
    }
    public async Task<CatalogueSubmissionImageReceipt> UploadProposalImageAsync(Guid id, Guid uploadId, CatalogueSubmissionImageRole role, IBrowserFile file) {
        using var message = new HttpRequestMessage(HttpMethod.Post, $"api/v1/public-product-proposals/{id}/images");
        message.Options.Set(ProductionIdentityForwardingHandler.ContributionUser, (await authentication.GetAuthenticationStateAsync()).User);
        using var form = new MultipartFormDataContent(); await using var stream = file.OpenReadStream(15 * 1024 * 1024);
        var content = new StreamContent(stream); content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
        form.Add(content, "file", Path.GetFileName(file.Name)); form.Add(new StringContent(role.ToString()), "role"); form.Add(new StringContent(uploadId.ToString()), "uploadId"); message.Content = form;
        using var response = await client.SendAsync(message); await CheckAsync(response);
        return await response.Content.ReadFromJsonAsync<CatalogueSubmissionImageReceipt>() ?? throw new ContributionException("The photograph could not be confirmed. Retry before continuing.");
    }
    public Task<IReadOnlyList<AtlasPlace>> AtlasAsync() => ReadAsync<AtlasPlace>("api/v1/places/atlas");
    private async Task<IReadOnlyList<T>> ReadAsync<T>(string path) => await client.GetFromJsonAsync<T[]>(path) ?? [];
    public async Task<ProductIdentification?> PackAsync(Guid id)
    {
        using var response = await client.GetAsync("api/v1/places/packs/" + id);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProductIdentification>();
    }
    public Task<PlaceItem> SelectProviderPlaceAsync(string token) => PostAsync<PlaceItem>("api/v1/places/select", new SelectProviderPlaceRequest(token));
    public Task<PlaceItem> UpdateCategoryAsync(Guid id, UpdatePlaceCategoryRequest request) => PostAsync<PlaceItem>($"api/v1/places/{id}/category", request);
    public Task<PhysicalObservationReceipt> ObserveAsync(CreatePhysicalObservationRequest request) => PostAsync<PhysicalObservationReceipt>("api/v1/physical-observations", request);
    public Task<PublicProductProposalReceipt> ProposeAsync(PublicProductProposal request) => PostAsync<PublicProductProposalReceipt>("api/v1/public-product-proposals", request);
    public async Task ResolveAsync(Guid id, ResolveBarcodeProposal request)
    {
        using var message = await CreatePostAsync($"api/v1/catalogue-submissions/{id}/resolve-existing-pack", request);
        using var response = await client.SendAsync(message);
        await CheckAsync(response);
    }
    private async Task<T> PostAsync<T>(string path, object request)
    {
        using var message = await CreatePostAsync(path, request);
        using var response = await client.SendAsync(message); await CheckAsync(response);
        return await response.Content.ReadFromJsonAsync<T>() ?? throw new HttpRequestException("The server did not confirm the save. Your entered information is still here.");
    }
    private async Task<HttpRequestMessage> CreatePostAsync(string path, object value)
    {
        var message = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(value, value.GetType()) };
        message.Options.Set(ProductionIdentityForwardingHandler.ContributionUser,
            (await authentication.GetAuthenticationStateAsync()).User);
        return message;
    }
    private static async Task CheckAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;
        if (response.StatusCode == HttpStatusCode.TooManyRequests) throw new ContributionException("Too many contributions were sent just now. Wait a minute before trying again; your entered information is still here.");
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var problem = await response.Content.ReadFromJsonAsync<ValidationProblem>();
            throw new ContributionException(string.Join(" ", problem?.Errors.Values.SelectMany(e => e) ?? ["Check the entered information."]));
        }
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new ContributionException("Sign in to save this contribution. Your entered information is still here.", requiresAuthentication: true);
        if (response.StatusCode == HttpStatusCode.Forbidden)
            throw new ContributionException("Your account cannot send this contribution. Your entered information is still here; signing in again will not change this permission.");
        throw new ContributionException("We couldn’t save just now. Your information is still here; check your connection and try again.");
    }
    private sealed record ValidationProblem(Dictionary<string, string[]> Errors);
}
public sealed class ContributionException(string message, bool requiresAuthentication = false) : Exception(message)
{ public bool RequiresAuthentication { get; } = requiresAuthentication; }
