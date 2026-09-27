using System.Net.Http.Json;
using DiaperScout.Application;

namespace DiaperScout.Web.Services;

public sealed class UserManagementClient(HttpClient client)
{
    public async Task<UserManagementItem[]> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        using var response = await client.GetAsync("api/v1/user-management/users", cancellationToken);
        if (!response.IsSuccessStatusCode) return [];
        return await response.Content.ReadFromJsonAsync<UserManagementItem[]>(cancellationToken) ?? [];
    }

    public async Task<RegistrationSettingsItem?> GetRegistrationSettingsAsync(CancellationToken cancellationToken = default)
    {
        using var response = await client.GetAsync("api/v1/user-management/registration", cancellationToken);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<RegistrationSettingsItem>(cancellationToken);
    }

    public async Task<RegistrationSettingsItem?> SetRegistrationEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        using var response = await client.PutAsJsonAsync("api/v1/user-management/registration", new SetRegistrationEnabledRequest(enabled), cancellationToken);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<RegistrationSettingsItem>(cancellationToken);
    }
}