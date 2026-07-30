using CinemaSystem.DAL.Models;

namespace CinemaSystem.Services.Services.Refunds;

public interface IRefundNotificationService
{
    Task NotifyCustomerAsync(Refund refund, CancellationToken cancellationToken = default);
}
