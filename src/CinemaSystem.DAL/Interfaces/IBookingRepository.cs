using CinemaSystem.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CinemaSystem.DAL.Interfaces;

public interface IBookingRepository
{
    IQueryable<Booking> Query();

    Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Booking?> GetByIdForTicketGenerationAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Booking?> GetBookingByRefAsync(string bookingRef, CancellationToken cancellationToken = default);

    Task<Booking?> GetBookingByRefWithDetailsAsync(string bookingRef, CancellationToken cancellationToken = default);

    Task<List<Booking>> GetExpiredPendingBookingsAsync(DateTime expiredBefore, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Booking> Items, int TotalCount)> GetPagedByCustomerAsync(
        Guid customerId,
        string? status,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task AddAsync(Booking booking, CancellationToken cancellationToken = default);

    void Update(Booking booking);

    Task AddBookingSeatsAsync(IEnumerable<BookingSeat> bookingSeats, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
