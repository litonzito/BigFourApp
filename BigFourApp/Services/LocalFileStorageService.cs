using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace BigFourApp.Services
{
    public class LocalFileStorageService : IFileStorageService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<LocalFileStorageService> _logger;

        public LocalFileStorageService(IWebHostEnvironment environment, ILogger<LocalFileStorageService> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        public async Task<string> UploadAsync(IFormFile file, string folder, CancellationToken cancellationToken = default)
        {
            if (file == null || file.Length == 0)
            {
                return string.Empty;
            }

            var uploadsRoot = Path.Combine(_environment.WebRootPath, "uploads", folder);
            Directory.CreateDirectory(uploadsRoot);

            var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadsRoot, fileName);

            await using var stream = System.IO.File.Create(filePath);
            await file.CopyToAsync(stream, cancellationToken);

            return $"/uploads/{folder}/{fileName}".Replace("\\", "/");
        }

        public Task DeleteAsync(string? url, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(url) || !url.StartsWith("/uploads", StringComparison.OrdinalIgnoreCase))
            {
                return Task.CompletedTask;
            }

            var localPath = url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(_environment.WebRootPath, localPath);

            if (!System.IO.File.Exists(fullPath))
            {
                return Task.CompletedTask;
            }

            try
            {
                System.IO.File.Delete(fullPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo eliminar el archivo {File}", fullPath);
            }

            return Task.CompletedTask;
        }
    }
}
