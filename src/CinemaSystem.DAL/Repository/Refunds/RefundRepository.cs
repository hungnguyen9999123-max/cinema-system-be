using CinemaSystem.Common.Enums;
using CinemaSystem.DAL.Interfaces;
using CinemaSystem.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaSystem.DAL.Repository.Refunds;

public sealed class RefundRepository(CinemaDbContext dbContext) : IRefundRepository
{
    public Task<Payment?> GetPaymentForRefundAsync(Guid bookingId, CancellationToken cancellationToken = default) =>
        dbContext.Payments
            .Include(payment => payment.Refunds)
            .Include(payment => payment.Booking)
                .ThenInclude(booking => booking.Showtime)
            .Include(payment => payment.Booking)
                .ThenInclude(booking => booking.Tickets)
            .Include(payment => payment.Booking)
                .ThenInclude(booking => booking.BookingSeatBookings)
            .Include(payment => payment.Booking)
                .ThenInclude(booking => booking.FnbOrders)
            .FirstOrDefaultAsync(payment => payment.BookingId == bookingId && payment.Status == "SUCCESS", cancellationToken);

    public Task<Refund?> GetByIdAsync(Guid refundId, CancellationToken cancellationToken = default) =>
        DetailsQuery().FirstOrDefaultAsync(refund => refund.Id == refundId, cancellationToken);

    public Task<Refund?> GetActiveForPaymentAsync(Guid paymentId, CancellationToken cancellationToken = default) =>
        DetailsQuery().FirstOrDefaultAsync(refund =>
            refund.PaymentId == paymentId &&
            (refund.Status == RefundStatus.Requested ||
             refund.Status == RefundStatus.Processing ||
             refund.Status == RefundStatus.ReconciliationRequired), cancellationToken);

    public Task<Refund?> GetByIdempotencyKeyAsync(Guid customerId, string keyHash, CancellationToken cancellationToken = default) =>
        DetailsQuery().FirstOrDefaultAsync(refund => refund.RequestedBy == customerId && refund.IdempotencyKeyHash == keyHash, cancellationToken);

    public Task<int> CountRequestsByCustomerSinceAsync(Guid customerId, DateTime since, CancellationToken cancellationToken = default) =>
        dbContext.Refunds.CountAsync(refund => refund.RequestedBy == customerId && refund.RequestedAt >= since, cancellationToken);

    public async Task<(IReadOnlyList<Refund> Items, int TotalCount)> GetByCustomerAsync(Guid customerId, string? status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = DetailsQuery().AsNoTracking().Where(refund => refund.RequestedBy == customerId);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(refund => refund.Status == status.Trim().ToUpperInvariant());
        return await PageAsync(query, page, pageSize, cancellationToken);
    }

    public async Task<(IReadOnlyList<Refund> Items, int TotalCount)> GetForOperationsAsync(string? status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = DetailsQuery().AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(refund => refund.Status == status.Trim().ToUpperInvariant());
        return await PageAsync(query, page, pageSize, cancellationToken);
    }

    public async Task<IReadOnlyList<Refund>> GetDueForProcessingAsync(DateTime now, CancellationToken cancellationToken = default)
    {
        return await DetailsQuery()
            .Where(refund => refund.Status == RefundStatus.Processing ||
                (refund.Status == RefundStatus.ReconciliationRequired &&
                 (!refund.NextReconciliationAt.HasValue || refund.NextReconciliationAt <= now)))
            .OrderBy(refund => refund.UpdatedAt ?? refund.RequestedAt)
            .Take(25)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Refund refund, CancellationToken cancellationToken = default)
    {
        await dbContext.Refunds.AddAsync(refund, cancellationToken);
    }

    public async Task AddAttemptAsync(RefundGatewayAttempt attempt, CancellationToken cancellationToken = default)
    {
        await dbContext.RefundGatewayAttempts.AddAsync(attempt, cancellationToken);
    }

    public void Update(Refund refund)
    {
        // A newly-created refund is already tracked as Added. Marking it as
        // Modified changes the pending INSERT into an UPDATE that includes a
        // null row-version predicate, which always raises a concurrency error.
        // Only attach entities that originated outside this DbContext.
        if (dbContext.Entry(refund).State == EntityState.Detached)
            dbContext.Refunds.Update(refund);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => dbContext.SaveChangesAsync(cancellationToken);

    private IQueryable<Refund> DetailsQuery() =>
        dbContext.Refunds
            .Include(refund => refund.Payment)
                .ThenInclude(payment => payment.Booking)
                    .ThenInclude(booking => booking.Showtime)
            .Include(refund => refund.Payment)
                .ThenInclude(payment => payment.Booking)
                    .ThenInclude(booking => booking.Tickets)
            .Include(refund => refund.Payment)
                .ThenInclude(payment => payment.Booking)
                    .ThenInclude(booking => booking.BookingSeatBookings)
            .Include(refund => refund.GatewayAttempts);

    private static async Task<(IReadOnlyList<Refund> Items, int TotalCount)> PageAsync(IQueryable<Refund> query, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(refund => refund.RequestedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }
}
