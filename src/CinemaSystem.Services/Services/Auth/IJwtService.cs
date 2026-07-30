using CinemaSystem.DAL.Models;

namespace CinemaSystem.Services.Services.Auth;

public interface IJwtService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
}
