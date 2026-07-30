using CinemaSystem.Common.DTOs.QrTickets;
using CinemaSystem.DAL.Interfaces;
using CinemaSystem.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaSystem.DAL.Repository.QrTickets;

public class QrTicketRepository : IQrTicketRepository
{
    private readonly CinemaDbContext _dbContext;

    public QrTicketRepository(CinemaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Ticket?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await BuildDetailedQuery()
            .FirstOrDefaultAsync(ticket => ticket.QrCode == token, cancellationToken);
    }

    public async Task<Ticket?> GetByIdWithDetailsAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        return await BuildDetailedQuery()
            .FirstOrDefaultAsync(ticket => ticket.Id == ticketId, cancellationToken);
    }

    public async Task<IReadOnlyList<Ticket>> GetByBookingIdAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        return await BuildDetailedQuery()
            .Where(ticket => ticket.BookingId == bookingId)
            .OrderBy(ticket => ticket.GeneratedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<Ticket> tickets, CancellationToken cancellationToken = default)
    {
        await _dbContext.Tickets.AddRangeAsync(tickets, cancellationToken);
    }

    public void Update(Ticket ticket)
    {
        _dbContext.Tickets.Update(ticket);
    }

    public async Task<(IReadOnlyList<Ticket> Items, int TotalCount)> GetCheckInHistoryAsync(
        CheckInHistorySearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Tickets
            .AsNoTracking()
            .Where(ticket => ticket.ScannedAt != null)
            .Include(ticket => ticket.Booking)
                .ThenInclude(booking => booking.Showtime)
                    .ThenInclude(showtime => showtime.Movie)
            .Include(ticket => ticket.Booking)
                .ThenInclude(booking => booking.Showtime)
                    .ThenInclude(showtime => showtime.Cinema)
            .Include(ticket => ticket.Booking)
                .ThenInclude(booking => booking.Showtime)
                    .ThenInclude(showtime => showtime.Room)
            .Include(ticket => ticket.BookingSeat)
                .ThenInclude(bookingSeat => bookingSeat.Seat)
            .Include(ticket => ticket.ScannedByNavigation)
            .AsQueryable();

        if (request.CinemaId.HasValue)
        {
            query = query.Where(ticket => ticket.Booking.Showtime.CinemaId == request.CinemaId.Value);
        }

        if (request.From.HasValue)
        {
            query = query.Where(ticket => ticket.ScannedAt >= request.From.Value);
        }

        if (request.To.HasValue)
        {
            query = query.Where(ticket => ticket.ScannedAt <= request.To.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, 100);

        var items = await query
            .OrderByDescending(ticket => ticket.ScannedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    private IQueryable<Ticket> BuildDetailedQuery()
    {
        return _dbContext.Tickets
            .Include(ticket => ticket.Booking)
                .ThenInclude(booking => booking.Showtime)
                    .ThenInclude(showtime => showtime.Movie)
            .Include(ticket => ticket.Booking)
                .ThenInclude(booking => booking.Showtime)
                    .ThenInclude(showtime => showtime.Cinema)
            .Include(ticket => ticket.Booking)
                .ThenInclude(booking => booking.Showtime)
                    .ThenInclude(showtime => showtime.Room)
            .Include(ticket => ticket.Booking)
                .ThenInclude(booking => booking.Payments)
            .Include(ticket => ticket.BookingSeat)
                .ThenInclude(bookingSeat => bookingSeat.Seat)
            .Include(ticket => ticket.ScannedByNavigation);
    }
}
