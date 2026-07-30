using CinemaSystem.DAL.Models;

namespace CinemaSystem.Services.Services.Refunds;

public sealed class RefundAuditService(CinemaDbContext dbContext) : IRefundAuditService
{
    public async Task LogAsync(Guid? actorId, string action, Guid? refundId, string? ipAddress, string endpoint, CancellationToken cancellationToken = default)
    {
        dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorId = actorId,
            ActionType = action[..Math.Min(action.Length, 20)],
            EntityName = "REFUND",
            EntityId = refundId,
            IpAddress = string.IsNullOrWhiteSpace(ipAddress) ? null : ipAddress[..Math.Min(ipAddress.Length, 45)],
            Endpoint = endpoint[..Math.Min(endpoint.Length, 255)],
            CreatedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
