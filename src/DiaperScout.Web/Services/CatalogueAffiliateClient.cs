using System.Net;
using System.Net.Http.Json;
using DiaperScout.Application;
using DiaperScout.Domain;
using Microsoft.AspNetCore.Mvc;

namespace DiaperScout.Web.Services;

public sealed class CatalogueAffiliateClient(HttpClient client)
{
    public async Task<AffiliateWorkspaceResult> GetWorkspaceAsync(
        Guid submissionId,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.GetAsync(
            $"api/v1/catalogue-submissions/{submissionId}/retail-workspace",
            cancellationToken);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            return AffiliateWorkspaceResult.AccessDenied();

        if (!response.IsSuccessStatusCode)
            return AffiliateWorkspaceResult.Failed();

        var workspace = await response.Content.ReadFromJsonAsync<CatalogueSubmissionRetailWorkspace>(cancellationToken);
        return workspace is null
            ? AffiliateWorkspaceResult.Failed()
            : AffiliateWorkspaceResult.Found(workspace);
    }

    public async Task<AffiliateSaveResult> AddAsync(
        Guid submissionId,
        Guid retailDestinationId,
        AffiliateRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.PostAsJsonAsync(
            $"api/v1/catalogue-submissions/{submissionId}/retail-destinations/{retailDestinationId}/affiliate",
            request,
            cancellationToken);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            return AffiliateSaveResult.AccessDenied();

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(cancellationToken);
            IReadOnlyDictionary<string, string[]> errors = problem?.Errors is not null
                ? new Dictionary<string, string[]>(problem.Errors)
                : new Dictionary<string, string[]> { ["affiliate"] = ["The affiliate record could not be saved."] };
            return AffiliateSaveResult.Invalid(errors);
        }

        if (!response.IsSuccessStatusCode)
            return AffiliateSaveResult.Failed();

        var receipt = await response.Content.ReadFromJsonAsync<CatalogueSubmissionRetailAffiliateReceipt>(cancellationToken);
        return receipt is null
            ? AffiliateSaveResult.Failed()
            : AffiliateSaveResult.Saved(receipt);
    }
}

public sealed record AffiliateRequest(
    AffiliateProgrammeStatus Status,
    string? Network,
    string? TrackingConfiguration,
    string? DeepLinkMechanism,
    string? TermsUrl,
    string? ApplicationReference,
    string? Notes);

public sealed record AffiliateWorkspaceResult(
    AffiliateWorkspaceStatus Status,
    CatalogueSubmissionRetailWorkspace? Workspace = null,
    string? Message = null)
{
    public static AffiliateWorkspaceResult Found(CatalogueSubmissionRetailWorkspace workspace) =>
        new(AffiliateWorkspaceStatus.Found, workspace);
    public static AffiliateWorkspaceResult AccessDenied() =>
        new(AffiliateWorkspaceStatus.AccessDenied, Message: "You need Moderator editorial authority to manage the catalogue.");
    public static AffiliateWorkspaceResult Failed() =>
        new(AffiliateWorkspaceStatus.Failed, Message: "The retail workspace is unavailable just now. Please try again.");
}

public enum AffiliateWorkspaceStatus { Found, AccessDenied, Failed }

public sealed record AffiliateSaveResult(
    AffiliateSaveStatus Status,
    CatalogueSubmissionRetailAffiliateReceipt? Receipt = null,
    IReadOnlyDictionary<string, string[]>? Errors = null,
    string? Message = null)
{
    public static AffiliateSaveResult Saved(CatalogueSubmissionRetailAffiliateReceipt receipt) =>
        new(AffiliateSaveStatus.Saved, Receipt: receipt);
    public static AffiliateSaveResult Invalid(IReadOnlyDictionary<string, string[]> errors) =>
        new(AffiliateSaveStatus.Invalid, Errors: errors);
    public static AffiliateSaveResult AccessDenied() =>
        new(AffiliateSaveStatus.AccessDenied, Message: "You need Moderator editorial authority to manage the catalogue.");
    public static AffiliateSaveResult Failed() =>
        new(AffiliateSaveStatus.Failed, Message: "The affiliate record could not be saved just now. Please try again.");
}

public enum AffiliateSaveStatus { Saved, Invalid, AccessDenied, Failed }
