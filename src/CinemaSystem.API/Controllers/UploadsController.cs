using CinemaSystem.API.DTOs.Uploads;
using CinemaSystem.Common.Constants;
using CinemaSystem.Common.DTOs.Responses;
using CinemaSystem.Common.DTOs.Uploads;
using CinemaSystem.Services.Services.Uploads;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaSystem.API.Controllers;

[ApiController]
[Route("api/uploads")]
[Authorize(Roles = "Admin,Manager,Staff")]
public sealed class UploadsController(ICloudinaryService cloudinaryService) : ControllerBase
{
    private const long MaxFileSize = 5 * 1024 * 1024;

    private static readonly IReadOnlySet<string> AllowedExtensions =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp"
        };

    private static readonly IReadOnlySet<string> AllowedContentTypes =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/webp"
        };

    [HttpPost("image")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<ApiResponse<UploadImageResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<UploadImageResponse>>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<UploadImageResponse>>> UploadImage(
        [FromForm] UploadImageRequest request,
        CancellationToken cancellationToken)
    {
        if (request.File is null || request.File.Length == 0)
        {
            return BadRequest(ApiResponse<UploadImageResponse>.Fail(UploadMessages.FileRequired));
        }

        if (request.File.Length > MaxFileSize)
        {
            return BadRequest(ApiResponse<UploadImageResponse>.Fail(UploadMessages.FileTooLarge));
        }

        var extension = Path.GetExtension(request.File.FileName);
        if (!AllowedExtensions.Contains(extension) ||
            !AllowedContentTypes.Contains(request.File.ContentType))
        {
            return BadRequest(ApiResponse<UploadImageResponse>.Fail(UploadMessages.InvalidImageFormat));
        }

        var folder = request.Folder?.Trim() ?? string.Empty;
        if (!UploadFolders.Allowed.Contains(folder))
        {
            return BadRequest(ApiResponse<UploadImageResponse>.Fail(UploadMessages.InvalidFolder));
        }

        var result = await cloudinaryService.UploadImageAsync(
            request.File,
            folder,
            cancellationToken);

        return Ok(ApiResponse<UploadImageResponse>.Success(
            result,
            UploadMessages.UploadSuccess));
    }
}
