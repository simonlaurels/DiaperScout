using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DiaperScout.Application;
using DiaperScout.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DiaperScout.Infrastructure;

public sealed record DataForSeoRuntimeSettings(
    bool Enabled,
    string? Login,
    string? Password,
    string LocationName,
    string LanguageCode,
    string SearchDomain);

public sealed class DataForSeoRuntimeSettingsStore
{
    private readonly object sync = new();
    private DataForSeoRuntimeSettings current;

    public DataForSeoRuntimeSettingsStore(IOptions<DataForSeoOptions> options)
    {
        current = new(
            options.Value.Enabled,
            options.Value.Login,
            options.Value.Password,
            options.Value.LocationName,
            options.Value.LanguageCode,
            options.Value.SearchDomain);
    }

    public DataForSeoRuntimeSettings Current
    {
        get { lock (sync) return current; }
    }

    public void Update(DataForSeoRuntimeSettings settings)
    {
        lock (sync) current = settings;
    }
}

public sealed class DataForSeoIntegrationStore(
    DiaperScoutDbContext db,
    IDataProtectionProvider dataProtectionProvider,
    DataForSeoRuntimeSettingsStore runtime)
{
    private const string ProviderKey = "DataForSEO";
    private readonly IDataProtector protector =
        dataProtectionProvider.CreateProtector("DiaperScout.Integrations.DataForSEO.Password.v1");

    public async Task<DataForSeoIntegrationSettings> GetAsync(CancellationToken cancellationToken = default)
    {
        var setting = await db.DataForSeoIntegrationSettings
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.ProviderKey == ProviderKey, cancellationToken);

        if (setting is null)
        {
            var current = runtime.Current;
            return ToItem(current);
        }

        var password = string.IsNullOrWhiteSpace(setting.ProtectedPassword)
            ? null
            : protector.Unprotect(setting.ProtectedPassword);

        runtime.Update(new(
            setting.Enabled,
            setting.Login,
            password,
            setting.LocationName,
            setting.LanguageCode,
            setting.SearchDomain));

        return new(
            setting.Enabled,
            setting.Login,
            !string.IsNullOrWhiteSpace(password),
            setting.LocationName,
            setting.LanguageCode,
            setting.SearchDomain);
    }

    public async Task<DataForSeoIntegrationSettings> SaveAsync(
        UpdateDataForSeoIntegrationSettings request,
        CancellationToken cancellationToken = default)
    {
        var login = request.Login?.Trim() ?? string.Empty;
        if (login.Length == 0)
            throw new ArgumentException("An API login is required.", nameof(request.Login));

        var location = string.IsNullOrWhiteSpace(request.LocationName)
            ? "United Kingdom"
            : request.LocationName.Trim();
        var language = string.IsNullOrWhiteSpace(request.LanguageCode)
            ? "en"
            : request.LanguageCode.Trim();
        var searchDomain = string.IsNullOrWhiteSpace(request.SearchDomain)
            ? "google.co.uk"
            : request.SearchDomain.Trim();

        var setting = await db.DataForSeoIntegrationSettings
            .SingleOrDefaultAsync(x => x.ProviderKey == ProviderKey, cancellationToken);

        string? password = request.Password?.Trim();
        if (setting is not null && string.IsNullOrWhiteSpace(password))
        {
            password = string.IsNullOrWhiteSpace(setting.ProtectedPassword)
                ? null
                : protector.Unprotect(setting.ProtectedPassword);
        }

        if (request.Enabled && string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("An API password is required when DataForSEO is enabled.", nameof(request.Password));

        if (setting is null)
        {
            setting = new DataForSeoIntegrationSetting
            {
                Id = Guid.NewGuid(),
                ProviderKey = ProviderKey,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
            db.DataForSeoIntegrationSettings.Add(setting);
        }

        setting.Enabled = request.Enabled;
        setting.Login = login;
        setting.ProtectedPassword = string.IsNullOrWhiteSpace(password)
            ? null
            : protector.Protect(password);
        setting.LocationName = location;
        setting.LanguageCode = language;
        setting.SearchDomain = searchDomain;
        setting.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        runtime.Update(new(request.Enabled, login, password, location, language, searchDomain));

        return new(
            request.Enabled,
            login,
            !string.IsNullOrWhiteSpace(password),
            location,
            language,
            searchDomain);
    }

    private static DataForSeoIntegrationSettings ToItem(DataForSeoRuntimeSettings settings) =>
        new(
            settings.Enabled,
            settings.Login ?? string.Empty,
            !string.IsNullOrWhiteSpace(settings.Password),
            settings.LocationName,
            settings.LanguageCode,
            settings.SearchDomain);
}

public sealed class DataForSeoConnectionTester(DataForSeoRuntimeSettingsStore runtime)
{
    private readonly HttpClient httpClient = new()
    {
        BaseAddress = new Uri("https://api.dataforseo.com/")
    };

    public async Task<DataForSeoConnectionTestResult> TestAsync(CancellationToken cancellationToken = default)
    {
        var settings = runtime.Current;
        if (string.IsNullOrWhiteSpace(settings.Login) || string.IsNullOrWhiteSpace(settings.Password))
            return new(false, "DataForSEO credentials are not configured.", null);

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(Encoding.ASCII.GetBytes($"{settings.Login}:{settings.Password}")));

        try
        {
            using var response = await httpClient.GetAsync(
                "v3/appendix/user_data",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
                return new(false, $"DataForSEO returned HTTP {(int)response.StatusCode}.", settings.Login);

            using var document = await response.Content.ReadFromJsonAsync<JsonDocument>(
                cancellationToken: cancellationToken);

            if (document is null ||
                !document.RootElement.TryGetProperty("status_code", out var statusCode) ||
                statusCode.GetInt32() != 20000)
            {
                var message = document?.RootElement.TryGetProperty("status_message", out var statusMessage) == true
                    ? statusMessage.GetString()
                    : null;

                return new(false, message ?? "DataForSEO rejected the connection.", settings.Login);
            }

            return new(true, "Connected to DataForSEO successfully.", settings.Login);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new(false, "DataForSEO connection timed out.", settings.Login);
        }
        catch (HttpRequestException)
        {
            return new(false, "DataForSEO could not be reached.", settings.Login);
        }
    }
}