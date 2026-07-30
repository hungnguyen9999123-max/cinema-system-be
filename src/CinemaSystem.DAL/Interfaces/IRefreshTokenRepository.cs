using CinemaSystem.DAL.Models;

namespace CinemaSystem.DAL.Interfaces;

public interface IRefreshTokenRepository
{
    Task CreateAsync(RefreshToken refreshToken);
    Task<RefreshToken?> GetByHashAsync(string tokenHash);
    Task<RefreshToken?> GetActiveByUserIdAsync(Guid userId);
    Task RevokeAsync(RefreshToken refreshToken, string? revokedByIp, string? replacedByTokenHash = null);
    Task RevokeAllUserTokensAsync(Guid userId, string? revokedByIp = null);
    Task SaveChangesAsync();
}
