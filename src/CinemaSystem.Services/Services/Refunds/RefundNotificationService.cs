using CinemaSystem.Common.Enums;
using CinemaSystem.Common.Services;
using CinemaSystem.DAL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CinemaSystem.Services.Services.Refunds;

public sealed class RefundNotificationService(
    CinemaDbContext dbContext,
    IEmailService emailService,
    ILogger<RefundNotificationService> logger) : IRefundNotificationService
{
    private static readonly TimeSpan EmailDeliveryTimeout = TimeSpan.FromSeconds(5);

    public async Task NotifyCustomerAsync(Refund refund, CancellationToken cancellationToken = default)
    {
        if (!refund.RequestedBy.HasValue)
        {
            return;
        }

        var subject = "Cập nhật yêu cầu hoàn tiền";
        var body = BuildMessage(refund);
        var customerId = refund.RequestedBy.Value;
        dbContext.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            UserId = customerId,
            Type = "REFUND",
            Channel = "IN_APP",
            Subject = subject,
            Body = body,
            Status = "SENT",
            RetryCount = 0,
            SentAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        var customer = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(user => user.Id == customerId, cancellationToken);
        if (customer is null || !customer.IsEmailVerified || string.IsNullOrWhiteSpace(customer.Email))
        {
            return;
        }

        try
        {
            // A notification must never leave the refund API request waiting for an
            // unavailable SMTP server. The in-app notice has already been persisted.
            await emailService
                .SendEmailAsync(customer.Email, subject, $"<p>{System.Net.WebUtility.HtmlEncode(body)}</p>")
                .WaitAsync(EmailDeliveryTimeout, cancellationToken);
            dbContext.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = customerId,
                Type = "REFUND",
                Channel = "EMAIL",
                Subject = subject,
                Body = body,
                Status = "SENT",
                RetryCount = 0,
                SentAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not email refund update for {RefundId}.", refund.Id);
            dbContext.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = customerId,
                Type = "REFUND",
                Channel = "EMAIL",
                Subject = subject,
                Body = body,
                Status = "FAILED",
                RetryCount = 1,
                CreatedAt = DateTime.UtcNow
            });
        }
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string BuildMessage(Refund refund) => refund.Status switch
    {
        RefundStatus.Requested => "Yêu cầu hoàn tiền đang được xử lý.",
        RefundStatus.Succeeded => "Tiền vé đã được hoàn tự động vào ví CINE-MAX của bạn. Bạn có thể tạo yêu cầu rút tiền từ ví.",
        RefundStatus.Rejected => $"Yêu cầu hoàn tiền bị từ chối: {refund.DecisionReason ?? "OTHER"}.",
        _ => "Trạng thái yêu cầu hoàn tiền đã được cập nhật."
    };
}
