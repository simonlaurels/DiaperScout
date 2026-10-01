using DiaperScout.Application;
using System.Net;
using System.Net.Http.Json;
namespace DiaperScout.Web.Services;
public sealed class PlaceObservationClient(HttpClient client)
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
    public Task<PhysicalObservationReceipt> ObserveAsync(CreatePhysicalObservationRequest request) => PostAsync<PhysicalObservationReceipt>("api/v1/physical-observations", request);
    public Task<PublicProductProposalReceipt> ProposeAsync(PublicProductProposal request) => PostAsync<PublicProductProposalReceipt>("api/v1/public-product-proposals", request);
    public async Task ResolveAsync(Guid id, ResolveBarcodeProposal request)
    {
        using var response = await client.PostAsJsonAsync($"api/v1/catalogue-submissions/{id}/resolve-existing-pack", request);
        await CheckAsync(response);
    }
    private async Task<T> PostAsync<T>(string path, object request)
    {
        using var response = await client.PostAsJsonAsync(path, request); await CheckAsync(response);
        return await response.Content.ReadFromJsonAsync<T>() ?? throw new HttpRequestException("The server did not confirm the save. Your entered information is still here.");
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
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            throw new ContributionException("Sign in again to save this contribution. Your entered information is still here.");
        throw new ContributionException("We couldn’t save just now. Your information is still here; check your connection and try again.");
    }
    private sealed record ValidationProblem(Dictionary<string, string[]> Errors);
}
public sealed class ContributionException(string message) : Exception(message);
