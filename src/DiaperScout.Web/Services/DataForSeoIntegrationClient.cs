using System.Net;
using System.Net.Http.Json;
using DiaperScout.Application;

namespace DiaperScout.Web.Services;

public sealed class DataForSeoIntegrationClient(HttpClient client)
{
    public async Task<DataForSeoIntegrationResult> GetAsync(CancellationToken cancellationToken = default)
    {
        using var response = await client.GetAsync(
            "api/v1/admin/integrations/dataforseo",
            cancellationToken);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            return DataForSeoIntegrationResult.Denied();

        if (!response.IsSuccessStatusCode)
            return DataForSeoIntegrationResult.Failed();

        var settings = await response.Content.ReadFromJsonAsync<DataForSeoIntegrationSettings>(cancellationToken);
        return settings is null
            ? DataForSeoIntegrationResult.Failed()
            : DataForSeoIntegrationResult.Found(settings);
    }

    public async Task<DataForSeoIntegrationResult> SaveAsync(
        UpdateDataForSeoIntegrationSettings request,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.PutAsJsonAsync(
            "api/v1/admin/integrations/dataforseo",
            request,
            cancellationToken);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            return DataForSeoIntegrationResult.Denied();

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ValidationProblemDetails>(cancellationToken);
            return DataForSeoIntegrationResult.Invalid(
                problem?.Errors is null
                    ? new Dictionary<string, string[]> { ["integration"] = ["The integration settings are invalid."] }
                    : new Dictionary<string, string[]>(problem.Errors));
        }

        if (!response.IsSuccessStatusCode)
            return DataForSeoIntegrationResult.Failed();

        var settings = await response.Content.ReadFromJsonAsync<DataForSeoIntegrationSettings>(cancellationToken);
        return settings is null
            ? DataForSeoIntegrationResult.Failed()
            : DataForSeoIntegrationResult.Saved(settings);
    }

    public async Task<DataForSeoConnectionTestResult> TestConnectionAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await client.PostAsync(
            "api/v1/admin/integrations/dataforseo/test",
            null,
            cancellationToken);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            return new(false, "You need moderator or administrator access to test integrations.", null);

        if (!response.IsSuccessStatusCode)
            return new(false, "The DataForSEO connection test could not be completed.", null);

        return await response.Content.ReadFromJsonAsync<DataForSeoConnectionTestResult>(cancellationToken)
            ?? new(false, "The DataForSEO connection test returned no result.", null);
    }
}

public sealed record DataForSeoIntegrationResult(
    bool Success,
    bool AccessDenied,
    DataForSeoIntegrationSettings? Settings,
    IReadOnlyDictionary<string, string[]> Errors)
{
    public static DataForSeoIntegrationResult Found(DataForSeoIntegrationSettings settings) =>
        new(true, false, settings, new Dictionary<string, string[]>());

    public static DataForSeoIntegrationResult Saved(DataForSeoIntegrationSettings settings) =>
        Found(settings);

    public static DataForSeoIntegrationResult Invalid(IReadOnlyDictionary<string, string[]> errors) =>
        new(false, false, null, errors);

    public static DataForSeoIntegrationResult Denied() =>
        new(false, true, null, new Dictionary<string, string[]>());

    public static DataForSeoIntegrationResult Failed() =>
        new(false, false, null, new Dictionary<string, string[]>());
}
