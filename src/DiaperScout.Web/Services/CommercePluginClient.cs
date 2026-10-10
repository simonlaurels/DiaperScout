using System.Net;
using System.Net.Http.Json;
using DiaperScout.Application;

namespace DiaperScout.Web.Services;

public sealed class CommercePluginClient(HttpClient client)
{
    public Task<CommercePluginResponse<IReadOnlyList<CommercePluginStatus>>> GetAsync() =>
        SendAsync<IReadOnlyList<CommercePluginStatus>>(HttpMethod.Get, "api/v1/commerce-plugins/");
    public Task<CommercePluginResponse<CommercePluginStatus>> SetEnabledAsync(string id, bool enabled) =>
        SendAsync<CommercePluginStatus>(HttpMethod.Put, $"api/v1/commerce-plugins/{Uri.EscapeDataString(id)}/enabled", new CommercePluginEnabledRequest(enabled));
    private async Task<CommercePluginResponse<T>> SendAsync<T>(HttpMethod method, string path, object? body = null)
    {
        using var request = new HttpRequestMessage(method, path);
        if (body is not null) request.Content = JsonContent.Create(body);
        using var response = await client.SendAsync(request);
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            return new(default, "You need Moderator or Administrator access to manage commerce integrations.");
        if (response.StatusCode == HttpStatusCode.BadRequest)
            return new(default, "This integration is disabled by deployment configuration.");
        if (!response.IsSuccessStatusCode) return new(default, "We couldn’t update the integrations just now. Please try again.");
        return new(await response.Content.ReadFromJsonAsync<T>());
    }
}
public sealed record CommercePluginResponse<T>(T? Value, string? Error = null);
