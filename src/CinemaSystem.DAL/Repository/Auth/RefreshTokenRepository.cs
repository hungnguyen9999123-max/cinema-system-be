using CinemaSystem.DAL.Interfaces;
using CinemaSystem.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaSystem.DAL.Repository.Auth;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly CinemaDbContext _context;

    public RefreshTokenRepository(CinemaDbContext context)
    {
        _context = context;
    }

    public async Task CreateAsync(RefreshToken refreshToken)
    {
        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();
    }

    public async Task<RefreshToken?> GetByHashAsync(string tokenHash)
    {
        return await _context.RefreshTokens
            .Include(token => token.User)
            .FirstOrDefaultAsync(token => token.TokenHash == tokenHash);
    }

    public async Task<RefreshToken?> GetActiveByUserIdAsync(Guid userId)
    {
        return await _context.RefreshTokens
            .FirstOrDefaultAsync(token => token.UserId == userId && !token.IsRevoked && token.ExpiresAt > DateTime.UtcNow);
    }

    public async Task RevokeAsync(RefreshToken refreshToken, string? revokedByIp, string? replacedByTokenHash = null)
    {
        refreshToken.IsRevoked = true;
        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.RevokedByIp = revokedByIp;
        refreshToken.ReplacedByToken = replacedByTokenHash;
        await _context.SaveChangesAsync();
    }

    public async Task RevokeAllUserTokensAsync(Guid userId, string? revokedByIp = null)
    {
        var activeTokens = await _context.RefreshTokens
            .Where(token => token.UserId == userId && !token.IsRevoked)
            .ToListAsync();

        foreach (var token in activeTokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedByIp = revokedByIp;
        }

        await _context.SaveChangesAsync();
    }

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
