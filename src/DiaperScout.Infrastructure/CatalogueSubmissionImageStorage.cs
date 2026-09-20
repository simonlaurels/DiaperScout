using DiaperScout.Application;
using Microsoft.Extensions.Configuration;

namespace DiaperScout.Infrastructure;

internal sealed class CatalogueSubmissionImageStorage : ICatalogueSubmissionImageStorage
{
    private readonly string rootPath;

    public CatalogueSubmissionImageStorage(IConfiguration configuration)
    {
        var configuredPath = configuration["CatalogueImages:RootPath"];

        rootPath = string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine(AppContext.BaseDirectory, "catalogue-images")
            : Path.GetFullPath(configuredPath);

        Directory.CreateDirectory(rootPath);
    }

    public async Task SaveAsync(
        string storageKey,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        var fullPath = Resolve(storageKey);
        var fullDirectory = Path.GetDirectoryName(fullPath)
            ?? throw new InvalidOperationException("The image storage path is invalid.");

        Directory.CreateDirectory(fullDirectory);

        await using var output = new FileStream(
            fullPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 64 * 1024,
            useAsync: true);

        await content.CopyToAsync(output, cancellationToken);
    }

    public Task<Stream?> OpenReadAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        var fullPath = Resolve(storageKey);

        if (!File.Exists(fullPath))
            return Task.FromResult<Stream?>(null);

        Stream stream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 64 * 1024,
            useAsync: true);

        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        var fullPath = Resolve(storageKey);

        if (File.Exists(fullPath))
            File.Delete(fullPath);

        return Task.CompletedTask;
    }

    private string Resolve(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
            throw new ArgumentException("A storage key is required.", nameof(storageKey));

        var fullPath = Path.GetFullPath(Path.Combine(rootPath, storageKey));

        if (!fullPath.StartsWith(rootPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The image storage key is outside the configured image storage root.");

        return fullPath;
    }
}
