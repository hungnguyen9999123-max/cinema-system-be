using CinemaSystem.Common.DTOs.Uploads;
using Microsoft.AspNetCore.Http;

namespace CinemaSystem.Services.Services.Uploads;

public interface ICloudinaryService
{
    Task<UploadImageResponse> UploadImageAsync(
        IFormFile file,
        string folder,
        CancellationToken cancellationToken);

    Task DeleteImageAsync(
        string publicId,
        CancellationToken cancellationToken);
}
