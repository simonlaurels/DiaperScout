using System.Net;
using System.Net.Http.Json;
using DiaperScout.Application;
using DiaperScout.Domain;

namespace DiaperScout.Web.Services;

public sealed class RetailerManagementClient(HttpClient client)
{
    public async Task<RetailerManagementResult> GetAsync(
        string? query = null,
        RetailerStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var parameters = new List<string>();

        if (!string.IsNullOrWhiteSpace(query))
            parameters.Add($"q={Uri.EscapeDataString(query.Trim())}");

        if (status.HasValue)
            parameters.Add($"status={Uri.EscapeDataString(status.Value.ToString())}");

        var path = "api/v1/retailer-management";
        if (parameters.Count > 0)
            path += "?" + string.Join("&", parameters);

        using var response = await client.GetAsync(path, cancellationToken);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            return RetailerManagementResult.AccessDenied();

        if (!response.IsSuccessStatusCode)
            return RetailerManagementResult.Failed();

        var retailers = await response.Content.ReadFromJsonAsync<IReadOnlyList<RetailerManagementItem>>(cancellationToken);
        return retailers is null
            ? RetailerManagementResult.Failed()
            : RetailerManagementResult.Found(retailers);
    }

    public async Task<RetailerMutationResult> CreateAsync(
        CreateRetailerManagement request,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.PostAsJsonAsync(
            "api/v1/retailer-management",
            request,
            cancellationToken);

        return await ReadMutationAsync(response, cancellationToken);
    }

    public async Task<RetailerMutationResult> UpdateIdentityAsync(
        Guid retailerId,
        UpdateRetailerIdentity request,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.PutAsJsonAsync(
            $"api/v1/retailer-management/{retailerId}/identity",
            request,
            cancellationToken);

        return await ReadMutationAsync(response, cancellationToken);
    }


    public async Task<RetailerVerificationResult> VerifyIdentityAsync(
        Guid retailerId,
        RetailerIdentityVerificationRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.PostAsJsonAsync(
            $"api/v1/retailer-management/{retailerId}/identity-verification",
            request,
            cancellationToken);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            return RetailerVerificationResult.AccessDenied();

        if (response.StatusCode == HttpStatusCode.NotFound)
            return RetailerVerificationResult.NotFound();

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ValidationProblemDetails>(cancellationToken);
            return RetailerVerificationResult.Invalid(
                problem?.Errors is null
                    ? new Dictionary<string, string[]> { ["identity"] = ["The identity could not be verified."] }
                    : new Dictionary<string, string[]>(problem.Errors));
        }

        if (!response.IsSuccessStatusCode)
            return RetailerVerificationResult.Failed();

