namespace CinemaSystem.Common.DTOs.Auth;

public sealed class GoogleLoginResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public GoogleLoginUserDto User { get; set; } = new();
}

public sealed class GoogleLoginUserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string Provider { get; set; } = string.Empty;
}
