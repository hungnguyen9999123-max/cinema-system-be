using Microsoft.AspNetCore.Http;

namespace CinemaSystem.API.DTOs.Uploads;

public sealed class UploadImageRequest
{
    public IFormFile? File { get; init; }
    public string Folder { get; init; } = string.Empty;
}
