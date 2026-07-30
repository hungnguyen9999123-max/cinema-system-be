namespace CinemaSystem.Services.Services.Refunds;

public interface IRefundAuditService
{
    Task LogAsync(Guid? actorId, string action, Guid? refundId, string? ipAddress, string endpoint, CancellationToken cancellationToken = default);
}
