using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;
using CinemaSystem.Common.Services;

namespace CinemaSystem.API.Services;

internal sealed class SmtpSettings
{
    public string? Host { get; set; }
    public int Port { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool EnableSsl { get; set; }
    public string? FromEmail { get; set; }
    public string? FromName { get; set; }
}

public sealed class EmailService : IEmailService
{
    private static readonly TimeSpan SendTimeout = TimeSpan.FromSeconds(60);
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailAsync(
        string to,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        var smtp = new SmtpSettings();
        _configuration.GetSection("Smtp").Bind(smtp);

        if (string.IsNullOrWhiteSpace(smtp.Host)
            || smtp.Port == 0
            || string.IsNullOrWhiteSpace(smtp.Username)
            || string.IsNullOrWhiteSpace(smtp.Password)
            || string.IsNullOrWhiteSpace(smtp.FromEmail))
        {
            _logger.LogError("SMTP configuration is missing or incomplete.");
            throw new InvalidOperationException("SMTP configuration is missing or incomplete.");
        }

        using var message = new MailMessage();
        message.From = new MailAddress(smtp.FromEmail ?? smtp.Username ?? "no-reply@example.com", smtp.FromName ?? "Cinema System");
        message.To.Add(to);
        message.Subject = subject;
        message.Body = htmlBody;
        message.IsBodyHtml = true;

        using var client = new SmtpClient(smtp.Host, smtp.Port)
        {
            EnableSsl = smtp.EnableSsl,
            Credentials = new NetworkCredential(smtp.Username, smtp.Password)
        };

        _logger.LogInformation("Sending email to {Email} via SMTP {Host}:{Port}", to, smtp.Host, smtp.Port);

        using var timeoutCts =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(SendTimeout);

        try
        {
            await client.SendMailAsync(message, timeoutCts.Token);
            _logger.LogInformation("Email sent to {Email}", to);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                "Sending email to {Email} timed out after {TimeoutSeconds} seconds.",
                to,
                SendTimeout.TotalSeconds);
            throw new TimeoutException(
                $"SMTP sending timed out after {SendTimeout.TotalSeconds:0} seconds.");
        }
    }
}
