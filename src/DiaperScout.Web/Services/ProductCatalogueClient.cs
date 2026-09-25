using System.Net;
using System.Net.Http.Json;
using DiaperScout.Application;
using DiaperScout.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Components.Forms;

namespace DiaperScout.Web.Services;

public sealed class ProductCatalogueClient(HttpClient client)
{

    public async Task<HttpResponseMessage> GetProductImageAsync(
        Guid productId,
        Guid imageId,
        CancellationToken cancellationToken = default)
    {
        return await client.GetAsync(
            $"api/v1/products/{productId}/images/{imageId}",
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
    }

    public async Task<ProductCatalogueSearchResult> SearchAsync(
        string? query,
        CatalogueProductFilters? filters = null,
        string sort = "relevance",
        int limit = 24,
        int offset = 0,
        CancellationToken cancellationToken = default)
    {
        filters ??= new CatalogueProductFilters([], [], [], [], [], []);

        var parameters = new List<string>();

        if (!string.IsNullOrWhiteSpace(query))
            parameters.Add($"q={Uri.EscapeDataString(query.Trim())}");

        AddParameter(
            parameters,
            "manufacturer",
            filters.ManufacturerIds.Select(id => id.ToString()));

        AddParameter(
            parameters,
            "brand",
            filters.BrandIds.Select(id => id.ToString()));

        AddParameter(
            parameters,
            "productType",
            filters.ProductTypes.Select(value => value.ToString()));

        AddParameter(
            parameters,
            "size",
            filters.Sizes);

        AddParameter(
            parameters,
            "backing",
            filters.Backings.Select(value => value.ToString()));

        AddParameter(
            parameters,
            "packaging",
            filters.PackagingTypes.Select(value => value.ToString()));

        if (!string.IsNullOrWhiteSpace(sort) &&
            !string.Equals(sort, "relevance", StringComparison.OrdinalIgnoreCase))
        {
            parameters.Add($"sort={Uri.EscapeDataString(sort)}");
        }

        parameters.Add($"limit={Math.Clamp(limit, 1, 50)}");
        parameters.Add($"offset={Math.Max(offset, 0)}");

        var path =
            "api/v1/products" +
            (parameters.Count > 0
                ? "?" + string.Join("&", parameters)
                : string.Empty);

        using var response = await client.GetAsync(path, cancellationToken);

        if (!response.IsSuccessStatusCode)
            return ProductCatalogueSearchResult.Failed();

        var catalogue =
            await response.Content.ReadFromJsonAsync<CatalogueProductSearch>(
                cancellationToken);

        return catalogue is null
            ? ProductCatalogueSearchResult.Failed()
            : ProductCatalogueSearchResult.Found(catalogue);
    }

    public async Task<ProductCatalogueSearchResult> SearchManagementAsync(
        string? query,
        CatalogueProductManagementFilters? filters = null,
        string sort = "name",
        int limit = 25,
        int offset = 0,
        CancellationToken cancellationToken = default)
    {
        filters ??= new CatalogueProductManagementFilters([], [], []);
        var parameters = new List<string>();

        if (!string.IsNullOrWhiteSpace(query))
            parameters.Add($"q={Uri.EscapeDataString(query.Trim())}");

        AddParameter(parameters, "manufacturer", filters.ManufacturerIds.Select(id => id.ToString()));
        AddParameter(parameters, "productType", filters.ProductTypes.Select(value => value.ToString()));
        AddParameter(parameters, "status", filters.Statuses.Select(value => value.ToString()));

        if (!string.IsNullOrWhiteSpace(sort))
            parameters.Add($"sort={Uri.EscapeDataString(sort)}");

        parameters.Add($"limit={Math.Clamp(limit, 1, 50)}");
        parameters.Add($"offset={Math.Max(offset, 0)}");

        using var response = await client.GetAsync(
            "api/v1/catalogue-management/products?" + string.Join("&", parameters),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
            return ProductCatalogueSearchResult.Failed();

        var catalogue = await response.Content.ReadFromJsonAsync<CatalogueProductSearch>(cancellationToken);
        return catalogue is null
            ? ProductCatalogueSearchResult.Failed()
            : ProductCatalogueSearchResult.Found(catalogue);
    }

    private static void AddParameter(
        List<string> parameters,
        string name,
        IEnumerable<string> values)
    {
        var value = string.Join(',', values);

        if (!string.IsNullOrWhiteSpace(value))
            parameters.Add($"{name}={Uri.EscapeDataString(value)}");
    }

    public async Task<bool> SetProductDescriptionVisibilityAsync(
        Guid productId,
        CatalogueContentVisibility visibility,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.PutAsJsonAsync(
            $"api/v1/products/{productId}/description-visibility",
            new { visibility },
            cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<CatalogueDataQualitySummary?> GetProductDataQualityAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await client.GetAsync(
            "api/v1/catalogue-management/data-quality",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<CatalogueDataQualitySummary>(cancellationToken);
    }

    public async Task<CatalogueProductImageContent?> GetProductImageContentAsync(
        Guid productId,
        Guid imageId,
        bool moderatorOnly,
        CancellationToken cancellationToken = default)
    {
        var path = moderatorOnly
            ? $"api/v1/products/{productId}/moderator-images/{imageId}"
            : $"api/v1/products/{productId}/images/{imageId}";

        using var response = await client.GetAsync(
            path,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
            return null;

        var contentType = response.Content.Headers.ContentType?.MediaType
            ?? "application/octet-stream";
        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
            ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
            ?? $"{imageId}{GetFileExtension(contentType)}";
        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);

        return new CatalogueProductImageContent(
            new MemoryStream(bytes, writable: false),
            contentType,
            fileName);
    }

    private static string GetFileExtension(string contentType) =>
        contentType.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            "image/gif" => ".gif",
            "image/avif" => ".avif",
            _ => string.Empty
        };

    public async Task<CatalogueProductManagementResult> GetProductManagementAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.GetAsync(
            $"api/v1/catalogue-management/products/{productId}",
            cancellationToken);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            return CatalogueProductManagementResult.AccessDenied();
        if (response.StatusCode == HttpStatusCode.NotFound)
            return CatalogueProductManagementResult.NotFound();
        if (!response.IsSuccessStatusCode)
            return CatalogueProductManagementResult.Failed();

        var product = await response.Content.ReadFromJsonAsync<CatalogueProductManagementDetails>(cancellationToken);
        return product is null ? CatalogueProductManagementResult.Failed() : CatalogueProductManagementResult.Found(product);
    }

    public async Task<CatalogueProductImageAddResult> AddProductImageAsync(
        Guid productId,
        IBrowserFile file,
        AddCanonicalProductImageMetadata metadata,
        CancellationToken cancellationToken = default)
    {
        await using var stream = file.OpenReadStream(15 * 1024 * 1024, cancellationToken);
        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(stream);
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
        content.Add(streamContent, "file", file.Name);
        content.Add(new StringContent(metadata.Role.ToString()), "role");
        content.Add(new StringContent(metadata.SourceType.ToString()), "sourceType");
        content.Add(new StringContent(metadata.SourceUrl ?? string.Empty), "sourceUrl");
        content.Add(new StringContent(metadata.SourceNotes ?? string.Empty), "sourceNotes");
        content.Add(new StringContent(metadata.PermissionStatus.ToString()), "permissionStatus");
        content.Add(new StringContent(metadata.PermissionEvidence ?? string.Empty), "permissionEvidence");
        content.Add(new StringContent(metadata.IsPrimary.ToString()), "isPrimary");
        content.Add(new StringContent(metadata.SourceSummary), "sourceSummary");
        content.Add(new StringContent(string.Join('\n', metadata.SourceReferences)), "sourceReferences");
        content.Add(new StringContent(metadata.EditorialRationale), "editorialRationale");

        using var response = await client.PostAsync(
            $"api/v1/catalogue-management/products/{productId}/images",
            content,
            cancellationToken);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            return CatalogueProductImageAddResult.AccessDenied();
        if (response.StatusCode == HttpStatusCode.NotFound)
            return CatalogueProductImageAddResult.NotFound();
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(cancellationToken);
            return CatalogueProductImageAddResult.Invalid(
                problem?.Errors is null
                    ? new Dictionary<string, string[]> { ["image"] = ["The image could not be saved."] }
                    : new Dictionary<string, string[]>(problem.Errors));
        }

        if (!response.IsSuccessStatusCode)
            return CatalogueProductImageAddResult.Failed();

        var image = await response.Content.ReadFromJsonAsync<CatalogueModeratorProductImage>(cancellationToken);
        return image is null ? CatalogueProductImageAddResult.Failed() : CatalogueProductImageAddResult.Saved(image);
    }

    public async Task<CatalogueProductManagementUpdateResult> UpdateProductImageAsync(
        Guid productId,
        Guid imageId,
        UpdateCanonicalProductImageMetadata request,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.PutAsJsonAsync(
            $"api/v1/catalogue-management/products/{productId}/images/{imageId}",
            request,
            cancellationToken);
        return await ReadManagementMutationResponseAsync(response, cancellationToken);
    }

    public async Task<CatalogueProductManagementUpdateResult> RemoveProductImageAsync(
        Guid productId,
        Guid imageId,
        RemoveCanonicalProductElement request,
        CancellationToken cancellationToken = default)
    {
        using var message = new HttpRequestMessage(
            HttpMethod.Delete,
            $"api/v1/catalogue-management/products/{productId}/images/{imageId}")
        {
            Content = JsonContent.Create(request)
        };
        using var response = await client.SendAsync(message, cancellationToken);
        return await ReadManagementMutationResponseAsync(response, cancellationToken);
    }

    public async Task<CatalogueProductManagementUpdateResult> SetProductImagePrimaryAsync(
        Guid productId,
        Guid imageId,
        RemoveCanonicalProductElement request,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.PostAsJsonAsync(
            $"api/v1/catalogue-management/products/{productId}/images/{imageId}/primary",
            request,
            cancellationToken);
        return await ReadManagementMutationResponseAsync(response, cancellationToken);
    }

    public async Task<CatalogueProductManagementUpdateResult> UpdateProductIdentityAsync(
        Guid productId,
        UpdateCanonicalProductIdentity request,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.PutAsJsonAsync(
            $"api/v1/catalogue-management/products/{productId}/identity",
            request,
            cancellationToken);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            return CatalogueProductManagementUpdateResult.AccessDenied();
        if (response.StatusCode == HttpStatusCode.NotFound)
            return CatalogueProductManagementUpdateResult.NotFound();
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(cancellationToken);
            return CatalogueProductManagementUpdateResult.Invalid(
                problem?.Errors is null
                    ? new Dictionary<string, string[]> { ["product"] = ["The product could not be saved."] }
                    : new Dictionary<string, string[]>(problem.Errors));
        }

        return response.IsSuccessStatusCode
            ? CatalogueProductManagementUpdateResult.Saved()
            : CatalogueProductManagementUpdateResult.Failed();
    }

