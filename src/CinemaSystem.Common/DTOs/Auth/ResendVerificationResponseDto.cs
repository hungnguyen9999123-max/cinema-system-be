namespace CinemaSystem.Common.DTOs.Auth;

public class ResendVerificationResponseDto
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public DateTime EmailVerificationExpiresAt { get; set; }
    public bool VerificationEmailSent { get; set; }
}
