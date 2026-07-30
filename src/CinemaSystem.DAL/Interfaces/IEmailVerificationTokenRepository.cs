using CinemaSystem.DAL.Models;

namespace CinemaSystem.DAL.Interfaces;

public interface IEmailVerificationTokenRepository
{
    Task CreateAsync(EmailVerificationToken token);
    Task<EmailVerificationToken?> GetByHashAsync(string tokenHash);
    Task<EmailVerificationToken?> GetLatestUnverifiedByUserIdAsync(Guid userId);
    Task InvalidateUnverifiedTokensAsync(Guid userId);
    Task SaveChangesAsync();
}