    public async Task<CatalogueProductManagementUpdateResult> SetProductStatusAsync(
        Guid productId,
        SetCanonicalProductStatus request,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.PutAsJsonAsync(
            $"api/v1/catalogue-management/products/{productId}/status",
            request,
            cancellationToken);
        return await ReadManagementMutationResponseAsync(response, cancellationToken);
    }

    public async Task<CatalogueProductManagementUpdateResult> AddProductVariantAsync(
        Guid productId,
        CreateCanonicalProductVariantManagement request,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.PostAsJsonAsync(
            $"api/v1/catalogue-management/products/{productId}/variants",
            request,
            cancellationToken);
        return await ReadManagementMutationResponseAsync(response, cancellationToken);
    }

    public async Task<CatalogueProductManagementUpdateResult> UpdateProductVariantAsync(
        Guid productId,
        Guid variantId,
        UpdateCanonicalProductVariantManagement request,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.PutAsJsonAsync(
            $"api/v1/catalogue-management/products/{productId}/variants/{variantId}",
            request,
            cancellationToken);
        return await ReadManagementMutationResponseAsync(response, cancellationToken);
    }

    public async Task<CatalogueProductManagementUpdateResult> RemoveProductVariantAsync(
        Guid productId,
        Guid variantId,
        RemoveCanonicalProductElement request,
        CancellationToken cancellationToken = default)
    {
        using var message = new HttpRequestMessage(
            HttpMethod.Delete,
            $"api/v1/catalogue-management/products/{productId}/variants/{variantId}")
        {
            Content = JsonContent.Create(request)
        };
        using var response = await client.SendAsync(message, cancellationToken);
        return await ReadManagementMutationResponseAsync(response, cancellationToken);
    }

    public async Task<CatalogueProductManagementUpdateResult> AddProductSizeAsync(
        Guid productId,
        Guid variantId,
        CreateCanonicalProductSizeManagement request,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.PostAsJsonAsync(
            $"api/v1/catalogue-management/products/{productId}/variants/{variantId}/sizes",
            request,
            cancellationToken);
        return await ReadManagementMutationResponseAsync(response, cancellationToken);
    }

    public async Task<CatalogueProductManagementUpdateResult> UpdateProductSizeAsync(
        Guid productId,
        Guid variantId,
        Guid sizeId,
        UpdateCanonicalProductSizeManagement request,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.PutAsJsonAsync(
            $"api/v1/catalogue-management/products/{productId}/variants/{variantId}/sizes/{sizeId}",
            request,
            cancellationToken);
        return await ReadManagementMutationResponseAsync(response, cancellationToken);
    }

    public async Task<CatalogueProductManagementUpdateResult> RemoveProductSizeAsync(
        Guid productId,
        Guid variantId,
        Guid sizeId,
        RemoveCanonicalProductElement request,
        CancellationToken cancellationToken = default)
    {
        using var message = new HttpRequestMessage(
            HttpMethod.Delete,
            $"api/v1/catalogue-management/products/{productId}/variants/{variantId}/sizes/{sizeId}")
        {
            Content = JsonContent.Create(request)
        };
        using var response = await client.SendAsync(message, cancellationToken);
        return await ReadManagementMutationResponseAsync(response, cancellationToken);
    }