        var verification = await response.Content.ReadFromJsonAsync<RetailerIdentityVerificationItem>(cancellationToken);
        return verification is null
            ? RetailerVerificationResult.Failed()
            : RetailerVerificationResult.Saved(verification);
    }

    public async Task<RetailerAffiliateProgrammeResult> GetAffiliateProgrammesAsync(
        Guid retailerId,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.GetAsync(
            $"api/v1/retailer-management/{retailerId}/affiliate-programmes",
            cancellationToken);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            return RetailerAffiliateProgrammeResult.AccessDenied();

        if (response.StatusCode == HttpStatusCode.NotFound)
            return RetailerAffiliateProgrammeResult.NotFound();

        if (!response.IsSuccessStatusCode)
            return RetailerAffiliateProgrammeResult.Failed();

        var programmes = await response.Content.ReadFromJsonAsync<IReadOnlyList<RetailerAffiliateProgrammeItem>>(cancellationToken);
        return programmes is null
            ? RetailerAffiliateProgrammeResult.Failed()
            : RetailerAffiliateProgrammeResult.Found(programmes);
    }

    public async Task<RetailerAffiliateProgrammeResult> UpdateAffiliateProgrammeStatusAsync(
        Guid retailerId,
        Guid programmeId,
        RetailerAffiliateProgrammeStatusUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.PutAsJsonAsync(
            $"api/v1/retailer-management/{retailerId}/affiliate-programmes/{programmeId}/status",
            request,
            cancellationToken);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            return RetailerAffiliateProgrammeResult.AccessDenied();

        if (response.StatusCode == HttpStatusCode.NotFound)
            return RetailerAffiliateProgrammeResult.NotFound();

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ValidationProblemDetails>(cancellationToken);
            return RetailerAffiliateProgrammeResult.Invalid(
                problem?.Errors is null
                    ? new Dictionary<string, string[]> { ["status"] = ["The affiliate programme status could not be updated."] }
                    : new Dictionary<string, string[]>(problem.Errors));
        }

        if (!response.IsSuccessStatusCode)
            return RetailerAffiliateProgrammeResult.Failed();

        var programme = await response.Content.ReadFromJsonAsync<RetailerAffiliateProgrammeItem>(cancellationToken);
        return programme is null
            ? RetailerAffiliateProgrammeResult.Failed()
            : RetailerAffiliateProgrammeResult.StatusUpdated(programme);
    }

    public async Task<RetailerAffiliateProgrammeResult> SelectAffiliateProgrammeAsync(
        Guid retailerId,
        Guid programmeId,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.PostAsync(
            $"api/v1/retailer-management/{retailerId}/affiliate-programmes/{programmeId}/select",
            null,
            cancellationToken);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            return RetailerAffiliateProgrammeResult.AccessDenied();

        if (response.StatusCode == HttpStatusCode.NotFound)
            return RetailerAffiliateProgrammeResult.NotFound();

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ValidationProblemDetails>(cancellationToken);
            return RetailerAffiliateProgrammeResult.Invalid(
                problem?.Errors is null
                    ? new Dictionary<string, string[]> { ["affiliate"] = ["The affiliate programme could not be selected."] }
                    : new Dictionary<string, string[]>(problem.Errors));
        }

        if (!response.IsSuccessStatusCode)
            return RetailerAffiliateProgrammeResult.Failed();

        var programme = await response.Content.ReadFromJsonAsync<RetailerAffiliateProgrammeItem>(cancellationToken);
        return programme is null
            ? RetailerAffiliateProgrammeResult.Failed()
            : RetailerAffiliateProgrammeResult.Selected(programme);
    }

    private static async Task<RetailerMutationResult> ReadMutationAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            return RetailerMutationResult.AccessDenied();

        if (response.StatusCode == HttpStatusCode.NotFound)
            return RetailerMutationResult.NotFound();

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ValidationProblemDetails>(cancellationToken);
            return RetailerMutationResult.Invalid(
                problem?.Errors is null
                    ? new Dictionary<string, string[]> { ["retailer"] = ["The retailer could not be saved."] }
                    : new Dictionary<string, string[]>(problem.Errors));
        }

        if (!response.IsSuccessStatusCode)
            return RetailerMutationResult.Failed();

        var retailer = await response.Content.ReadFromJsonAsync<RetailerManagementItem>(cancellationToken);
        return retailer is null
            ? RetailerMutationResult.Failed()
            : RetailerMutationResult.Saved(retailer);
    }
}

public sealed record RetailerManagementResult(
    RetailerManagementStatus Status,
    IReadOnlyList<RetailerManagementItem>? Retailers = null,
    string? Message = null)
{
    public static RetailerManagementResult Found(IReadOnlyList<RetailerManagementItem> retailers) =>
        new(RetailerManagementStatus.Found, retailers);

    public static RetailerManagementResult AccessDenied() =>
        new(RetailerManagementStatus.AccessDenied, Message: "You need Moderator editorial authority to manage retailers.");

    public static RetailerManagementResult Failed() =>
        new(RetailerManagementStatus.Failed, Message: "The retailer management workspace is unavailable just now. Please try again.");
}

public enum RetailerManagementStatus
{
    Found,
    AccessDenied,
    Failed
}

