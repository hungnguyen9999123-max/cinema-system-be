using CinemaSystem.Common.DTOs.Reports;
using CinemaSystem.DAL.Interfaces;
using CinemaSystem.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaSystem.DAL.Repository.Reports;

/// <summary>
/// Provides Entity Framework Core queries for reporting and dashboard data.
/// </summary>
public sealed class ReportRepository(CinemaDbContext dbContext) : IReportRepository
{
    private const string PaymentSuccessStatus = "SUCCESS";
    private const string TicketCancelledStatus = "CANCELLED";
    private const string BookingConfirmedStatus = "CONFIRMED";
    private const string MovieActiveStatus = "ACTIVE";
    private const int TopMoviesLimit = 5;

    /// <inheritdoc />
    public async Task<DashboardResponse> GetDashboardAsync(
        CancellationToken cancellationToken = default)
    {
        var totalRevenue = await dbContext.Payments
            .AsNoTracking()
            .Where(payment => payment.Status == PaymentSuccessStatus)
            .SumAsync(payment => (decimal?)payment.Amount, cancellationToken) ?? 0m;

        var totalTickets = await dbContext.Tickets
            .AsNoTracking()
            .CountAsync(ticket => ticket.Status != TicketCancelledStatus, cancellationToken);

        var totalBookings = await dbContext.Bookings
            .AsNoTracking()
            .CountAsync(booking => booking.Status == BookingConfirmedStatus, cancellationToken);

        var activeMovies = await dbContext.Movies
            .AsNoTracking()
            .CountAsync(movie => movie.Status == MovieActiveStatus, cancellationToken);

        return new DashboardResponse
        {
            TotalRevenue = totalRevenue,
            TotalTickets = totalTickets,
            TotalBookings = totalBookings,
            ActiveMovies = activeMovies
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RevenueByMonthResponse>> GetRevenueByMonthAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Payments
            .AsNoTracking()
            .Where(payment =>
                payment.Status == PaymentSuccessStatus && payment.PaidAt.HasValue)
            .GroupBy(payment => payment.PaidAt!.Value.Month)
            .OrderBy(group => group.Key)
            .Select(group => new RevenueByMonthResponse
            {
                Month = group.Key,
                Revenue = group.Sum(payment => payment.Amount)
            })
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TopMovieResponse>> GetTopMoviesAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Tickets
            .AsNoTracking()
            .Join(
                dbContext.Bookings.AsNoTracking(),
                ticket => ticket.BookingId,
                booking => booking.Id,
                (ticket, booking) => new { ticket, booking })
            .Join(
                dbContext.Showtimes.AsNoTracking(),
                ticketBooking => ticketBooking.booking.ShowtimeId,
                showtime => showtime.Id,
                (ticketBooking, showtime) => new { ticketBooking.ticket, showtime })
            .Join(
                dbContext.Movies.AsNoTracking(),
                ticketShowtime => ticketShowtime.showtime.MovieId,
                movie => movie.Id,
                (ticketShowtime, movie) => new { movie.Title })
            .GroupBy(result => result.Title)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key)
            .Take(TopMoviesLimit)
            .Select(group => new TopMovieResponse
            {
                Title = group.Key,
                TicketsSold = group.Count()
            })
            .ToListAsync(cancellationToken);
    }
}