    private static async Task<CatalogueProductManagementUpdateResult> ReadManagementMutationResponseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            return CatalogueProductManagementUpdateResult.AccessDenied();
        if (response.StatusCode == HttpStatusCode.NotFound)
            return CatalogueProductManagementUpdateResult.NotFound();
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(cancellationToken);
            return CatalogueProductManagementUpdateResult.Invalid(
                problem?.Errors is null
                    ? new Dictionary<string, string[]> { ["product"] = ["The catalogue change could not be saved."] }
                    : new Dictionary<string, string[]>(problem.Errors));
        }

        return response.IsSuccessStatusCode
            ? CatalogueProductManagementUpdateResult.Saved()
            : CatalogueProductManagementUpdateResult.Failed();
    }

    public async Task<CatalogueModeratorProductPreviewResult> GetModeratorProductPreviewAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.GetAsync(
            $"api/v1/products/{productId}/moderator-preview",
            cancellationToken);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            return CatalogueModeratorProductPreviewResult.AccessDenied();
        if (response.StatusCode == HttpStatusCode.NotFound)
            return CatalogueModeratorProductPreviewResult.NotFound();
        if (!response.IsSuccessStatusCode)
            return CatalogueModeratorProductPreviewResult.Failed();

        var product = await response.Content.ReadFromJsonAsync<CatalogueModeratorProductDetails>(cancellationToken);
        return product is null ? CatalogueModeratorProductPreviewResult.Failed() : CatalogueModeratorProductPreviewResult.Found(product);
    }

    public async Task<ProductCatalogueDetailResult> GetAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.GetAsync(
            $"api/v1/products/{Uri.EscapeDataString(slug)}",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return ProductCatalogueDetailResult.NotFound();

        if (!response.IsSuccessStatusCode)
            return ProductCatalogueDetailResult.Failed();

        var product =
            await response.Content.ReadFromJsonAsync<CatalogueProductDetails>(
                cancellationToken);

        return product is null
            ? ProductCatalogueDetailResult.Failed()
            : ProductCatalogueDetailResult.Found(product);
    }

    public async Task<CatalogueSubmissionResult> CreateSubmissionAsync(
        CreateCatalogueSubmissionRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.PostAsJsonAsync(
            "api/v1/catalogue-submissions",
            request,
            cancellationToken);

        return await ReadSubmissionResponseAsync(
            response,
            cancellationToken);
    }

    public async Task<CatalogueSubmissionImportClientResult> ImportSubmissionsCsvAsync(
        IBrowserFile file,
        CancellationToken cancellationToken = default)
    {
        await using var stream = file.OpenReadStream(10 * 1024 * 1024, cancellationToken);
        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(stream);
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");
        content.Add(streamContent, "file", file.Name);

        using var response = await client.PostAsync(
            "api/v1/catalogue-submissions/import-csv",
            content,
            cancellationToken);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            return CatalogueSubmissionImportClientResult.AccessDenied();

        if (!response.IsSuccessStatusCode)
        {
            var problem = await response.Content.ReadFromJsonAsync<ImportErrorResponse>(cancellationToken);
            return CatalogueSubmissionImportClientResult.Failed(problem?.Message);
        }

        var result = await response.Content.ReadFromJsonAsync<CatalogueSubmissionImportResult>(cancellationToken);
        return result is null
            ? CatalogueSubmissionImportClientResult.Failed("The import completed without a result.")
            : CatalogueSubmissionImportClientResult.Succeeded(result);
    }

    public async Task<CatalogueEntryOptions> GetCatalogueEntryOptionsAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await client.GetAsync(
            "api/v1/catalogue-management/entry-options",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
            return new CatalogueEntryOptions([], []);

        return await response.Content.ReadFromJsonAsync<CatalogueEntryOptions>(cancellationToken)
            ?? new CatalogueEntryOptions([], []);
    }

    public string GetSubmissionImportTemplateUrl() =>
        new Uri(client.BaseAddress!, "api/v1/catalogue-submissions/import-template").ToString();

    public async Task<CatalogueSubmissionQueueResult> GetSubmissionsAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await client.GetAsync(
            "api/v1/catalogue-submissions",
            cancellationToken);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            return CatalogueSubmissionQueueResult.AccessDenied();

        if (!response.IsSuccessStatusCode)
            return CatalogueSubmissionQueueResult.Failed();

        var submissions = await response.Content.ReadFromJsonAsync<
            IReadOnlyList<CatalogueSubmissionQueueItem>>(cancellationToken);

        return submissions is null
            ? CatalogueSubmissionQueueResult.Failed()
            : CatalogueSubmissionQueueResult.Found(submissions);
    }

    public async Task<CatalogueSubmissionResult> GetSubmissionAsync(
        Guid submissionId,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.GetAsync(
            $"api/v1/catalogue-submissions/{submissionId}",
            cancellationToken);

        return await ReadSubmissionResponseAsync(response, cancellationToken);
    }

    public async Task<CatalogueSubmissionResult> DeleteSubmissionAsync(
        Guid submissionId,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.DeleteAsync(
            $"api/v1/catalogue-submissions/{submissionId}",
            cancellationToken);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            return CatalogueSubmissionResult.AccessDenied();

        if (response.StatusCode == HttpStatusCode.NotFound)
            return CatalogueSubmissionResult.Failed();

        return response.IsSuccessStatusCode
            ? CatalogueSubmissionResult.Deleted()
            : CatalogueSubmissionResult.Failed();
    }

    public async Task<CatalogueSubmissionVariantsResult> GetSubmissionVariantsAsync(
        Guid submissionId,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.GetAsync(
            $"api/v1/catalogue-submissions/{submissionId}/variants",
            cancellationToken);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            return CatalogueSubmissionVariantsResult.AccessDenied();

        if (!response.IsSuccessStatusCode)
            return CatalogueSubmissionVariantsResult.Failed();

        var variants = await response.Content.ReadFromJsonAsync<
            IReadOnlyList<CatalogueSubmissionVariantReceipt>>(cancellationToken);

        return variants is null
            ? CatalogueSubmissionVariantsResult.Failed()
            : CatalogueSubmissionVariantsResult.Found(variants);
    }

    public async Task<CatalogueSubmissionSizeVariantsResult> GetSubmissionSizeVariantsAsync(
        Guid submissionId,
        Guid variantId,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.GetAsync(
            $"api/v1/catalogue-submissions/{submissionId}/variants/{variantId}/sizes",
            cancellationToken);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            return CatalogueSubmissionSizeVariantsResult.AccessDenied();

        if (!response.IsSuccessStatusCode)
            return CatalogueSubmissionSizeVariantsResult.Failed();

        var sizes = await response.Content.ReadFromJsonAsync<
            IReadOnlyList<CatalogueSubmissionSizeVariantReceipt>>(cancellationToken);

        return sizes is null
            ? CatalogueSubmissionSizeVariantsResult.Failed()
            : CatalogueSubmissionSizeVariantsResult.Found(sizes);
    }

    public async Task<CatalogueSubmissionSizeVariantResult> AddSubmissionSizeVariantAsync(
        Guid submissionId,
        Guid variantId,
        AddCatalogueSubmissionSizeVariantRequest request,
        CancellationToken cancellationToken = default) =>
        await SendSizeVariantAsync(
            HttpMethod.Post,
            $"api/v1/catalogue-submissions/{submissionId}/variants/{variantId}/sizes",
            request,
            cancellationToken);

    public async Task<CatalogueSubmissionSizeVariantResult> UpdateSubmissionSizeVariantAsync(
        Guid submissionId,
        Guid variantId,
        Guid sizeVariantId,
        UpdateCatalogueSubmissionSizeVariantRequest request,
        CancellationToken cancellationToken = default) =>
        await SendSizeVariantAsync(
            HttpMethod.Put,
            $"api/v1/catalogue-submissions/{submissionId}/variants/{variantId}/sizes/{sizeVariantId}",
            request,
            cancellationToken);

    public async Task<CatalogueSubmissionSizeVariantResult> RemoveSubmissionSizeVariantAsync(
        Guid submissionId,
        Guid variantId,
        Guid sizeVariantId,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.DeleteAsync(
            $"api/v1/catalogue-submissions/{submissionId}/variants/{variantId}/sizes/{sizeVariantId}",
            cancellationToken);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            return CatalogueSubmissionSizeVariantResult.AccessDenied();

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(cancellationToken);
            return CatalogueSubmissionSizeVariantResult.Invalid(
                problem?.Errors is null
                    ? new Dictionary<string, string[]> { ["sizeVariant"] = ["The size variant could not be removed."] }
                    : new Dictionary<string, string[]>(problem.Errors));
        }

        return response.IsSuccessStatusCode
            ? CatalogueSubmissionSizeVariantResult.Removed()
            : CatalogueSubmissionSizeVariantResult.Failed();
    }

    private async Task<CatalogueSubmissionSizeVariantResult> SendSizeVariantAsync<TRequest>(
        HttpMethod method,
        string url,
        TRequest request,
        CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(method, url)
        {
            Content = JsonContent.Create(request)
        };

        using var response = await client.SendAsync(message, cancellationToken);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            return CatalogueSubmissionSizeVariantResult.AccessDenied();

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(cancellationToken);
            return CatalogueSubmissionSizeVariantResult.Invalid(
                problem?.Errors is null
                    ? new Dictionary<string, string[]> { ["sizeVariant"] = ["The size variant could not be saved."] }
                    : new Dictionary<string, string[]>(problem.Errors));
        }

        if (!response.IsSuccessStatusCode)
            return CatalogueSubmissionSizeVariantResult.Failed();

        var receipt = await response.Content.ReadFromJsonAsync<CatalogueSubmissionSizeVariantReceipt>(cancellationToken);
        return receipt is null
            ? CatalogueSubmissionSizeVariantResult.Failed()
            : CatalogueSubmissionSizeVariantResult.Saved(receipt);
    }

    public async Task<CatalogueSubmissionVariantOverrideResult> GetSubmissionVariantOverrideAsync(
        Guid submissionId,
        Guid variantId,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.GetAsync(
            $"api/v1/catalogue-submissions/{submissionId}/variants/{variantId}/override",
            cancellationToken);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            return CatalogueSubmissionVariantOverrideResult.AccessDenied();
        if (response.StatusCode == HttpStatusCode.NoContent)
            return CatalogueSubmissionVariantOverrideResult.Found(null);
        if (!response.IsSuccessStatusCode)
            return CatalogueSubmissionVariantOverrideResult.Failed($"The variant details could not be loaded (HTTP {(int)response.StatusCode}).");

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(content) || content.Trim() == "null")
            return CatalogueSubmissionVariantOverrideResult.Found(null);

        var value = System.Text.Json.JsonSerializer.Deserialize<CatalogueSubmissionVariantOverrideReceipt>(
            content,
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        return CatalogueSubmissionVariantOverrideResult.Found(value);
    }

    public async Task<CatalogueSubmissionVariantOverrideResult> UpdateSubmissionVariantOverrideAsync(
        Guid submissionId,
        Guid variantId,
        UpdateCatalogueSubmissionVariantOverrideRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.PutAsJsonAsync(
            $"api/v1/catalogue-submissions/{submissionId}/variants/{variantId}/override",
            request,
            cancellationToken);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            return CatalogueSubmissionVariantOverrideResult.AccessDenied();

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(cancellationToken);
            return CatalogueSubmissionVariantOverrideResult.Invalid(
                problem?.Errors is not null
                    ? new Dictionary<string, string[]>(problem.Errors)
                    : new Dictionary<string, string[]> { ["override"] = ["The variant difference could not be saved."] });
        }

        if (!response.IsSuccessStatusCode)
            return CatalogueSubmissionVariantOverrideResult.Failed($"The variant attribute could not be saved (HTTP {(int)response.StatusCode}).");

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(content) || content.Trim() == "null")
            return CatalogueSubmissionVariantOverrideResult.Failed("The server saved the variant attribute but returned no receipt.");

        var value = System.Text.Json.JsonSerializer.Deserialize<CatalogueSubmissionVariantOverrideReceipt>(
            content,
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        return value is null
            ? CatalogueSubmissionVariantOverrideResult.Failed("The server saved the variant attribute but returned an invalid receipt.")
            : CatalogueSubmissionVariantOverrideResult.Saved(value);
    }

    public async Task<CatalogueSubmissionVariantResult> AddSubmissionBaseVariantAsync(
        Guid submissionId,
        CancellationToken cancellationToken = default)
        => await AddSubmissionVariantAsync(
            submissionId,
            new AddCatalogueSubmissionVariantRequest(null),
            cancellationToken);

    public async Task<CatalogueSubmissionVariantResult> AddSubmissionNamedVariantAsync(
        Guid submissionId,
        string name,
        CancellationToken cancellationToken = default)
        => await AddSubmissionVariantAsync(
            submissionId,
            new AddCatalogueSubmissionVariantRequest(name),
            cancellationToken);

    public async Task<CatalogueSubmissionVariantResult> AddSubmissionVariantAsync(
        Guid submissionId,
        AddCatalogueSubmissionVariantRequest request,
        CancellationToken cancellationToken = default)
        => await SendVariantAsync(
            HttpMethod.Post,
            $"api/v1/catalogue-submissions/{submissionId}/variants",
            request,
            cancellationToken);

    public async Task<CatalogueSubmissionVariantResult> UpdateSubmissionVariantAsync(
        Guid submissionId,
        Guid variantId,
        UpdateCatalogueSubmissionVariantRequest request,
        CancellationToken cancellationToken = default)
        => await SendVariantAsync(
            HttpMethod.Put,
            $"api/v1/catalogue-submissions/{submissionId}/variants/{variantId}",
            request,
            cancellationToken);

    public async Task<CatalogueSubmissionVariantResult> RemoveSubmissionVariantAsync(
        Guid submissionId,
        Guid variantId,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.DeleteAsync(
            $"api/v1/catalogue-submissions/{submissionId}/variants/{variantId}",
            cancellationToken);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            return CatalogueSubmissionVariantResult.AccessDenied();

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(cancellationToken);
            return CatalogueSubmissionVariantResult.Invalid(
                problem?.Errors is null
                    ? new Dictionary<string, string[]>
                    {
                        ["variant"] = ["The product variant could not be removed."]
                    }
                    : new Dictionary<string, string[]>(problem.Errors));
        }

        return response.IsSuccessStatusCode
            ? CatalogueSubmissionVariantResult.Removed()
            : CatalogueSubmissionVariantResult.Failed();
    }

    private async Task<CatalogueSubmissionVariantResult> SendVariantAsync<TRequest>(
        HttpMethod method,
        string url,
        TRequest request,
        CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(method, url)
        {
            Content = JsonContent.Create(request)
        };

        using var response = await client.SendAsync(message, cancellationToken);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            return CatalogueSubmissionVariantResult.AccessDenied();

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(cancellationToken);
            return CatalogueSubmissionVariantResult.Invalid(
                problem?.Errors is null
                    ? new Dictionary<string, string[]>
                    {
                        ["variant"] = ["The product variant could not be saved."]
                    }
                    : new Dictionary<string, string[]>(problem.Errors));
        }

        if (!response.IsSuccessStatusCode)
            return CatalogueSubmissionVariantResult.Failed();

        var receipt = await response.Content.ReadFromJsonAsync<CatalogueSubmissionVariantReceipt>(cancellationToken);
        return receipt is null
            ? CatalogueSubmissionVariantResult.Failed()
            : CatalogueSubmissionVariantResult.Saved(receipt);
    }

    public async Task<CatalogueSubmissionResult> ResolveSubmissionEntitiesAsync(
        Guid submissionId,
        ResolveCatalogueSubmissionEntitiesRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.PutAsJsonAsync(
            $"api/v1/catalogue-submissions/{submissionId}/entity-resolution",
            request,
            cancellationToken);

        return await ReadSubmissionResponseAsync(response, cancellationToken);
    }

    public async Task<CatalogueSubmissionResult> UpdateSubmissionIdentityAsync(
        Guid submissionId,
        UpdateCatalogueSubmissionIdentityRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.PutAsJsonAsync(
            $"api/v1/catalogue-submissions/{submissionId}/identity",
            request,
            cancellationToken);

        return await ReadSubmissionResponseAsync(
            response,
            cancellationToken);
    }

    public async Task<CatalogueSubmissionResult> UpdateSubmissionSpecificationsAsync(
        Guid submissionId,
        UpdateCatalogueSubmissionSpecificationsRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.PutAsJsonAsync(
            $"api/v1/catalogue-submissions/{submissionId}/specifications",
            request,
            cancellationToken);

        return await ReadSubmissionResponseAsync(
            response,
            cancellationToken);
    }

    public async Task<CatalogueSubmissionResult> UpdateSubmissionDescriptionVisibilityAsync(
        Guid submissionId,
        CatalogueContentVisibility visibility,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.PutAsJsonAsync(
            $"api/v1/catalogue-submissions/{submissionId}/description-visibility",
            new UpdateCatalogueSubmissionDescriptionVisibilityRequest(visibility),
            cancellationToken);

        return await ReadSubmissionResponseAsync(
            response,
            cancellationToken);
    }

    public async Task<CatalogueSubmissionResult> MarkReadyForReviewAsync(
        Guid submissionId,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.PostAsync(
            $"api/v1/catalogue-submissions/{submissionId}/ready-for-review",
            content: null,
            cancellationToken);

        return await ReadSubmissionResponseAsync(
            response,
            cancellationToken);
    }

    public async Task<CatalogueSubmissionResult> ReturnSubmissionToVerificationAsync(
        Guid submissionId,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.PostAsync(
            $"api/v1/catalogue-submissions/{submissionId}/return-to-verification",
            content: null,
            cancellationToken);

        return await ReadSubmissionResponseAsync(
            response,
            cancellationToken);
    }

    public async Task<CatalogueSubmissionReviewResult> ReviewSubmissionAsync(
        Guid submissionId,
        EditorialOutcome outcome,
        string? rationale,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.PostAsJsonAsync(
            $"api/v1/catalogue-submissions/{submissionId}/review",
            new ReviewCatalogueSubmissionRequest(outcome, rationale),
            cancellationToken);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            return CatalogueSubmissionReviewResult.AccessDenied();

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var problem =
                await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(
                    cancellationToken);

            IReadOnlyDictionary<string, string[]> errors =
                problem?.Errors is not null
                    ? new Dictionary<string, string[]>(problem.Errors)
                    : new Dictionary<string, string[]>
                    {
                        ["review"] =
                        [
                            "The editorial review could not be completed."
                        ]
                    };

            return CatalogueSubmissionReviewResult.Invalid(errors);
        }

        if (!response.IsSuccessStatusCode)
            return CatalogueSubmissionReviewResult.Failed();

        var receipt =
            await response.Content.ReadFromJsonAsync<CatalogueSubmissionEditorialDecisionReceipt>(
                cancellationToken);

        return receipt is null
            ? CatalogueSubmissionReviewResult.Failed()
            : CatalogueSubmissionReviewResult.Decided(receipt);
    }

    public async Task<CataloguePublicationResult> PublishSubmissionAsync(
        Guid submissionId,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.PostAsync(
            $"api/v1/catalogue-submissions/{submissionId}/publish",
            content: null,
            cancellationToken);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            return CataloguePublicationResult.AccessDenied();

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var problem =
                await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(
                    cancellationToken);

            IReadOnlyDictionary<string, string[]> errors =
                problem?.Errors is not null
                    ? new Dictionary<string, string[]>(problem.Errors)
                    : new Dictionary<string, string[]>
                    {
                        ["publication"] =
                        [
                            "The catalogue submission could not be published."
                        ]
                    };

            return CataloguePublicationResult.Invalid(errors);
        }

        if (!response.IsSuccessStatusCode)
            return CataloguePublicationResult.Failed();

        var receipt =
            await response.Content.ReadFromJsonAsync<CataloguePublicationReceipt>(
                cancellationToken);

        return receipt is null
            ? CataloguePublicationResult.Failed()
            : CataloguePublicationResult.Published(receipt);
    }

    public async Task<CatalogueRetailersResult> GetRetailersAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await client.GetAsync(
            "api/v1/catalogue-submissions/retailers",
            cancellationToken);

        if (response.StatusCode is
            HttpStatusCode.Unauthorized or
            HttpStatusCode.Forbidden)
        {
            return CatalogueRetailersResult.AccessDenied();
        }

        if (!response.IsSuccessStatusCode)
            return CatalogueRetailersResult.Failed();

        var retailers =
            await response.Content.ReadFromJsonAsync<
                IReadOnlyList<CatalogueRetailerOption>>(
                cancellationToken);

        return retailers is null
            ? CatalogueRetailersResult.Failed()
            : CatalogueRetailersResult.Found(retailers);
    }

    public async Task<CatalogueRetailDestinationsResult> GetRetailDestinationsAsync(
        Guid submissionId,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.GetAsync(
            $"api/v1/catalogue-submissions/{submissionId}/retail-destinations",
            cancellationToken);

        if (response.StatusCode is
            HttpStatusCode.Unauthorized or
            HttpStatusCode.Forbidden)
        {
            return CatalogueRetailDestinationsResult.AccessDenied();
        }

        if (!response.IsSuccessStatusCode)
            return CatalogueRetailDestinationsResult.Failed();

        var destinations =
            await response.Content.ReadFromJsonAsync<
                IReadOnlyList<CatalogueSubmissionRetailDestinationReceipt>>(
                cancellationToken);

        return destinations is null
            ? CatalogueRetailDestinationsResult.Failed()
            : CatalogueRetailDestinationsResult.Found(destinations);
    }

    public async Task<CatalogueRetailDestinationResult> AddRetailDestinationAsync(
        Guid submissionId,
        AddCatalogueSubmissionRetailDestinationRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.PostAsJsonAsync(
            $"api/v1/catalogue-submissions/{submissionId}/retail-destinations",
            request,
            cancellationToken);

        if (response.StatusCode is
            HttpStatusCode.Unauthorized or
            HttpStatusCode.Forbidden)
        {
            return CatalogueRetailDestinationResult.AccessDenied();
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var problem =
                await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(
                    cancellationToken);

            IReadOnlyDictionary<string, string[]> errors =
                problem?.Errors is not null
                    ? new Dictionary<string, string[]>(problem.Errors)
                    : new Dictionary<string, string[]>
                    {
                        ["retail"] =
                        [
                            "The retail destination could not be validated."
                        ]
                    };

            return CatalogueRetailDestinationResult.Invalid(errors);
        }

        if (!response.IsSuccessStatusCode)
            return CatalogueRetailDestinationResult.Failed();

        var receipt =
            await response.Content.ReadFromJsonAsync<
                CatalogueSubmissionRetailDestinationReceipt>(
                cancellationToken);

        return receipt is null
            ? CatalogueRetailDestinationResult.Failed()
            : CatalogueRetailDestinationResult.Added(receipt);
    }

    public async Task<CatalogueEntryOptionsResult> GetEntryOptionsAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await client.GetAsync(
            "api/v1/products/catalogue-entry-options",
            cancellationToken);

        if (response.StatusCode is
            HttpStatusCode.Unauthorized or
            HttpStatusCode.Forbidden)
        {
            return CatalogueEntryOptionsResult.AccessDenied();
        }

        if (!response.IsSuccessStatusCode)
            return CatalogueEntryOptionsResult.Failed();

        var options =
            await response.Content.ReadFromJsonAsync<CatalogueEntryOptions>(
                cancellationToken);

        return options is null
            ? CatalogueEntryOptionsResult.Failed()
            : CatalogueEntryOptionsResult.Found(options);
    }

    public async Task<CatalogueCreationResult> CreateAsync(
        CatalogueProductCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.PostAsJsonAsync(
            "api/v1/products",
            request,
            cancellationToken);

        if (response.StatusCode is
            HttpStatusCode.Unauthorized or
            HttpStatusCode.Forbidden)
        {
            return CatalogueCreationResult.AccessDenied();
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var problem =
                await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(
                    cancellationToken);

            IReadOnlyDictionary<string, string[]> errors =
                problem?.Errors is not null
                    ? new Dictionary<string, string[]>(problem.Errors)
                    : new Dictionary<string, string[]>
                    {
                        ["catalogue"] =
                        [
                            "The product could not be validated."
                        ]
                    };

            return CatalogueCreationResult.Invalid(errors);
        }

        if (!response.IsSuccessStatusCode)
            return CatalogueCreationResult.Failed();

        var receipt =
            await response.Content.ReadFromJsonAsync<CanonicalProductReceipt>(
                cancellationToken);

        return receipt is null
            ? CatalogueCreationResult.Failed()
            : CatalogueCreationResult.Created(receipt);
    }

    public async Task<CatalogueSubmissionVerificationWorkspaceResult>
        GetVerificationWorkspaceAsync(
            Guid submissionId,
            CancellationToken cancellationToken = default)
    {
        using var response = await client.GetAsync(
            $"api/v1/catalogue-submissions/{submissionId}/verification-workspace",
            cancellationToken);

        if (response.StatusCode is
            HttpStatusCode.Unauthorized or
            HttpStatusCode.Forbidden)
        {
            return CatalogueSubmissionVerificationWorkspaceResult.AccessDenied();
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
            return CatalogueSubmissionVerificationWorkspaceResult.NotFound();

        if (!response.IsSuccessStatusCode)
            return CatalogueSubmissionVerificationWorkspaceResult.Failed();

        var workspace =
            await response.Content.ReadFromJsonAsync<
                CatalogueSubmissionVerificationWorkspace>(
                cancellationToken);

        return workspace is null
            ? CatalogueSubmissionVerificationWorkspaceResult.Failed()
            : CatalogueSubmissionVerificationWorkspaceResult.Found(workspace);
    }

    public async Task<CatalogueSubmissionResult> BeginVerificationAsync(
        Guid submissionId,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.PostAsync(
            $"api/v1/catalogue-submissions/{submissionId}/begin-verification",
            content: null,
            cancellationToken);

        return await ReadSubmissionResponseAsync(
            response,
            cancellationToken);
    }

    public async Task<CatalogueSubmissionVerificationResult>
        AddVerificationAsync(
            Guid submissionId,
            AddCatalogueSubmissionVerificationRequest request,
            CancellationToken cancellationToken = default)
    {
        using var response = await client.PostAsJsonAsync(
            $"api/v1/catalogue-submissions/{submissionId}/verifications",
            request,
            cancellationToken);

        if (response.StatusCode is
            HttpStatusCode.Unauthorized or
            HttpStatusCode.Forbidden)
        {
            return CatalogueSubmissionVerificationResult.AccessDenied();
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var problem =
                await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(
                    cancellationToken);

            IReadOnlyDictionary<string, string[]> errors =
                problem?.Errors is not null
                    ? new Dictionary<string, string[]>(problem.Errors)
                    : new Dictionary<string, string[]>
                    {
                        ["verification"] =
                        [
                            "The verification could not be validated."
                        ]
                    };

            return CatalogueSubmissionVerificationResult.Invalid(errors);
        }

        if (!response.IsSuccessStatusCode)
            return CatalogueSubmissionVerificationResult.Failed();

        var receipt =
            await response.Content.ReadFromJsonAsync<
                CatalogueSubmissionVerificationReceipt>(
                cancellationToken);

        return receipt is null
            ? CatalogueSubmissionVerificationResult.Failed()
            : CatalogueSubmissionVerificationResult.Added(receipt);
    }

    public async Task<CatalogueSubmissionImagesResult> GetSubmissionImagesAsync(
        Guid submissionId,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.GetAsync(
            $"api/v1/catalogue-submissions/{submissionId}/images",
            cancellationToken);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            return CatalogueSubmissionImagesResult.AccessDenied();

        if (response.StatusCode == HttpStatusCode.NotFound)
            return CatalogueSubmissionImagesResult.NotFound();

        if (!response.IsSuccessStatusCode)
            return CatalogueSubmissionImagesResult.Failed();

        var images = await response.Content.ReadFromJsonAsync<IReadOnlyList<CatalogueSubmissionImageReceipt>>(
            cancellationToken);

        return images is null
            ? CatalogueSubmissionImagesResult.Failed()
            : CatalogueSubmissionImagesResult.Found(images);
    }

    public async Task<CatalogueSubmissionImageResult> AddSubmissionImageAsync(
        Guid submissionId,
        CatalogueSubmissionImageRole role,
        IBrowserFile file,
        CatalogueImageSourceType sourceType,
        string? sourceUrl,
        string? sourceNotes,
        CatalogueImagePermissionStatus permissionStatus,
        string? permissionEvidence,
        CancellationToken cancellationToken = default)
    {
        const long maxAllowedSize = 15 * 1024 * 1024;

        await using var fileStream = file.OpenReadStream(maxAllowedSize, cancellationToken);
        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(fileStream);

        streamContent.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);

        content.Add(streamContent, "file", Path.GetFileName(file.Name));
        content.Add(new StringContent(role.ToString()), "role");
        content.Add(new StringContent(sourceType.ToString()), "sourceType");
        content.Add(new StringContent(sourceUrl ?? string.Empty), "sourceUrl");
        content.Add(new StringContent(sourceNotes ?? string.Empty), "sourceNotes");
        content.Add(new StringContent(permissionStatus.ToString()), "permissionStatus");
        content.Add(new StringContent(permissionEvidence ?? string.Empty), "permissionEvidence");

        using var response = await client.PostAsync(
            $"api/v1/catalogue-submissions/{submissionId}/images",
            content,
            cancellationToken);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            return CatalogueSubmissionImageResult.AccessDenied();

        if (response.StatusCode == HttpStatusCode.NotFound)
            return CatalogueSubmissionImageResult.NotFound();

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(
                cancellationToken);

            var errors = problem?.Errors is not null
                ? new Dictionary<string, string[]>(problem.Errors)
                : new Dictionary<string, string[]>
                {
                    ["image"] = ["The image could not be validated."]
                };

            return CatalogueSubmissionImageResult.Invalid(errors);
        }

        if (!response.IsSuccessStatusCode)
            return CatalogueSubmissionImageResult.Failed();

        var receipt = await response.Content.ReadFromJsonAsync<CatalogueSubmissionImageReceipt>(
            cancellationToken);

        return receipt is null
            ? CatalogueSubmissionImageResult.Failed()
            : CatalogueSubmissionImageResult.Saved(receipt);
    }

    public async Task<CatalogueSubmissionImageResult> UpdateSubmissionImageMetadataAsync(
        Guid submissionId,
        Guid imageId,
        UpdateCatalogueSubmissionImageMetadata request,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.PutAsJsonAsync(
            $"api/v1/catalogue-submissions/{submissionId}/images/{imageId}",
            request,
            cancellationToken);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            return CatalogueSubmissionImageResult.AccessDenied();

        if (response.StatusCode == HttpStatusCode.NotFound)
            return CatalogueSubmissionImageResult.NotFound();

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(
                cancellationToken);

            var errors = problem?.Errors is not null
                ? new Dictionary<string, string[]>(problem.Errors)
                : new Dictionary<string, string[]>
                {
                    ["image"] = ["The image could not be validated."]
                };

            return CatalogueSubmissionImageResult.Invalid(errors);
        }

        if (!response.IsSuccessStatusCode)
            return CatalogueSubmissionImageResult.Failed();

        var receipt = await response.Content.ReadFromJsonAsync<CatalogueSubmissionImageReceipt>(
            cancellationToken);

        return receipt is null
            ? CatalogueSubmissionImageResult.Failed()
            : CatalogueSubmissionImageResult.Saved(receipt);
    }

    public async Task<bool> RemoveSubmissionImageAsync(
        Guid submissionId,
        Guid imageId,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.DeleteAsync(
            $"api/v1/catalogue-submissions/{submissionId}/images/{imageId}",
            cancellationToken);

        return response.IsSuccessStatusCode;
    }

    public async Task<string?> GetSubmissionImageDataUriAsync(
        Guid submissionId,
        Guid imageId,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.GetAsync(
            $"api/v1/catalogue-submissions/{submissionId}/images/{imageId}/content",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
            return null;

        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        return $"data:{contentType};base64,{Convert.ToBase64String(bytes)}";
    }

    private static async Task<CatalogueSubmissionResult>
        ReadSubmissionResponseAsync(
            HttpResponseMessage response,
            CancellationToken cancellationToken)
    {
        if (response.StatusCode is
            HttpStatusCode.Unauthorized or
            HttpStatusCode.Forbidden)
        {
            return CatalogueSubmissionResult.AccessDenied();
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var problem =
                await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(
                    cancellationToken);

            IReadOnlyDictionary<string, string[]> errors =
                problem?.Errors is not null
                    ? new Dictionary<string, string[]>(problem.Errors)
                    : new Dictionary<string, string[]>
                    {
                        ["submission"] =
                        [
                            "The submission could not be validated."
                        ]
                    };

            return CatalogueSubmissionResult.Invalid(errors);
        }

        if (!response.IsSuccessStatusCode)
            return CatalogueSubmissionResult.Failed();

        var receipt =
            await response.Content.ReadFromJsonAsync<CatalogueSubmissionReceipt>(
                cancellationToken);

        return receipt is null
            ? CatalogueSubmissionResult.Failed()
            : CatalogueSubmissionResult.Saved(receipt);
    }
}