public sealed record RetailerMutationResult(
    RetailerMutationStatus Status,
    RetailerManagementItem? Retailer = null,
    IReadOnlyDictionary<string, string[]>? Errors = null,
    string? Message = null)
{
    public static RetailerMutationResult Saved(RetailerManagementItem retailer) =>
        new(RetailerMutationStatus.Saved, retailer);

    public static RetailerMutationResult Invalid(IReadOnlyDictionary<string, string[]> errors) =>
        new(RetailerMutationStatus.Invalid, Errors: errors);

    public static RetailerMutationResult AccessDenied() =>
        new(RetailerMutationStatus.AccessDenied, Message: "You need Moderator editorial authority to manage retailers.");

    public static RetailerMutationResult NotFound() =>
        new(RetailerMutationStatus.NotFound, Message: "The retailer could not be found.");

    public static RetailerMutationResult Failed() =>
        new(RetailerMutationStatus.Failed, Message: "The retailer could not be saved just now. Please try again.");
}

public enum RetailerMutationStatus
{
    Saved,
    Invalid,
    AccessDenied,
    NotFound,
    Failed
}


public sealed record RetailerVerificationResult(
    RetailerVerificationStatus Status,
    RetailerIdentityVerificationItem? Verification = null,
    IReadOnlyDictionary<string, string[]>? Errors = null,
    string? Message = null)
{
    public static RetailerVerificationResult Saved(RetailerIdentityVerificationItem verification) =>
        new(RetailerVerificationStatus.Saved, verification);
    public static RetailerVerificationResult Invalid(IReadOnlyDictionary<string, string[]> errors) =>
        new(RetailerVerificationStatus.Invalid, Errors: errors);
    public static RetailerVerificationResult AccessDenied() =>
        new(RetailerVerificationStatus.AccessDenied, Message: "You need Moderator editorial authority to manage retailers.");
    public static RetailerVerificationResult NotFound() =>
        new(RetailerVerificationStatus.NotFound, Message: "The retailer could not be found.");
    public static RetailerVerificationResult Failed() =>
        new(RetailerVerificationStatus.Failed, Message: "The identity verification could not be completed just now.");
}

public enum RetailerVerificationStatus
{
    Saved,
    Invalid,
    AccessDenied,
    NotFound,
    Failed
}

public sealed record RetailerAffiliateProgrammeResult(
    RetailerAffiliateProgrammeResultStatus Status,
    IReadOnlyList<RetailerAffiliateProgrammeItem>? Programmes = null,
    RetailerAffiliateProgrammeItem? SelectedProgramme = null,
    IReadOnlyDictionary<string, string[]>? Errors = null,
    string? Message = null)
{
    public static RetailerAffiliateProgrammeResult Found(IReadOnlyList<RetailerAffiliateProgrammeItem> programmes) =>
        new(RetailerAffiliateProgrammeResultStatus.Found, Programmes: programmes);

    public static RetailerAffiliateProgrammeResult Selected(RetailerAffiliateProgrammeItem programme) =>
        new(RetailerAffiliateProgrammeResultStatus.Selected, SelectedProgramme: programme);

    public static RetailerAffiliateProgrammeResult StatusUpdated(RetailerAffiliateProgrammeItem programme) =>
        new(RetailerAffiliateProgrammeResultStatus.StatusUpdated, SelectedProgramme: programme);

    public static RetailerAffiliateProgrammeResult Invalid(IReadOnlyDictionary<string, string[]> errors) =>
        new(RetailerAffiliateProgrammeResultStatus.Invalid, Errors: errors);

    public static RetailerAffiliateProgrammeResult AccessDenied() =>
        new(RetailerAffiliateProgrammeResultStatus.AccessDenied, Message: "You need Moderator editorial authority to manage retailer affiliate programmes.");

    public static RetailerAffiliateProgrammeResult NotFound() =>
        new(RetailerAffiliateProgrammeResultStatus.NotFound, Message: "The retailer or affiliate programme could not be found.");

    public static RetailerAffiliateProgrammeResult Failed() =>
        new(RetailerAffiliateProgrammeResultStatus.Failed, Message: "The affiliate programme workspace is unavailable just now. Please try again.");
}

public enum RetailerAffiliateProgrammeResultStatus
{
    Found,
    Selected,
    StatusUpdated,
    Invalid,
    AccessDenied,
    NotFound,
    Failed
}

