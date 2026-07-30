using CinemaSystem.DAL.Models;

namespace CinemaSystem.DAL.Interfaces;

public interface IRefundRepository
{
    Task<Payment?> GetPaymentForRefundAsync(Guid bookingId, CancellationToken cancellationToken = default);
    Task<Refund?> GetByIdAsync(Guid refundId, CancellationToken cancellationToken = default);
    Task<Refund?> GetActiveForPaymentAsync(Guid paymentId, CancellationToken cancellationToken = default);
    Task<Refund?> GetByIdempotencyKeyAsync(Guid customerId, string keyHash, CancellationToken cancellationToken = default);
    Task<int> CountRequestsByCustomerSinceAsync(Guid customerId, DateTime since, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Refund> Items, int TotalCount)> GetByCustomerAsync(Guid customerId, string? status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Refund> Items, int TotalCount)> GetForOperationsAsync(string? status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Refund>> GetDueForProcessingAsync(DateTime now, CancellationToken cancellationToken = default);
    Task AddAsync(Refund refund, CancellationToken cancellationToken = default);
    Task AddAttemptAsync(RefundGatewayAttempt attempt, CancellationToken cancellationToken = default);
    void Update(Refund refund);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