public sealed record CatalogueProductCreateRequest(
    Guid ManufacturerId,
    Guid? BrandId,
    string ProductName,
    string ProductSlug,
    ProductType ProductType,
    ProductStatus Status,
    string VariantName,
    BackingType BackingType,
    string ManufacturerSize,
    int? WaistMinimumCm,
    int? WaistMaximumCm,
    int QuantityPerPack,
    PackagingType PackagingType,
    string Gtin,
    string SourceSummary,
    IReadOnlyList<string> SourceReferences,
    string EditorialRationale);

public sealed record CreateCatalogueSubmissionRequest(
    CatalogueSubmissionSource Source,
    string ProposedManufacturerName,
    string? ProposedBrandName,
    string ProposedProductName,
    string? ProposedVariantName,
    string? Notes);

public sealed record UpdateCatalogueSubmissionIdentityRequest(
    string? ProposedGtin,
    string? ProposedSku,
    string? IdentitySourceUrl);

public sealed record ResolveCatalogueSubmissionEntitiesRequest(
    Guid? ManufacturerId,
    string? NewManufacturerName,
    Guid? BrandId,
    string? NewBrandName);

public sealed record ReviewCatalogueSubmissionRequest(
    EditorialOutcome Outcome,
    string? Rationale);

public sealed record UpdateCatalogueSubmissionDescriptionVisibilityRequest(
    CatalogueContentVisibility Visibility);

