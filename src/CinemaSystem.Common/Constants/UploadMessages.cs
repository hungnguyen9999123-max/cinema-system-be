namespace CinemaSystem.Common.Constants;

public static class UploadMessages
{
    public const string UploadSuccess = "Upload image successfully.";
    public const string UploadFailed = "Failed to upload image.";
    public const string DeleteFailed = "Failed to delete image.";
    public const string FileRequired = "Image file is required.";
    public const string InvalidImageFormat = "Only jpg, jpeg, png, and webp images are allowed.";
    public const string FileTooLarge = "Image size must not exceed 5 MB.";
    public const string InvalidFolder = "The upload folder is not allowed.";
    public const string CloudinaryUploadFailed = "Cloudinary image upload failed.";
    public const string CloudinaryDeleteFailed = "Cloudinary image deletion failed.";
    public const string CloudinaryConfigurationMissing = "Cloudinary configuration is missing or invalid.";
}
