using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace DiaperScout.Infrastructure;

public sealed class DataForSeoOptions
{
    public const string SectionName = "DataForSEO";

    public bool Enabled { get; set; }
    public string? Login { get; set; }
    public string? Password { get; set; }
    public string LocationName { get; set; } = "United Kingdom";
    public string LanguageCode { get; set; } = "en";
    public string SearchDomain { get; set; } = "google.co.uk";
    public int SearchDepth { get; set; } = 10;
    public int MaxProductsPerGtin { get; set; } = 3;
    public int PollAttempts { get; set; } = 36;
    public int PollDelaySeconds { get; set; } = 10;
}

public sealed class DataForSeoGoogleShoppingProvider : DiaperScout.Application.IRetailerDiscoveryProvider
{
    private readonly HttpClient httpClient;
    private readonly DataForSeoOptions fallbackOptions;
    private readonly DataForSeoRuntimeSettingsStore? runtime;

    public DataForSeoGoogleShoppingProvider(
        HttpClient httpClient,
        IOptions<DataForSeoOptions> options,
        DataForSeoRuntimeSettingsStore? runtime = null)
    {
        this.httpClient = httpClient;
        fallbackOptions = options.Value;
        this.runtime = runtime;
    }

    private DataForSeoOptions Options
    {
        get
        {
            var current = runtime?.Current;
            if (current is null)
                return fallbackOptions;

            return new DataForSeoOptions
            {
                Enabled = current.Enabled,
                Login = current.Login,
                Password = current.Password,
                LocationName = current.LocationName,
                LanguageCode = current.LanguageCode,
                SearchDomain = current.SearchDomain,
                SearchDepth = fallbackOptions.SearchDepth,
                MaxProductsPerGtin = fallbackOptions.MaxProductsPerGtin,
                PollAttempts = fallbackOptions.PollAttempts,
                PollDelaySeconds = fallbackOptions.PollDelaySeconds
            };
        }
    }

    public async Task<IReadOnlyList<DiaperScout.Application.RetailerDiscoveryCandidate>> DiscoverAsync(
        string gtin,
        CancellationToken cancellationToken = default)
    {
        if (!Options.Enabled)
            return [];

        var normalizedGtin = gtin.Trim();
        if (normalizedGtin.Length == 0)
            return [];

        ConfigureClient();

        var taskId = await CreateProductSearchTaskAsync(normalizedGtin, cancellationToken);
        using var searchDocument = await WaitForTaskAsync(
            $"v3/merchant/google/products/task_get/advanced/{taskId}",
            cancellationToken);

        var productIds = ExtractProductIds(searchDocument)
            .Take(Math.Max(1, Options.MaxProductsPerGtin))
            .ToArray();

        if (productIds.Length == 0)
            return [];

        var candidates = new Dictionary<string, DiaperScout.Application.RetailerDiscoveryCandidate>(StringComparer.OrdinalIgnoreCase);

        foreach (var productId in productIds)
        {
            var productInfoTaskId = await CreateProductInfoTaskAsync(productId, cancellationToken);
            using var productInfoDocument = await WaitForTaskAsync(
                $"v3/merchant/google/product_info/task_get/advanced/{productInfoTaskId}",
                cancellationToken);

            foreach (var candidate in ExtractCandidates(normalizedGtin, productId, productInfoDocument))
                candidates.TryAdd(NormalizeUrl(candidate.ListingUrl), candidate);
        }

        return candidates.Values.ToArray();
    }

    private void ConfigureClient()
    {
        if (string.IsNullOrWhiteSpace(Options.Login) || string.IsNullOrWhiteSpace(Options.Password))
            throw new InvalidOperationException("DataForSEO is enabled but DataForSEO:Login and DataForSEO:Password are not configured.");

        httpClient.BaseAddress ??= new Uri("https://api.dataforseo.com/");
        var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{Options.Login}:{Options.Password}"));
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
    }