public sealed record UpdateCatalogueSubmissionSpecificationsRequest(
    ProductType? ProposedProductType,
    PackagingType? ProposedPackagingType,
    string? ProposedProductFamily,
    string? ProposedDescription,
    CatalogueContentVisibility ProposedDescriptionVisibility,
    ProductStatus? ProposedProductStatus,
    string? ProposedOfficialWebsiteUrl,
    string? SharedPrintDesign,
    string? SharedPrimaryColour,
    bool? SharedWetnessIndicator,
    bool? SharedStandingLeakGuards,
    WaistbandStyle? SharedWaistbandStyle,
    FragranceType? SharedFragrance,
    bool? SharedLatexFree,
    string? SharedDesignedFor,
    int? SharedFastenerCount,
    string? SharedConstructionNotes);

public sealed record UpdateCatalogueSubmissionVariantOverrideRequest(
    CatalogueVariantOverrideAttribute Attribute,
    string? Value);

public sealed record AddCatalogueSubmissionVerificationRequest(
    CatalogueVerificationArea Area,
    CatalogueVerificationStatus Status,
    string Scope,
    string Source,
    string? SourceUrl,
    string? Notes,
    string? PermissionTerms);

public sealed record CatalogueSubmissionResult(
    CatalogueSubmissionResultStatus Status,
    CatalogueSubmissionReceipt? Receipt = null,
    IReadOnlyDictionary<string, string[]>? Errors = null,
    string? Message = null)
{
    public static CatalogueSubmissionResult Saved(
        CatalogueSubmissionReceipt receipt) =>
        new(
            CatalogueSubmissionResultStatus.Saved,
            Receipt: receipt);

    public static CatalogueSubmissionResult Deleted() =>
        new(
            CatalogueSubmissionResultStatus.Deleted);

    public static CatalogueSubmissionResult Invalid(
        IReadOnlyDictionary<string, string[]> errors) =>
        new(
            CatalogueSubmissionResultStatus.Invalid,
            Errors: errors);

    public static CatalogueSubmissionResult AccessDenied() =>
        new(
            CatalogueSubmissionResultStatus.AccessDenied,
            Message:
                "You need Moderator editorial authority to manage the catalogue.");

    public static CatalogueSubmissionResult Failed() =>
        new(
            CatalogueSubmissionResultStatus.Failed,
            Message:
                "The catalogue submission could not be saved just now. Please try again.");
}

