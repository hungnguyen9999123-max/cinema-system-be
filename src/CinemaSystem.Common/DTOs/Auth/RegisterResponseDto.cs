namespace CinemaSystem.Common.DTOs.Auth;

public class RegisterResponseDto
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime EmailVerificationExpiresAt { get; set; }
    public bool VerificationEmailSent { get; set; }
}
