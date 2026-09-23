using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using DiaperScout.Application;
using DiaperScout.Domain;
using Microsoft.Extensions.Options;

namespace DiaperScout.Infrastructure;

public sealed class DataForSeoGoogleShoppingMonitor(
    HttpClient httpClient,
    IOptions<DataForSeoOptions> options)
    : IRetailerProductMonitor
{
    private const string DiscoveryProvider = "DataForSEO.GoogleShopping";
    private const string Source = "DataForSEO.GoogleShopping.ProductInfo";
    private readonly DataForSeoOptions options = options.Value;

    public bool CanMonitor(RetailerProductListing listing) =>
        options.Enabled &&
        string.Equals(listing.DiscoveryProvider, DiscoveryProvider, StringComparison.OrdinalIgnoreCase) &&
        !string.IsNullOrWhiteSpace(listing.ExternalListingId);

    public async Task<RetailerProductObservationReading> ObserveAsync(
        RetailerProductListing listing,
        CancellationToken cancellationToken = default)
    {
        if (!CanMonitor(listing))
            throw new InvalidOperationException("DataForSEO cannot monitor this retailer product listing.");

        ConfigureClient();

        var taskId = await CreateProductInfoTaskAsync(
            listing.ExternalListingId!,
            cancellationToken);

        using var document = await WaitForTaskAsync(
            $"v3/merchant/google/product_info/task_get/advanced/{taskId}",
            cancellationToken);

        return ExtractObservation(listing, document);
    }

    private void ConfigureClient()
    {
        if (string.IsNullOrWhiteSpace(options.Login) || string.IsNullOrWhiteSpace(options.Password))
            throw new InvalidOperationException(
                "DataForSEO is enabled but DataForSEO:Login and DataForSEO:Password are not configured.");

        httpClient.BaseAddress ??= new Uri("https://api.dataforseo.com/");
        var credentials = Convert.ToBase64String(
            Encoding.ASCII.GetBytes($"{options.Login}:{options.Password}"));
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", credentials);
    }

    private async Task<string> CreateProductInfoTaskAsync(
        string productId,
        CancellationToken cancellationToken)
    {
        var payload = new[]
        {
            new
            {
                location_name = options.LocationName,
                language_code = options.LanguageCode,
                se_domain = options.SearchDomain,
                product_id = productId,
                priority = 1
            }
        };

        using var response = await httpClient.PostAsJsonAsync(
            "v3/merchant/google/product_info/task_post",
            payload,
            cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);

        using var document = await response.Content.ReadFromJsonAsync<JsonDocument>(
            cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException(
                "DataForSEO returned an empty task response.");

        return ReadTaskId(document);
    }

    private async Task<JsonDocument> WaitForTaskAsync(
        string path,
        CancellationToken cancellationToken)
    {
        var attempts = Math.Max(1, options.PollAttempts);
        var delay = TimeSpan.FromSeconds(Math.Max(1, options.PollDelaySeconds));

        for (var attempt = 0; attempt < attempts; attempt++)
        {
            using var response = await httpClient.GetAsync(path, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);

            var document = await response.Content.ReadFromJsonAsync<JsonDocument>(
                cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException(
                    "DataForSEO returned an empty task response.");

            if (HasCompletedResult(document))
                return document;

            document.Dispose();

            if (attempt + 1 < attempts)
                await Task.Delay(delay, cancellationToken);
        }

        throw new TimeoutException(
            "DataForSEO did not return a completed Google Shopping product-info task within the configured polling window.");
    }

    private static RetailerProductObservationReading ExtractObservation(
        RetailerProductListing listing,
        JsonDocument document)
    {
        if (!document.RootElement.TryGetProperty("tasks", out var tasks) ||
            tasks.ValueKind != JsonValueKind.Array ||
            tasks.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("DataForSEO returned no product-info task.");
        }

        var task = tasks[0];
        if (!task.TryGetProperty("result", out var results) ||
            results.ValueKind != JsonValueKind.Array ||
            results.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("DataForSEO returned no product-info result.");
        }

        var result = results[0];
        var observedAt = ParseObservedAt(result);
        var checkUrl = result.TryGetProperty("check_url", out var checkUrlElement)
            ? checkUrlElement.GetString()
            : null;

        if (!result.TryGetProperty("items", out var items) ||
            items.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException(
                $"DataForSEO returned no product sellers for listing {listing.Id}.");
        }

        foreach (var item in items.EnumerateArray())
        {
            if (!item.TryGetProperty("type", out var type) ||
                !string.Equals(type.GetString(), "product_info_element", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!item.TryGetProperty("sellers", out var sellers) ||
                sellers.ValueKind != JsonValueKind.Array)
                continue;

            foreach (var seller in sellers.EnumerateArray())
            {
                var sellerUrl = seller.TryGetProperty("url", out var urlElement)
                    ? urlElement.GetString()
                    : null;

                if (!UrlsMatch(sellerUrl, listing.ListingUrl))
                    continue;

                var price = ReadCurrentPrice(seller);
                var currency = ReadString(seller, "price", "currency");
                var availability = MapAvailability(
                    ReadString(seller, "product_availability"));

                return new RetailerProductObservationReading(
                    observedAt,
                    price,
                    currency,
                    availability,
                    Source,
                    checkUrl);
            }
        }

        throw new InvalidOperationException(
            $"DataForSEO did not return a seller matching listing URL '{listing.ListingUrl}'.");
    }

    private static decimal? ReadCurrentPrice(JsonElement seller)
    {
        if (!seller.TryGetProperty("price", out var price) ||
            price.ValueKind != JsonValueKind.Object ||
            !price.TryGetProperty("current", out var current) ||
            current.ValueKind != JsonValueKind.Number)
            return null;

        return current.TryGetDecimal(out var value) ? value : null;
    }

    private static string? ReadString(JsonElement seller, string propertyName) =>
        seller.TryGetProperty(propertyName, out var value) &&
        value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static string? ReadString(
        JsonElement seller,
        string parentPropertyName,
        string propertyName)
    {
        if (!seller.TryGetProperty(parentPropertyName, out var parent) ||
            parent.ValueKind != JsonValueKind.Object)
            return null;

        return parent.TryGetProperty(propertyName, out var value) &&
               value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static RetailerProductAvailability MapAvailability(string? value) =>
        value?.Trim().ToLowerInvariant() switch
        {
            "in_stock" => RetailerProductAvailability.InStock,
            "limited_stock" => RetailerProductAvailability.InStock,
            "out_of_stock" => RetailerProductAvailability.OutOfStock,
            "pre_order_available" => RetailerProductAvailability.PreOrder,
            _ => RetailerProductAvailability.Unknown
        };

    private static bool UrlsMatch(string? left, string right)
    {
        if (string.IsNullOrWhiteSpace(left))
            return false;

        if (!Uri.TryCreate(left.Trim(), UriKind.Absolute, out var leftUri) ||
            !Uri.TryCreate(right.Trim(), UriKind.Absolute, out var rightUri))
            return false;

        return string.Equals(
                   leftUri.GetLeftPart(UriPartial.Path).TrimEnd('/'),
                   rightUri.GetLeftPart(UriPartial.Path).TrimEnd('/'),
                   StringComparison.OrdinalIgnoreCase);
    }

    private static DateTimeOffset ParseObservedAt(JsonElement result)
    {
        if (result.TryGetProperty("datetime", out var datetime) &&
            datetime.ValueKind == JsonValueKind.String &&
            DateTimeOffset.TryParse(datetime.GetString(), out var parsed))
            return parsed;

        return DateTimeOffset.UtcNow;
    }

    private static bool HasCompletedResult(JsonDocument document)
    {
        if (!document.RootElement.TryGetProperty("tasks", out var tasks) ||
            tasks.ValueKind != JsonValueKind.Array ||
            tasks.GetArrayLength() == 0)
            return false;

        var task = tasks[0];

        if (!task.TryGetProperty("status_code", out var statusCode) ||
            statusCode.ValueKind != JsonValueKind.Number)
            return false;

        var code = statusCode.GetInt32();
        if (code >= 40000)
            throw new InvalidOperationException(ReadStatusMessage(task));

        return task.TryGetProperty("result", out var result) &&
               result.ValueKind == JsonValueKind.Array &&
               result.GetArrayLength() > 0;
    }

    private static string ReadTaskId(JsonDocument document)
    {
        var task = document.RootElement.GetProperty("tasks")[0];
        var statusCode = task.GetProperty("status_code").GetInt32();

        if (statusCode >= 40000)
            throw new InvalidOperationException(ReadStatusMessage(task));

        return task.GetProperty("id").GetString()
            ?? throw new InvalidOperationException("DataForSEO did not return a task id.");
    }

    private static string ReadStatusMessage(JsonElement task) =>
        task.TryGetProperty("status_message", out var message)
            ? message.GetString() ?? "DataForSEO returned an unknown error."
            : "DataForSEO returned an unknown error.";

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new HttpRequestException(
            $"DataForSEO returned HTTP {(int)response.StatusCode}: {body}");
    }
}
