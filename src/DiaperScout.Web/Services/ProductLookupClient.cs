using System.Net;
using System.Net.Http.Json;
using DiaperScout.Application;

namespace DiaperScout.Web.Services;

public sealed class ProductLookupClient(HttpClient client)
{
    public async Task<ProductLookupResult> LookupAsync(string gtin, CancellationToken cancellationToken = default)
    {
        var normalizedGtin = gtin.Replace(" ", string.Empty).Replace("-", string.Empty);
        if (string.IsNullOrWhiteSpace(normalizedGtin))
        {
            return ProductLookupResult.Invalid("Enter a barcode number to search the Atlas.");
        }

        using var response = await client.GetAsync($"api/v1/products/lookup/{Uri.EscapeDataString(normalizedGtin)}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return ProductLookupResult.NotFound(normalizedGtin);
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            return ProductLookupResult.Invalid("Enter a GTIN containing 8 to 14 digits.");
        }

        if (!response.IsSuccessStatusCode)
        {
            return ProductLookupResult.Failed();
        }

        var product = await response.Content.ReadFromJsonAsync<ProductIdentification>(cancellationToken);
        return product is null ? ProductLookupResult.Failed() : ProductLookupResult.Found(product);
    }
}

public sealed record ProductLookupResult(ProductLookupStatus Status, string? Gtin = null, ProductIdentification? Product = null, string? Message = null)
{
    public static ProductLookupResult Found(ProductIdentification product) => new(ProductLookupStatus.Found, product.Gtin, product);
    public static ProductLookupResult NotFound(string gtin) => new(ProductLookupStatus.NotFound, gtin);
    public static ProductLookupResult Invalid(string message) => new(ProductLookupStatus.Invalid, Message: message);
    public static ProductLookupResult Failed() => new(ProductLookupStatus.Failed, Message: "We couldn't search the Atlas just now. Please try again.");
}

public enum ProductLookupStatus { Found, NotFound, Invalid, Failed }
