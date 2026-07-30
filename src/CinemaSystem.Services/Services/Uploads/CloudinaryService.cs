using CinemaSystem.Common.Constants;
using CinemaSystem.Common.DTOs.Uploads;
using CinemaSystem.Common.Exceptions;
using CinemaSystem.Common.Settings;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CinemaSystem.Services.Services.Uploads;

public sealed class CloudinaryService : ICloudinaryService
{
    private readonly Cloudinary _cloudinary;
    private readonly bool _isConfigured;

    public CloudinaryService(IOptions<CloudinarySettings> options)
    {
        var settings = options.Value;
        _isConfigured =
            !string.IsNullOrWhiteSpace(settings.CloudName) &&
            !string.IsNullOrWhiteSpace(settings.ApiKey) &&
            !string.IsNullOrWhiteSpace(settings.ApiSecret);

        _cloudinary = new Cloudinary(new Account(
            settings.CloudName,
            settings.ApiKey,
            settings.ApiSecret))
        {
            Api = { Secure = true }
        };
    }

    public async Task<UploadImageResponse> UploadImageAsync(
        IFormFile file,
        string folder,
        CancellationToken cancellationToken)
    {
        EnsureConfigured();

        if (!UploadFolders.Allowed.Contains(folder))
        {
            throw new ArgumentException(UploadMessages.InvalidFolder, nameof(folder));
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folder,
                UseFilename = false,
                UniqueFilename = true,
                Overwrite = false
            };

            var result = await _cloudinary.UploadAsync(uploadParams, cancellationToken);
            if (result.Error is not null ||
                result.SecureUrl is null ||
                string.IsNullOrWhiteSpace(result.PublicId))
            {
                throw new CloudinaryOperationException(
                    result.Error?.Message ?? UploadMessages.CloudinaryUploadFailed);
            }

            return new UploadImageResponse(
                result.SecureUrl.AbsoluteUri,
                result.PublicId);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (CloudinaryOperationException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new CloudinaryOperationException(
                UploadMessages.CloudinaryUploadFailed,
                exception);
        }
    }

    public async Task DeleteImageAsync(
        string publicId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(publicId))
        {
            return;
        }

        EnsureConfigured();
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var result = await _cloudinary.DestroyAsync(new DeletionParams(publicId)
            {
                ResourceType = ResourceType.Image,
                Invalidate = true
            });

            cancellationToken.ThrowIfCancellationRequested();

            if (result.Error is not null ||
                (!string.Equals(result.Result, "ok", StringComparison.OrdinalIgnoreCase) &&
                 !string.Equals(result.Result, "not found", StringComparison.OrdinalIgnoreCase)))
            {
                throw new CloudinaryOperationException(
                    result.Error?.Message ?? UploadMessages.CloudinaryDeleteFailed);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (CloudinaryOperationException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new CloudinaryOperationException(
                UploadMessages.CloudinaryDeleteFailed,
                exception);
        }
    }

    private void EnsureConfigured()
    {
        if (!_isConfigured)
        {
            throw new CloudinaryOperationException(
                UploadMessages.CloudinaryConfigurationMissing);
        }
    }
}