    private async Task<string> CreateProductSearchTaskAsync(string gtin, CancellationToken cancellationToken)
    {
        var payload = new[]
        {
            new
            {
                location_name = Options.LocationName,
                language_code = Options.LanguageCode,
                se_domain = Options.SearchDomain,
                keyword = gtin,
                depth = Math.Clamp(Options.SearchDepth, 1, 120),
                priority = 1,
                tag = gtin
            }
        };

        using var response = await httpClient.PostAsJsonAsync(
            "v3/merchant/google/products/task_post",
            payload,
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        using var document = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("DataForSEO returned an empty task response.");

        return ReadTaskId(document);
    }

    private async Task<string> CreateProductInfoTaskAsync(string productId, CancellationToken cancellationToken)
    {
        var payload = new[]
        {
            new
            {
                location_name = Options.LocationName,
                language_code = Options.LanguageCode,
                se_domain = Options.SearchDomain,
                product_id = productId,
                priority = 1
            }
        };

        using var response = await httpClient.PostAsJsonAsync(
            "v3/merchant/google/product_info/task_post",
            payload,
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        using var document = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("DataForSEO returned an empty task response.");

        return ReadTaskId(document);
    }

    private async Task<JsonDocument> WaitForTaskAsync(string path, CancellationToken cancellationToken)
    {
        var attempts = Math.Max(1, Options.PollAttempts);
        var delay = TimeSpan.FromSeconds(Math.Max(1, Options.PollDelaySeconds));

        for (var attempt = 0; attempt < attempts; attempt++)
        {
            using var response = await httpClient.GetAsync(path, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);
            var document = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("DataForSEO returned an empty task response.");

            if (HasCompletedResult(document))
                return document;

            document.Dispose();
            if (attempt + 1 < attempts)
                await Task.Delay(delay, cancellationToken);
        }

        throw new TimeoutException("DataForSEO did not return a completed Google Shopping task within the configured polling window.");
    }

    private static bool HasCompletedResult(JsonDocument document)
    {
        if (!document.RootElement.TryGetProperty("tasks", out var tasks) || tasks.GetArrayLength() == 0)
            return false;

        var task = tasks[0];
        if (!task.TryGetProperty("status_code", out var statusCode) || statusCode.GetInt32() >= 40000)
            throw new InvalidOperationException(ReadStatusMessage(task));

        return task.TryGetProperty("result", out var result)
            && result.ValueKind == JsonValueKind.Array
            && result.GetArrayLength() > 0;
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

    private static IEnumerable<string> ExtractProductIds(JsonDocument document)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var element in Walk(document.RootElement))
        {
            if (element.ValueKind != JsonValueKind.Object ||
                !element.TryGetProperty("type", out var type) ||
                type.ValueKind != JsonValueKind.String ||
                !string.Equals(type.GetString(), "google_shopping_serp", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!element.TryGetProperty("product_id", out var productId))
                continue;

            var value = productId.GetString();
            if (!string.IsNullOrWhiteSpace(value) && seen.Add(value))
                yield return value;
        }
    }

    private static IEnumerable<DiaperScout.Application.RetailerDiscoveryCandidate> ExtractCandidates(
        string gtin,
        string productId,
        JsonDocument document)
    {
        if (!document.RootElement.TryGetProperty("tasks", out var tasks) || tasks.GetArrayLength() == 0)
            yield break;

        var result = tasks[0].TryGetProperty("result", out var results) && results.GetArrayLength() > 0
            ? results[0]
            : default;

        if (result.ValueKind == JsonValueKind.Undefined ||
            !result.TryGetProperty("items", out var items))
            yield break;

        foreach (var item in items.EnumerateArray())
        {
            if (!item.TryGetProperty("sellers", out var sellers) || sellers.ValueKind != JsonValueKind.Array)
                continue;

            foreach (var seller in sellers.EnumerateArray())
            {
                var name = seller.TryGetProperty("title", out var title) ? title.GetString() : null;
                var url = seller.TryGetProperty("url", out var sellerUrl) ? sellerUrl.GetString() : null;
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(url) || !IsHttpUrl(url))
                    continue;

                yield return new DiaperScout.Application.RetailerDiscoveryCandidate(
                    gtin,
                    name.Trim(),
                    GetOrigin(url),
                    url.Trim(),
                    productId,
                    result.TryGetProperty("check_url", out var checkUrl) ? checkUrl.GetString() : null);
            }
        }
    }

    private static IEnumerable<JsonElement> Walk(JsonElement element)
    {
        yield return element;
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            foreach (var child in Walk(property.Value))
                yield return child;
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in element.EnumerateArray())
            foreach (var nested in Walk(child))
                yield return nested;
        }
    }

    private static bool IsHttpUrl(string value) =>
        Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    private static string GetOrigin(string value)
    {
        var uri = new Uri(value.Trim());
        return uri.GetLeftPart(UriPartial.Authority);
    }

    private static string NormalizeUrl(string value) =>
        value.Trim().TrimEnd('/');

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new HttpRequestException($"DataForSEO returned HTTP {(int)response.StatusCode}: {body}");
    }
}
