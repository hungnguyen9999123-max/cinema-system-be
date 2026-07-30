using CinemaSystem.DAL.Interfaces;
using CinemaSystem.DAL.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CinemaSystem.DAL.Repository.Payments;

public class PaymentRepository : IPaymentRepository
{
    private readonly CinemaDbContext _dbContext;

    public PaymentRepository(CinemaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IQueryable<Payment> Query()
    {
        return _dbContext.Payments.AsQueryable();
    }

    public async Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Payments
            .Include(p => p.Booking)
                .ThenInclude(b => b.Showtime)
            .Include(p => p.Booking)
                .ThenInclude(b => b.BookingSeatBookings)
            .Include(p => p.Booking)
                .ThenInclude(b => b.Tickets)
            .Include(p => p.Booking)
                .ThenInclude(b => b.FnbOrders)
            .Include(p => p.FnbOrder)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    /// <summary>
    /// Lấy payment + booking đầy đủ (showtime, seats, tickets, fnb orders) cho callback hiển thị QR.
    /// </summary>
    public async Task<Payment?> GetByIdWithBookingAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Payments
            .Include(p => p.Booking)
                .ThenInclude(b => b.Showtime)
                    .ThenInclude(s => s.Movie)
            .Include(p => p.Booking)
                .ThenInclude(b => b.Showtime)
                    .ThenInclude(s => s.Room)
                        .ThenInclude(r => r.Cinema)
            .Include(p => p.Booking)
                .ThenInclude(b => b.BookingSeatBookings)
                    .ThenInclude(bs => bs.Seat)
            .Include(p => p.Booking)
                .ThenInclude(b => b.Tickets)
                    .ThenInclude(t => t.BookingSeat)
                        .ThenInclude(bs => bs.Seat)
            .Include(p => p.Booking)
                .ThenInclude(b => b.FnbOrders)
                    .ThenInclude(fo => fo.FnbOrderDetails)
                        .ThenInclude(d => d.Item)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Payment?> GetLatestForBookingAsync(Guid bookingId, string gateway, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Payments
            .Where(p => p.BookingId == bookingId && p.Gateway == gateway)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<Payment?> GetByBookingAndIdempotencyKeyAsync(Guid bookingId, string idempotencyKeyHash, CancellationToken cancellationToken = default) =>
        _dbContext.Payments
            .Include(payment => payment.Booking)
            .FirstOrDefaultAsync(payment => payment.BookingId == bookingId && payment.IdempotencyKeyHash == idempotencyKeyHash, cancellationToken);

    public Task<Payment?> GetSuccessfulForBookingAsync(Guid bookingId, CancellationToken cancellationToken = default) =>
        _dbContext.Payments
            .Include(payment => payment.Booking)
            .FirstOrDefaultAsync(payment => payment.BookingId == bookingId && payment.Status == "SUCCESS", cancellationToken);

    public async Task<Payment?> GetLatestForFnbOrderAsync(Guid fnbOrderId, string gateway, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Payments
            .Where(p => p.FnbOrderId == fnbOrderId && p.Gateway == gateway)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        await _dbContext.Payments.AddAsync(payment, cancellationToken);
    }

    public void Update(Payment payment)
    {
        // New payments are already tracked as Added. Calling Update on them
        // turns the pending INSERT into an UPDATE for a row that does not yet
        // exist, which surfaces as an optimistic-concurrency failure.
        if (_dbContext.Entry(payment).State == EntityState.Detached)
        {
            _dbContext.Payments.Update(payment);
        }
    }
}
