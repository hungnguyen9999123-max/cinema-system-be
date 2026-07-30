namespace CinemaSystem.Common.DTOs.Auth;

public class RegisterDto
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? Phone { get; set; }
    public string Role { get; set; } = "CUSTOMER";
}
