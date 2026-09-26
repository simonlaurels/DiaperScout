using Amazon.S3;
using Amazon.S3.Model;
using DiaperScout.Application;
using Microsoft.Extensions.Configuration;

namespace DiaperScout.Infrastructure;

internal sealed class R2CatalogueSubmissionImageStorage : ICatalogueSubmissionImageStorage
{
    private readonly IAmazonS3 client;
    private readonly string bucketName;

    public R2CatalogueSubmissionImageStorage(IConfiguration configuration)
    {
        var endpoint = configuration["CloudflareR2:Endpoint"]
            ?? throw new InvalidOperationException("Cloudflare R2 endpoint is required.");

        var accessKeyId = configuration["CloudflareR2:AccessKeyId"]
            ?? throw new InvalidOperationException("Cloudflare R2 access key ID is required.");

        var secretAccessKey = configuration["CloudflareR2:SecretAccessKey"]
            ?? throw new InvalidOperationException("Cloudflare R2 secret access key is required.");

        bucketName = configuration["CloudflareR2:BucketName"]
            ?? throw new InvalidOperationException("Cloudflare R2 bucket name is required.");

        client = new AmazonS3Client(
            accessKeyId,
            secretAccessKey,
            new AmazonS3Config
            {
                ServiceURL = endpoint,
                ForcePathStyle = true
            });
    }

    public async Task SaveAsync(
        string storageKey,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        ValidateStorageKey(storageKey);

        var request = new PutObjectRequest
        {
            BucketName = bucketName,
            Key = storageKey,
            InputStream = content
        };

        await client.PutObjectAsync(request, cancellationToken);
    }

    public async Task<Stream?> OpenReadAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        ValidateStorageKey(storageKey);

        try
        {
            var response = await client.GetObjectAsync(
                new GetObjectRequest
                {
                    BucketName = bucketName,
                    Key = storageKey
                },
                cancellationToken);

            var memory = new MemoryStream();
            await response.ResponseStream.CopyToAsync(memory, cancellationToken);
            memory.Position = 0;

            response.Dispose();

            return memory;
        }
        catch (AmazonS3Exception exception)
            when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task DeleteAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        ValidateStorageKey(storageKey);

        await client.DeleteObjectAsync(
            new DeleteObjectRequest
            {
                BucketName = bucketName,
                Key = storageKey
            },
            cancellationToken);
    }

    private static void ValidateStorageKey(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
            throw new ArgumentException(
                "A storage key is required.",
                nameof(storageKey));

        if (storageKey.StartsWith('/') ||
            storageKey.Contains("..", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "The image storage key is invalid.");
        }
    }
}