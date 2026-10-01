using Azure.Identity;
using Microsoft.AspNetCore.DataProtection;

namespace DiaperScout.Web.Services;

public static class WebDataProtection
{
    public static IServiceCollection AddWebDataProtection(this IServiceCollection services,
        IConfiguration configuration, IHostEnvironment environment)
    {
        var applicationName = configuration["DataProtection:ApplicationName"];
        var blob = configuration["DataProtection:BlobUri"];
        var key = configuration["DataProtection:KeyVaultKeyUri"];
        if (environment.IsDevelopment() && applicationName is null && blob is null && key is null)
        {
            services.AddDataProtection().SetApplicationName("DiaperScout.Web.Development");
            return services;
        }

        if (string.IsNullOrWhiteSpace(applicationName) ||
            applicationName != $"DiaperScout.Web.{environment.EnvironmentName}")
            throw new InvalidOperationException("DataProtection:ApplicationName must identify this Web environment: DiaperScout.Web." + environment.EnvironmentName);

        var blobUri = RequireUri(blob, "BlobUri", ".blob.core.windows.net");
        var keyUri = RequireUri(key, "KeyVaultKeyUri", ".vault.azure.net");
        if (blobUri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries).Length < 2)
            throw new InvalidOperationException("DataProtection:BlobUri must identify a container and key-ring blob.");
        var keySegments = keyUri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (keySegments.Length != 2 || keySegments[0] != "keys")
            throw new InvalidOperationException("DataProtection:KeyVaultKeyUri must be a versionless /keys/name URI.");

        // Production uses only the Container App identity: no secrets or developer credential fallback.
        var credential = new ManagedIdentityCredential(ManagedIdentityId.SystemAssigned);
        services.AddDataProtection()
            .SetApplicationName(applicationName)
            .SetDefaultKeyLifetime(TimeSpan.FromDays(90))
            .PersistKeysToAzureBlobStorage(blobUri, credential)
            .ProtectKeysWithAzureKeyVault(keyUri, credential);
        return services;
    }

    private static Uri RequireUri(string? value, string setting, string hostSuffix)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != "https" ||
            !uri.IsDefaultPort || !uri.Host.EndsWith(hostSuffix, StringComparison.OrdinalIgnoreCase) ||
            uri.Query.Length != 0 || uri.Fragment.Length != 0 || uri.UserInfo.Length != 0)
            throw new InvalidOperationException($"DataProtection:{setting} must be a credential-free Azure HTTPS URI.");
        return uri;
    }
}
