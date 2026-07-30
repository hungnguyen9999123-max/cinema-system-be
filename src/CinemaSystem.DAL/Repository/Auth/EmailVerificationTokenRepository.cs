using CinemaSystem.DAL.Interfaces;
using CinemaSystem.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaSystem.DAL.Repository.Auth;

public class EmailVerificationTokenRepository : IEmailVerificationTokenRepository
{
    private readonly CinemaDbContext _context;

    public EmailVerificationTokenRepository(CinemaDbContext context)
    {
        _context = context;
    }

    public async Task CreateAsync(EmailVerificationToken token)
    {
        _context.EmailVerificationTokens.Add(token);
        await _context.SaveChangesAsync();
    }

    public async Task<EmailVerificationToken?> GetByHashAsync(string tokenHash)
    {
        return await _context.EmailVerificationTokens
            .Include(token => token.User)
            .FirstOrDefaultAsync(token => token.TokenHash == tokenHash);
    }

    public async Task<EmailVerificationToken?> GetLatestUnverifiedByUserIdAsync(Guid userId)
    {
        return await _context.EmailVerificationTokens
            .Where(token => token.UserId == userId && !token.IsVerified)
            .OrderByDescending(token => token.CreatedAt)
            .ThenByDescending(token => token.Id)
            .FirstOrDefaultAsync();
    }

    public async Task InvalidateUnverifiedTokensAsync(Guid userId)
    {
        var now = DateTime.UtcNow;
        var activeTokens = await _context.EmailVerificationTokens
            .Where(token => token.UserId == userId && !token.IsVerified)
            .ToListAsync();

        foreach (var token in activeTokens)
        {
            token.IsVerified = true;
            token.ExpiresAt = now.AddSeconds(-1);
        }

        await _context.SaveChangesAsync();
    }

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
