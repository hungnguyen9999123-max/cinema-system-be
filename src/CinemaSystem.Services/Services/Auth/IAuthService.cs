using CinemaSystem.Common.DTOs.Auth;
using CinemaSystem.Common.DTOs.Responses;

namespace CinemaSystem.Services.Services.Auth;

public interface IAuthService
{
    Task<ApiResponse<RegisterResponseDto>> RegisterAsync(
        RegisterRequestDto request,
        string? clientIp = null,
        CancellationToken cancellationToken = default);
    Task<ApiResponse<LoginResponseDto>> LoginAsync(LoginRequestDto request, string? clientIp = null);
    Task<ApiResponse<GoogleLoginResponseDto>> GoogleLoginAsync(GoogleLoginRequest request, string? clientIp = null);
    Task<ApiResponse<LoginResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto request, string? clientIp = null);
    Task<EmailVerificationResultDto> VerifyEmailAsync(VerifyEmailRequestDto request, string? clientIp = null);
    Task<ApiResponse<ResendVerificationResponseDto>> ResendVerificationAsync(
        ResendVerificationRequestDto request,
        string? clientIp = null,
        CancellationToken cancellationToken = default);
    Task<ApiResponse<string>> ForgotPasswordAsync(
        ForgotPasswordRequestDto request,
        CancellationToken cancellationToken = default);
    Task<ApiResponse<string>> ResetPasswordAsync(ResetPasswordRequestDto request, string? clientIp = null);
}
