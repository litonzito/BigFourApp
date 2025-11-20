using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace BigFourApp.Services
{
    public class BlobStorageService : IFileStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly BlobStorageOptions _options;
        private readonly ILogger<BlobStorageService> _logger;

        public BlobStorageService(
            BlobServiceClient blobServiceClient,
            IOptions<BlobStorageOptions> options,
            ILogger<BlobStorageService> logger)
        {
            _blobServiceClient = blobServiceClient;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<string> UploadAsync(IFormFile file, string folder, CancellationToken cancellationToken = default)
        {
            if (file == null || file.Length == 0 || !_options.IsConfigured)
            {
                return string.Empty;
            }

            var containerClient = _blobServiceClient.GetBlobContainerClient(_options.ContainerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);

            var blobName = $"{folder}/{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}";
            var blobClient = containerClient.GetBlobClient(blobName);

            var uploadOptions = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = string.IsNullOrWhiteSpace(file.ContentType)
                        ? "application/octet-stream"
                        : file.ContentType
                }
            };

            await using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(stream, uploadOptions, cancellationToken);

            return blobClient.Uri.ToString();
        }

        public async Task DeleteAsync(string? url, CancellationToken cancellationToken = default)
        {
            if (!_options.IsConfigured || string.IsNullOrWhiteSpace(url))
            {
                return;
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return;
            }

            var containerClient = _blobServiceClient.GetBlobContainerClient(_options.ContainerName);
            var prefix = $"{containerClient.Name}/";
            var absolutePath = uri.AbsolutePath.TrimStart('/');

            if (!absolutePath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var blobName = absolutePath.Substring(prefix.Length);

            try
            {
                await containerClient.DeleteBlobIfExistsAsync(blobName, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo eliminar el blob {BlobName}", blobName);
            }
        }
    }
}
