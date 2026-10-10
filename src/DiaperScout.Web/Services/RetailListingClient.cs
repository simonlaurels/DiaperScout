using System.Net;
using System.Net.Http.Json;
using DiaperScout.Application;
using Microsoft.AspNetCore.Mvc;

namespace DiaperScout.Web.Services;

public sealed partial class RetailerManagementClient
{
    public Task<RetailListingResponse<IReadOnlyList<RetailListingProductOption>>> GetListingCatalogueAsync() =>
        SendListingAsync<IReadOnlyList<RetailListingProductOption>>(HttpMethod.Get, "api/v1/retail-listings/catalogue");
    public Task<RetailListingResponse<IReadOnlyList<ManagedRetailerListing>>> GetListingsAsync() =>
        SendListingAsync<IReadOnlyList<ManagedRetailerListing>>(HttpMethod.Get, "api/v1/retail-listings/");
    public Task<RetailListingResponse<RetailerProductListingItem>> CreateListingAsync(ManualRetailerListingRequest request) =>
        SendListingAsync<RetailerProductListingItem>(HttpMethod.Post, "api/v1/retail-listings/", request);
    public Task<RetailListingResponse<RetailerProductListingItem>> UpdateListingAsync(Guid id, RetailerListingUpdate request) =>
        SendListingAsync<RetailerProductListingItem>(HttpMethod.Put, $"api/v1/retail-listings/{id}", request);
    public Task<RetailListingResponse<RetailerProductListingItem>> SetListingStatusAsync(Guid id, RetailerListingStatusUpdate request) =>
        SendListingAsync<RetailerProductListingItem>(HttpMethod.Put, $"api/v1/retail-listings/{id}/status", request);

    private async Task<RetailListingResponse<T>> SendListingAsync<T>(HttpMethod method, string path, object? body = null)
    {
        using var request = new HttpRequestMessage(method, path);
        if (body is not null) request.Content = JsonContent.Create(body);
        using var response = await client.SendAsync(request);
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            return new(default, "You need Moderator editorial authority to manage retail listings.", true);
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
            return new(default, problem?.Errors is null ? "Check the listing details." : string.Join(" ", problem.Errors.Values.SelectMany(x => x)));
        }
        if (response.StatusCode == HttpStatusCode.NotFound) return new(default, "This listing is no longer available. Refresh the page.");
        if (!response.IsSuccessStatusCode) return new(default, "The listing could not be loaded or saved. Please try again.");
        return new(await response.Content.ReadFromJsonAsync<T>());
    }
}

public sealed record RetailListingResponse<T>(T? Value, string? Error = null, bool AccessDenied = false);
