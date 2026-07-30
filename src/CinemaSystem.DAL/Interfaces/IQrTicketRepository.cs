using CinemaSystem.Common.DTOs.QrTickets;
using CinemaSystem.DAL.Models;

namespace CinemaSystem.DAL.Interfaces;

public interface IQrTicketRepository
{
    Task<Ticket?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<Ticket?> GetByIdWithDetailsAsync(Guid ticketId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Ticket>> GetByBookingIdAsync(Guid bookingId, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<Ticket> tickets, CancellationToken cancellationToken = default);
    void Update(Ticket ticket);
    Task<(IReadOnlyList<Ticket> Items, int TotalCount)> GetCheckInHistoryAsync(
        CheckInHistorySearchRequest request,
        CancellationToken cancellationToken = default);
}