public enum CatalogueSubmissionResultStatus
{
    Saved,
    Deleted,
    Invalid,
    AccessDenied,
    Failed
}

public sealed record CatalogueSubmissionReviewResult(
    CatalogueSubmissionReviewResultStatus Status,
    CatalogueSubmissionEditorialDecisionReceipt? Receipt = null,
    IReadOnlyDictionary<string, string[]>? Errors = null,
    string? Message = null)
{
    public static CatalogueSubmissionReviewResult Decided(
        CatalogueSubmissionEditorialDecisionReceipt receipt) =>
        new(
            CatalogueSubmissionReviewResultStatus.Decided,
            Receipt: receipt);

    public static CatalogueSubmissionReviewResult Invalid(
        IReadOnlyDictionary<string, string[]> errors) =>
        new(
            CatalogueSubmissionReviewResultStatus.Invalid,
            Errors: errors);

    public static CatalogueSubmissionReviewResult AccessDenied() =>
        new(
            CatalogueSubmissionReviewResultStatus.AccessDenied,
            Message:
                "You need Moderator editorial authority to review catalogue products.");

    public static CatalogueSubmissionReviewResult Failed() =>
        new(
            CatalogueSubmissionReviewResultStatus.Failed,
            Message:
                "The catalogue submission could not be reviewed just now. Please try again.");
}

public enum CatalogueSubmissionReviewResultStatus
{
    Decided,
    Invalid,
    AccessDenied,
    Failed
}

public sealed record CataloguePublicationResult(
    CataloguePublicationResultStatus Status,
    CataloguePublicationReceipt? Receipt = null,
    IReadOnlyDictionary<string, string[]>? Errors = null,
    string? Message = null)
{
    public static CataloguePublicationResult Published(
        CataloguePublicationReceipt receipt) =>
        new(
            CataloguePublicationResultStatus.Published,
            Receipt: receipt);

    public static CataloguePublicationResult Invalid(
        IReadOnlyDictionary<string, string[]> errors) =>
        new(
            CataloguePublicationResultStatus.Invalid,
            Errors: errors);

    public static CataloguePublicationResult AccessDenied() =>
        new(
            CataloguePublicationResultStatus.AccessDenied,
            Message:
                "You need Moderator editorial authority to publish catalogue products.");

    public static CataloguePublicationResult Failed() =>
        new(
            CataloguePublicationResultStatus.Failed,
            Message:
                "The catalogue submission could not be published just now. Please try again.");
}

