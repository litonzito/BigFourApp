using Microsoft.AspNetCore.Http;

namespace BigFourApp.Services
{
    public interface IFileStorageService
    {
        Task<string> UploadAsync(IFormFile file, string folder, CancellationToken cancellationToken = default);
        Task DeleteAsync(string? url, CancellationToken cancellationToken = default);
    }
}
