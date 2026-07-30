namespace CinemaSystem.Common.Services;

public interface IEmailService
{
    Task SendEmailAsync(
        string to,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default);
}