public enum CataloguePublicationResultStatus
{
    Published,
    Invalid,
    AccessDenied,
    Failed
}

public sealed record CatalogueEntryOptionsResult(
    CatalogueEntryOptionsStatus Status,
    CatalogueEntryOptions? Options = null,
    string? Message = null)
{
    public static CatalogueEntryOptionsResult Found(
        CatalogueEntryOptions options) =>
        new(
            CatalogueEntryOptionsStatus.Found,
            options);

    public static CatalogueEntryOptionsResult AccessDenied() =>
        new(
            CatalogueEntryOptionsStatus.AccessDenied,
            Message:
                "You need Moderator editorial authority to manage the catalogue.");

    public static CatalogueEntryOptionsResult Failed() =>
        new(
            CatalogueEntryOptionsStatus.Failed,
            Message:
                "The catalogue workspace is unavailable just now.");
}

public enum CatalogueEntryOptionsStatus
{
    Found,
    AccessDenied,
    Failed
}

public sealed record CatalogueCreationResult(
    CatalogueCreationStatus Status,
    CanonicalProductReceipt? Receipt = null,
    IReadOnlyDictionary<string, string[]>? Errors = null,
    string? Message = null)
{
    public static CatalogueCreationResult Created(
        CanonicalProductReceipt receipt) =>
        new(
            CatalogueCreationStatus.Created,
            Receipt: receipt);

    public static CatalogueCreationResult Invalid(
        IReadOnlyDictionary<string, string[]> errors) =>
        new(
            CatalogueCreationStatus.Invalid,
            Errors: errors);

    public static CatalogueCreationResult AccessDenied() =>
        new(
            CatalogueCreationStatus.AccessDenied,
            Message:
                "Your editorial authority could not be confirmed.");

    public static CatalogueCreationResult Failed() =>
        new(
            CatalogueCreationStatus.Failed,
            Message:
                "The product could not be saved just now. Please try again.");
}

public enum CatalogueCreationStatus
{
    Created,
    Invalid,
    AccessDenied,
    Failed
}

public sealed record ProductCatalogueSearchResult(
    ProductCatalogueSearchStatus Status,
    CatalogueProductSearch? Catalogue = null,
    string? Message = null)
{
    public static ProductCatalogueSearchResult Found(
        CatalogueProductSearch catalogue) =>
        new(
            ProductCatalogueSearchStatus.Found,
            catalogue);

    public static ProductCatalogueSearchResult Failed() =>
        new(
            ProductCatalogueSearchStatus.Failed,
            Message:
                "We couldn't load the catalogue just now. Please try again.");
}

public enum ProductCatalogueSearchStatus
{
    Found,
    Failed
}

public sealed record ProductCatalogueDetailResult(
    ProductCatalogueDetailStatus Status,
    CatalogueProductDetails? Product = null,
    string? Message = null)
{
    public static ProductCatalogueDetailResult Found(
        CatalogueProductDetails product) =>
        new(
            ProductCatalogueDetailStatus.Found,
            product);

    public static ProductCatalogueDetailResult NotFound() =>
        new(
            ProductCatalogueDetailStatus.NotFound,
            Message:
                "We couldn't find that product.");

    public static ProductCatalogueDetailResult Failed() =>
        new(
            ProductCatalogueDetailStatus.Failed,
            Message:
                "We couldn't load that product just now. Please try again.");
}

public enum ProductCatalogueDetailStatus
{
    Found,
    NotFound,
    Failed
}

public sealed record AddCatalogueSubmissionRetailDestinationRequest(
    Guid RetailerId,
    string ListingUrl,
    string? Notes);

public sealed record CatalogueRetailersResult(
    CatalogueRetailersStatus Status,
    IReadOnlyList<CatalogueRetailerOption>? Retailers = null,
    string? Message = null)
{
    public static CatalogueRetailersResult Found(
        IReadOnlyList<CatalogueRetailerOption> retailers) =>
        new(
            CatalogueRetailersStatus.Found,
            retailers);

    public static CatalogueRetailersResult AccessDenied() =>
        new(
            CatalogueRetailersStatus.AccessDenied,
            Message:
                "You need Moderator editorial authority to manage the catalogue.");

    public static CatalogueRetailersResult Failed() =>
        new(
            CatalogueRetailersStatus.Failed,
            Message:
                "The retailer list could not be loaded just now.");
}

public enum CatalogueRetailersStatus
{
    Found,
    AccessDenied,
    Failed
}

public sealed record CatalogueRetailDestinationsResult(
    CatalogueRetailDestinationsStatus Status,
    IReadOnlyList<CatalogueSubmissionRetailDestinationReceipt>? Destinations = null,
    string? Message = null)
{
    public static CatalogueRetailDestinationsResult Found(
        IReadOnlyList<CatalogueSubmissionRetailDestinationReceipt> destinations) =>
        new(
            CatalogueRetailDestinationsStatus.Found,
            destinations);

    public static CatalogueRetailDestinationsResult AccessDenied() =>
        new(
            CatalogueRetailDestinationsStatus.AccessDenied,
            Message:
                "You need Moderator editorial authority to manage the catalogue.");

    public static CatalogueRetailDestinationsResult Failed() =>
        new(
            CatalogueRetailDestinationsStatus.Failed,
            Message:
                "The retail destinations could not be loaded just now.");
}

public enum CatalogueRetailDestinationsStatus
{
    Found,
    AccessDenied,
    Failed
}

public sealed record CatalogueRetailDestinationResult(
    CatalogueRetailDestinationResultStatus Status,
    CatalogueSubmissionRetailDestinationReceipt? Receipt = null,
    IReadOnlyDictionary<string, string[]>? Errors = null,
    string? Message = null)
{
    public static CatalogueRetailDestinationResult Added(
        CatalogueSubmissionRetailDestinationReceipt receipt) =>
        new(
            CatalogueRetailDestinationResultStatus.Added,
            Receipt: receipt);

    public static CatalogueRetailDestinationResult Invalid(
        IReadOnlyDictionary<string, string[]> errors) =>
        new(
            CatalogueRetailDestinationResultStatus.Invalid,
            Errors: errors);

    public static CatalogueRetailDestinationResult AccessDenied() =>
        new(
            CatalogueRetailDestinationResultStatus.AccessDenied,
            Message:
                "You need Moderator editorial authority to manage the catalogue.");

    public static CatalogueRetailDestinationResult Failed() =>
        new(
            CatalogueRetailDestinationResultStatus.Failed,
            Message:
                "The retail destination could not be saved just now.");
}

public enum CatalogueRetailDestinationResultStatus
{
    Added,
    Invalid,
    AccessDenied,
    Failed
}

public sealed record CatalogueSubmissionVerificationWorkspaceResult(
    CatalogueSubmissionVerificationWorkspaceStatus Status,
    CatalogueSubmissionVerificationWorkspace? Workspace = null,
    string? Message = null)
{
    public static CatalogueSubmissionVerificationWorkspaceResult Found(
        CatalogueSubmissionVerificationWorkspace workspace) =>
        new(
            CatalogueSubmissionVerificationWorkspaceStatus.Found,
            workspace);

    public static CatalogueSubmissionVerificationWorkspaceResult NotFound() =>
        new(
            CatalogueSubmissionVerificationWorkspaceStatus.NotFound,
            Message:
                "We couldn't find that catalogue submission.");

    public static CatalogueSubmissionVerificationWorkspaceResult AccessDenied() =>
        new(
            CatalogueSubmissionVerificationWorkspaceStatus.AccessDenied,
            Message:
                "You need Moderator editorial authority to manage the catalogue.");

    public static CatalogueSubmissionVerificationWorkspaceResult Failed() =>
        new(
            CatalogueSubmissionVerificationWorkspaceStatus.Failed,
            Message:
                "The verification workspace could not be loaded just now.");
}

public enum CatalogueSubmissionVerificationWorkspaceStatus
{
    Found,
    NotFound,
    AccessDenied,
    Failed
}

public sealed record CatalogueSubmissionVerificationResult(
    CatalogueSubmissionVerificationResultStatus Status,
    CatalogueSubmissionVerificationReceipt? Receipt = null,
    IReadOnlyDictionary<string, string[]>? Errors = null,
    string? Message = null)
{
    public static CatalogueSubmissionVerificationResult Added(
        CatalogueSubmissionVerificationReceipt receipt) =>
        new(
            CatalogueSubmissionVerificationResultStatus.Added,
            Receipt: receipt);

    public static CatalogueSubmissionVerificationResult Invalid(
        IReadOnlyDictionary<string, string[]> errors) =>
        new(
            CatalogueSubmissionVerificationResultStatus.Invalid,
            Errors: errors);

    public static CatalogueSubmissionVerificationResult AccessDenied() =>
        new(
            CatalogueSubmissionVerificationResultStatus.AccessDenied,
            Message:
                "You need Moderator editorial authority to manage the catalogue.");

    public static CatalogueSubmissionVerificationResult Failed() =>
        new(
            CatalogueSubmissionVerificationResultStatus.Failed,
            Message:
                "The verification could not be saved just now.");
}

