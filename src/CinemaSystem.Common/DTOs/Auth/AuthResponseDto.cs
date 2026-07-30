namespace CinemaSystem.Common.DTOs.Auth;

public class AuthResponseDto
{
    public string Token { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public Guid UserId { get; set; }
    public string Email { get; set; } = null!;
    public string Role { get; set; } = null!;
    public string FullName { get; set; } = null!;
}
