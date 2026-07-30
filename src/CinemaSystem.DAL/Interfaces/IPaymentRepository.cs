using CinemaSystem.DAL.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CinemaSystem.DAL.Interfaces;

public interface IPaymentRepository
{
    IQueryable<Payment> Query();
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Payment?> GetByIdWithBookingAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Payment?> GetLatestForBookingAsync(Guid bookingId, string gateway, CancellationToken cancellationToken = default);
    Task<Payment?> GetByBookingAndIdempotencyKeyAsync(Guid bookingId, string idempotencyKeyHash, CancellationToken cancellationToken = default);
    Task<Payment?> GetSuccessfulForBookingAsync(Guid bookingId, CancellationToken cancellationToken = default);
    Task<Payment?> GetLatestForFnbOrderAsync(Guid fnbOrderId, string gateway, CancellationToken cancellationToken = default);
    Task AddAsync(Payment payment, CancellationToken cancellationToken = default);
    void Update(Payment payment);
}