public enum CatalogueSubmissionVerificationResultStatus
{
    Added,
    Invalid,
    AccessDenied,
    Failed
}

public sealed record AddCatalogueSubmissionVariantRequest(
    string? Name);

public sealed record UpdateCatalogueSubmissionVariantRequest(
    string Name);

public sealed record CatalogueSubmissionVariantResult(
    CatalogueSubmissionVariantResultStatus Status,
    CatalogueSubmissionVariantReceipt? Variant = null,
    IReadOnlyDictionary<string, string[]>? Errors = null,
    string? Message = null)
{
    public static CatalogueSubmissionVariantResult Saved(
        CatalogueSubmissionVariantReceipt variant) =>
        new(CatalogueSubmissionVariantResultStatus.Saved, Variant: variant);

    public static CatalogueSubmissionVariantResult Removed() =>
        new(CatalogueSubmissionVariantResultStatus.Removed);

    public static CatalogueSubmissionVariantResult Invalid(
        IReadOnlyDictionary<string, string[]> errors) =>
        new(CatalogueSubmissionVariantResultStatus.Invalid, Errors: errors);

    public static CatalogueSubmissionVariantResult AccessDenied() =>
        new(
            CatalogueSubmissionVariantResultStatus.AccessDenied,
            Message: "You need Moderator editorial authority to manage the catalogue.");

    public static CatalogueSubmissionVariantResult Failed() =>
        new(
            CatalogueSubmissionVariantResultStatus.Failed,
            Message: "The product variant could not be saved just now.");
}

public enum CatalogueSubmissionVariantResultStatus
{
    Saved,
    Removed,
    Invalid,
    AccessDenied,
    Failed
}
public sealed record CatalogueSubmissionVariantOverrideResult(
    CatalogueSubmissionVariantOverrideResultStatus Status,
    CatalogueSubmissionVariantOverrideReceipt? Override = null,
    IReadOnlyDictionary<string, string[]>? Errors = null,
    string? Message = null)
{
    public static CatalogueSubmissionVariantOverrideResult Found(CatalogueSubmissionVariantOverrideReceipt? value) =>
        new(CatalogueSubmissionVariantOverrideResultStatus.Found, value);
    public static CatalogueSubmissionVariantOverrideResult Saved(CatalogueSubmissionVariantOverrideReceipt value) =>
        new(CatalogueSubmissionVariantOverrideResultStatus.Saved, value);
    public static CatalogueSubmissionVariantOverrideResult Invalid(IReadOnlyDictionary<string, string[]> errors) =>
        new(CatalogueSubmissionVariantOverrideResultStatus.Invalid, Errors: errors);
    public static CatalogueSubmissionVariantOverrideResult AccessDenied() =>
        new(CatalogueSubmissionVariantOverrideResultStatus.AccessDenied, Message: "You need Moderator editorial authority to manage the catalogue.");
    public static CatalogueSubmissionVariantOverrideResult Failed(string message = "The variant difference could not be loaded or saved just now.") =>
        new(CatalogueSubmissionVariantOverrideResultStatus.Failed, Message: message);
}

public enum CatalogueSubmissionVariantOverrideResultStatus
{
    Found,
    Saved,
    Deleted,
    Invalid,
    AccessDenied,
    Failed
}


public enum CatalogueSubmissionQueueResultStatus
{
    Found,
    AccessDenied,
    Failed
}

public sealed record CatalogueSubmissionQueueResult(
    CatalogueSubmissionQueueResultStatus Status,
    IReadOnlyList<CatalogueSubmissionQueueItem>? Submissions = null,
    string? Message = null)
{
    public static CatalogueSubmissionQueueResult Found(
        IReadOnlyList<CatalogueSubmissionQueueItem> submissions) =>
        new(CatalogueSubmissionQueueResultStatus.Found, submissions);

    public static CatalogueSubmissionQueueResult AccessDenied() =>
        new(CatalogueSubmissionQueueResultStatus.AccessDenied);

    public static CatalogueSubmissionQueueResult Failed(
        string? message = null) =>
        new(CatalogueSubmissionQueueResultStatus.Failed, Message: message);
}

public sealed record CatalogueSubmissionImportClientResult(CatalogueSubmissionImportClientStatus Status, CatalogueSubmissionImportResult? Result = null, string? Message = null)
{
    public static CatalogueSubmissionImportClientResult Succeeded(CatalogueSubmissionImportResult result) => new(CatalogueSubmissionImportClientStatus.Succeeded, result);
    public static CatalogueSubmissionImportClientResult AccessDenied() => new(CatalogueSubmissionImportClientStatus.AccessDenied);
    public static CatalogueSubmissionImportClientResult Failed(string? message = null) => new(CatalogueSubmissionImportClientStatus.Failed, Message: message);
}

public enum CatalogueSubmissionImportClientStatus { Succeeded, AccessDenied, Failed }

internal sealed record ImportErrorResponse(string? Message);

public enum CatalogueProductImageAddStatus { Saved, AccessDenied, NotFound, Invalid, Failed }

public sealed record CatalogueProductImageAddResult(
    CatalogueProductImageAddStatus Status,
    CatalogueModeratorProductImage? Image = null,
    IReadOnlyDictionary<string, string[]>? Errors = null)
{
    public static CatalogueProductImageAddResult Saved(CatalogueModeratorProductImage image) => new(CatalogueProductImageAddStatus.Saved, image);
    public static CatalogueProductImageAddResult AccessDenied() => new(CatalogueProductImageAddStatus.AccessDenied);
    public static CatalogueProductImageAddResult NotFound() => new(CatalogueProductImageAddStatus.NotFound);
    public static CatalogueProductImageAddResult Invalid(IReadOnlyDictionary<string, string[]> errors) => new(CatalogueProductImageAddStatus.Invalid, Errors: errors);
    public static CatalogueProductImageAddResult Failed() => new(CatalogueProductImageAddStatus.Failed);
}

public enum CatalogueProductManagementStatus { Found, AccessDenied, NotFound, Failed }

public sealed record CatalogueProductManagementResult(
    CatalogueProductManagementStatus Status,
    CatalogueProductManagementDetails? Product = null)
{
    public static CatalogueProductManagementResult Found(CatalogueProductManagementDetails product) => new(CatalogueProductManagementStatus.Found, product);
    public static CatalogueProductManagementResult AccessDenied() => new(CatalogueProductManagementStatus.AccessDenied);
    public static CatalogueProductManagementResult NotFound() => new(CatalogueProductManagementStatus.NotFound);
    public static CatalogueProductManagementResult Failed() => new(CatalogueProductManagementStatus.Failed);
}

public enum CatalogueProductManagementUpdateStatus { Saved, AccessDenied, NotFound, Invalid, Failed }

public sealed record CatalogueProductManagementUpdateResult(
    CatalogueProductManagementUpdateStatus Status,
    IReadOnlyDictionary<string, string[]>? Errors = null)
{
    public static CatalogueProductManagementUpdateResult Saved() => new(CatalogueProductManagementUpdateStatus.Saved);
    public static CatalogueProductManagementUpdateResult AccessDenied() => new(CatalogueProductManagementUpdateStatus.AccessDenied);
    public static CatalogueProductManagementUpdateResult NotFound() => new(CatalogueProductManagementUpdateStatus.NotFound);
    public static CatalogueProductManagementUpdateResult Invalid(IReadOnlyDictionary<string, string[]> errors) => new(CatalogueProductManagementUpdateStatus.Invalid, errors);
    public static CatalogueProductManagementUpdateResult Failed() => new(CatalogueProductManagementUpdateStatus.Failed);
}

public enum CatalogueModeratorProductPreviewStatus { Found, AccessDenied, NotFound, Failed }

public sealed record CatalogueModeratorProductPreviewResult(CatalogueModeratorProductPreviewStatus Status, CatalogueModeratorProductDetails? Product = null)
{
    public static CatalogueModeratorProductPreviewResult Found(CatalogueModeratorProductDetails product) => new(CatalogueModeratorProductPreviewStatus.Found, product);
    public static CatalogueModeratorProductPreviewResult AccessDenied() => new(CatalogueModeratorProductPreviewStatus.AccessDenied);
    public static CatalogueModeratorProductPreviewResult NotFound() => new(CatalogueModeratorProductPreviewStatus.NotFound);
    public static CatalogueModeratorProductPreviewResult Failed() => new(CatalogueModeratorProductPreviewStatus.Failed);
}
