using CinemaSystem.DAL.Interfaces;
using CinemaSystem.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaSystem.DAL.Repository.Auth;

public class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly CinemaDbContext _context;

    public PasswordResetTokenRepository(CinemaDbContext context)
    {
        _context = context;
    }

    public async Task CreateAsync(PasswordResetToken token)
    {
        _context.PasswordResetTokens.Add(token);
        await _context.SaveChangesAsync();
    }

    public async Task<PasswordResetToken?> GetByHashAsync(string tokenHash)
    {
        return await _context.PasswordResetTokens
            .Include(token => token.User)
            .FirstOrDefaultAsync(token => token.TokenHash == tokenHash);
    }

    public async Task UpdateAsync(PasswordResetToken token)
    {
        _context.PasswordResetTokens.Update(token);
        await _context.SaveChangesAsync();
    }

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
