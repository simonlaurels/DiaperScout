using System.Net;
using System.Net.Http.Json;
using DiaperScout.Application;
using DiaperScout.Domain;
using Microsoft.AspNetCore.Mvc;

namespace DiaperScout.Web.Services;

public sealed class ProductCatalogueClient(HttpClient client)
{
    public async Task<ProductCatalogueSearchResult> SearchAsync(
        string? query,
        CatalogueProductFilters? filters = null,
        string sort = "relevance",
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

    private static void AddParameter(
        List<string> parameters,
        string name,
        IEnumerable<string> values)
    {
        var value = string.Join(',', values);

        if (!string.IsNullOrWhiteSpace(value))
            parameters.Add($"{name}={Uri.EscapeDataString(value)}");
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
        if (!response.IsSuccessStatusCode)
            return CatalogueSubmissionVariantOverrideResult.Failed();

        var value = await response.Content.ReadFromJsonAsync<CatalogueSubmissionVariantOverrideReceipt>(cancellationToken);
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
            return CatalogueSubmissionVariantOverrideResult.Failed();

        var value = await response.Content.ReadFromJsonAsync<CatalogueSubmissionVariantOverrideReceipt>(cancellationToken);
        return value is null ? CatalogueSubmissionVariantOverrideResult.Failed() : CatalogueSubmissionVariantOverrideResult.Saved(value);
    }

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
    string ProposedVariantName,
    string? Notes);

public sealed record UpdateCatalogueSubmissionIdentityRequest(
    string? ProposedGtin,
    string? ProposedSku,
    string? IdentitySourceUrl);

public sealed record UpdateCatalogueSubmissionSpecificationsRequest(
    ProductType? ProposedProductType,
    string? ProposedManufacturerSize,
    int? ProposedWaistMinimumCm,
    int? ProposedWaistMaximumCm,
    BackingType? ProposedBackingType,
    FastenerType? ProposedFastenerType,
    WaistbandStyle? ProposedWaistbandStyle,
    FragranceType? ProposedFragranceType,
    int? ProposedQuantityPerPack,
    PackagingType? ProposedPackagingType,
    string? ProposedProductFamily,
    string? ProposedDescription,
    ProductStatus? ProposedProductStatus,
    string? ProposedOfficialWebsiteUrl,
    string? SharedPrintDesign,
    string? SharedPrimaryColour,
    string? SharedSecondaryColours,
    bool? SharedWetnessIndicator,
    bool? SharedStandingLeakGuards,
    bool? SharedInnerLeakGuards,
    bool? SharedElasticWaistbandFront,
    bool? SharedElasticWaistbandRear,
    bool? SharedLatexFree,
    bool? SharedChlorineFree,
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
    string Name);

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
    public static CatalogueSubmissionVariantOverrideResult Failed() =>
        new(CatalogueSubmissionVariantOverrideResultStatus.Failed, Message: "The variant difference could not be loaded or saved just now.");
}

public enum CatalogueSubmissionVariantOverrideResultStatus
{
    Found,
    Saved,
    Invalid,
    AccessDenied,
    Failed
}

