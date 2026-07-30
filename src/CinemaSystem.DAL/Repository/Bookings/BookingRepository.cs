using CinemaSystem.DAL.Interfaces;
using CinemaSystem.DAL.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CinemaSystem.DAL.Repository.Bookings;

public class BookingRepository : IBookingRepository
{
    private readonly CinemaDbContext _dbContext;

    public BookingRepository(CinemaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IQueryable<Booking> Query()
    {
        return _dbContext.Bookings.AsQueryable();
    }

    public async Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Bookings
            .Include(b => b.BookingSeatBookings)
            .Include(b => b.FnbOrders)
                .ThenInclude(order => order.FnbOrderDetails)
                    .ThenInclude(detail => detail.Item)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task<Booking?> GetByIdForTicketGenerationAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Bookings
            .Include(b => b.BookingSeatBookings)
            .Include(b => b.FnbOrders)
                .ThenInclude(order => order.FnbOrderDetails)
                    .ThenInclude(detail => detail.Item)
            .Include(b => b.Showtime)
                .ThenInclude(s => s!.Movie)
            .Include(b => b.Showtime)
                .ThenInclude(s => s!.Room)
                    .ThenInclude(r => r!.Cinema)
            .Include(b => b.Tickets)
            .Include(b => b.Payments)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task<Booking?> GetBookingByRefAsync(string bookingRef, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Bookings
            .Include(b => b.BookingSeatBookings)
            .Include(b => b.FnbOrders)
            .FirstOrDefaultAsync(b => b.BookingRef == bookingRef, cancellationToken);
    }

    public async Task<Booking?> GetBookingByRefWithDetailsAsync(string bookingRef, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Bookings
            .AsNoTracking()
            .Include(b => b.BookingSeatBookings)
                .ThenInclude(bs => bs.Seat)
            .Include(b => b.Showtime!)
                .ThenInclude(s => s.Movie)
            .Include(b => b.Showtime!)
                .ThenInclude(s => s.Room)
            .Include(b => b.Showtime!)
                .ThenInclude(s => s.Cinema)
            .Include(b => b.Tickets)
            .Include(b => b.Payments)
            .FirstOrDefaultAsync(b => b.BookingRef == bookingRef, cancellationToken);
    }

    public async Task<List<Booking>> GetExpiredPendingBookingsAsync(DateTime expiredBefore, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Bookings
            .Include(b => b.BookingSeatBookings)
            .Where(b => b.Status == "PENDING" && b.ExpiresAt < expiredBefore)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<Booking> Items, int TotalCount)> GetPagedByCustomerAsync(
        Guid customerId,
        string? status,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        // Build base query, then attach includes BEFORE filters to preserve IIncludable chain.
        IQueryable<Booking> query = _dbContext.Bookings
            .AsNoTracking()
            .Where(b => b.CustomerId == customerId);

        query = query
            .Include(b => b.Showtime!)
                .ThenInclude(s => s.Movie!)
            .Include(b => b.Showtime!)
                .ThenInclude(s => s.Room!)
            .Include(b => b.Showtime!)
                .ThenInclude(s => s.Cinema!)
            .Include(b => b.BookingSeatBookingNavigations!)
                .ThenInclude(bs => bs.Seat!)
            .Include(b => b.FnbOrders!)
                .ThenInclude(o => o.FnbOrderDetails!)
                    .ThenInclude(d => d.Item!);

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim().ToUpperInvariant();
            query = query.Where(b => b.Status == normalizedStatus);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(b => b.BookedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(b => b.BookedAt <= toDate.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(b => b.BookedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task AddAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        await _dbContext.Bookings.AddAsync(booking, cancellationToken);
    }

    public void Update(Booking booking)
    {
        _dbContext.Bookings.Update(booking);
    }

    public async Task AddBookingSeatsAsync(IEnumerable<BookingSeat> bookingSeats, CancellationToken cancellationToken = default)
    {
        await _dbContext.BookingSeats.AddRangeAsync(bookingSeats, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
