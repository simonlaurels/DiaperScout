using DiaperScout.Application;
using System.Net;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Json;
namespace DiaperScout.Web.Services;
public sealed class PlaceObservationClient(HttpClient client, AuthenticationStateProvider authentication)
{
    public Task<IReadOnlyList<PlaceCountry>> CountriesAsync() => ReadAsync<PlaceCountry>("api/v1/places/countries");
    public Task<IReadOnlyList<PlaceItem>> SearchAsync(string? query) => ReadAsync<PlaceItem>("api/v1/places/?query=" + Uri.EscapeDataString(query ?? ""));
    public Task<IReadOnlyList<AtlasPlace>> AtlasAsync() => ReadAsync<AtlasPlace>("api/v1/places/atlas");
    private async Task<IReadOnlyList<T>> ReadAsync<T>(string path) => await client.GetFromJsonAsync<T[]>(path) ?? [];
    public async Task<ProductIdentification?> PackAsync(Guid id)
    {
        using var response = await client.GetAsync("api/v1/places/packs/" + id);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProductIdentification>();
    }
    public Task<PlaceItem> CreateShopAsync(CreatePublicShopRequest request) => PostAsync<PlaceItem>("api/v1/places/", request);
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
