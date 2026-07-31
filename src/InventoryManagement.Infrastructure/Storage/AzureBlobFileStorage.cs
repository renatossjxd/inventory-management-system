using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using InventoryManagement.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace InventoryManagement.Infrastructure.Storage;

public sealed class AzureBlobFileStorage(BlobServiceClient serviceClient, IOptions<BlobStorageOptions> options)
    : IFileStorage
{
    private readonly BlobStorageOptions _options = options.Value;

    public async Task<string> UploadImageAsync(Stream content, string fileName, string contentType,
        CancellationToken cancellationToken = default)
    {
        var container = serviceClient.GetBlobContainerClient(_options.ContainerName);
        await container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var blobName = $"{Guid.NewGuid():N}{extension}";
        var blob = container.GetBlobClient(blobName);
        await blob.UploadAsync(content, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType },
            Conditions = new BlobRequestConditions { IfNoneMatch = Azure.ETag.All }
        }, cancellationToken);

        return string.IsNullOrWhiteSpace(_options.PublicBaseUrl)
            ? blob.Uri.ToString()
            : $"{_options.PublicBaseUrl.TrimEnd('/')}/{blobName}";
    }

    public async Task DeleteAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(fileUrl, UriKind.Absolute, out var uri)) return;
        var blobName = Uri.UnescapeDataString(uri.Segments[^1]);
        if (string.IsNullOrWhiteSpace(blobName)) return;
        var container = serviceClient.GetBlobContainerClient(_options.ContainerName);
        await container.DeleteBlobIfExistsAsync(blobName, cancellationToken: cancellationToken);
    }
}
