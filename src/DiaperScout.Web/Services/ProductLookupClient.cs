using System.Net;
using System.Net.Http.Json;
using DiaperScout.Application;
using DiaperScout.Domain;

namespace DiaperScout.Web.Services;

public sealed class ProductLookupClient(HttpClient client)
{
    public async Task<ProductLookupResult> LookupAsync(string gtin, CancellationToken cancellationToken = default)
    {
        var normalizedGtin = gtin.Replace(" ", string.Empty).Replace("-", string.Empty);
        if (RetailGtin.Normalise(normalizedGtin) is null)
        {
            return ProductLookupResult.Invalid("Enter a valid 8, 12, 13 or 14-digit barcode, including its check digit.");
        }

        try
        {
        using var response = await client.GetAsync($"api/v1/products/lookup/{Uri.EscapeDataString(normalizedGtin)}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return ProductLookupResult.NotFound(normalizedGtin);
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            return ProductLookupResult.Invalid("Enter a valid 8, 12, 13 or 14-digit barcode, including its check digit.");
        }
        if (response.StatusCode == HttpStatusCode.Conflict)
            return new(ProductLookupStatus.Ambiguous, normalizedGtin, Message: "This barcode has conflicting catalogue matches. We cannot safely identify it; please try another item. The catalogue needs review.");

        if (!response.IsSuccessStatusCode)
        {
            return ProductLookupResult.Failed();
        }

        var product = await response.Content.ReadFromJsonAsync<ProductIdentification>(cancellationToken);
        return product is null ? ProductLookupResult.Failed() : ProductLookupResult.Found(product);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException) { return ProductLookupResult.Failed(); }
    }
}

public sealed record ProductLookupResult(ProductLookupStatus Status, string? Gtin = null, ProductIdentification? Product = null, string? Message = null)
{
    public static ProductLookupResult Found(ProductIdentification product) => new(ProductLookupStatus.Found, product.Gtin, product);
    public static ProductLookupResult NotFound(string gtin) => new(ProductLookupStatus.NotFound, gtin);
    public static ProductLookupResult Invalid(string message) => new(ProductLookupStatus.Invalid, Message: message);
    public static ProductLookupResult Failed() => new(ProductLookupStatus.Failed, Message: "We couldn't search the Atlas just now. Please try again.");
}

public enum ProductLookupStatus { Found, NotFound, Invalid, Failed, Ambiguous }
