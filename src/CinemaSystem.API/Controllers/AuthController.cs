using CinemaSystem.Common.DTOs.Auth;
using CinemaSystem.Common.DTOs.Responses;
using CinemaSystem.Services.Services.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CinemaSystem.API.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<RegisterResponseDto>>> Register(
        [FromBody] RegisterRequestDto request,
        CancellationToken cancellationToken)
    {
        var response = await _authService.RegisterAsync(request, GetClientIp(), cancellationToken);
        return response.IsSuccess ? Created(string.Empty, response) : BadRequest(response);
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth-strict")]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Login([FromBody] LoginRequestDto request)
    {
        var response = await _authService.LoginAsync(request, GetClientIp());
        return response.IsSuccess ? Ok(response) : Unauthorized(response);
    }

    [HttpPost("google-login")]
    public async Task<ActionResult<ApiResponse<GoogleLoginResponseDto>>> GoogleLogin([FromBody] GoogleLoginRequest request)
    {
        var response = await _authService.GoogleLoginAsync(request, GetClientIp());
        return response.IsSuccess ? Ok(response) : Unauthorized(response);
    }

    [HttpPost("refresh-token")]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> RefreshToken([FromBody] RefreshTokenRequestDto request)
    {
        var response = await _authService.RefreshTokenAsync(request, GetClientIp());
        return response.IsSuccess ? Ok(response) : Unauthorized(response);
    }

    [HttpPost("verify-email")]
    public async Task<ActionResult<EmailVerificationResultDto>> VerifyEmail([FromBody] VerifyEmailRequestDto request)
    {
        var response = await _authService.VerifyEmailAsync(request, GetClientIp());
        return response.IsSuccess ? Ok(response) : BadRequest(response);
    }

    [HttpGet("verify-email")]
    public async Task<IActionResult> VerifyEmailGet([FromQuery] string token)
    {
        var dto = new VerifyEmailRequestDto { Token = token };
        var response = await _authService.VerifyEmailAsync(dto, GetClientIp());
        return Redirect(response.RedirectUrl);
    }

    [HttpPost("resend-verification")]
    public async Task<ActionResult<ApiResponse<ResendVerificationResponseDto>>> ResendVerification(
        [FromBody] ResendVerificationRequestDto request,
        CancellationToken cancellationToken)
    {
        var response = await _authService.ResendVerificationAsync(
            request,
            GetClientIp(),
            cancellationToken);
        return response.IsSuccess ? Ok(response) : BadRequest(response);

    }

    [HttpPost("forgot-password")]
    [EnableRateLimiting("auth-strict")]
    public async Task<ActionResult<ApiResponse<string>>> ForgotPassword(
        [FromBody] ForgotPasswordRequestDto request,
        CancellationToken cancellationToken)
    {
        var response = await _authService.ForgotPasswordAsync(request, cancellationToken);
        return response.IsSuccess ? Ok(response) : BadRequest(response);
    }

    [HttpPost("reset-password")]
    public async Task<ActionResult<ApiResponse<string>>> ResetPassword([FromBody] ResetPasswordRequestDto request)
    {
        var response = await _authService.ResetPasswordAsync(request, GetClientIp());
        return response.IsSuccess ? Ok(response) : BadRequest(response);
    }

    private string? GetClientIp()
    {
        var forwarded = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            return forwarded.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
        }
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}
