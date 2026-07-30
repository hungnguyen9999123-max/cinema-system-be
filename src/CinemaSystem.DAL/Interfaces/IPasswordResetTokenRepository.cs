using CinemaSystem.DAL.Models;

namespace CinemaSystem.DAL.Interfaces;

public interface IPasswordResetTokenRepository
{
    Task CreateAsync(PasswordResetToken token);
    Task<PasswordResetToken?> GetByHashAsync(string tokenHash);
    Task UpdateAsync(PasswordResetToken token);
    Task SaveChangesAsync();
}
