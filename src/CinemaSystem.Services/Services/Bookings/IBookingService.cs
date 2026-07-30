using CinemaSystem.Common.DTOs.Bookings;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CinemaSystem.Services.Services.Bookings;

public interface IBookingService
{
    Task<CreateBookingResponseDto> CreateBookingAsync(
        Guid callerUserId,
        string callerRole,
        CreateBookingRequestDto request,
        CancellationToken cancellationToken = default);

    Task<BookingResponseDto> GetBookingByIdAsync(Guid bookingId, Guid callerUserId, string callerRole, CancellationToken cancellationToken = default);

    Task<IEnumerable<SeatMapItemDto>> GetSeatMapAsync(Guid showtimeId, CancellationToken cancellationToken = default);

    Task<BookingResponseDto> CancelBookingAsync(Guid bookingId, Guid callerUserId, string callerRole, CancellationToken cancellationToken = default);

    Task<MyBookingsPagedResultDto> GetMyBookingsAsync(
        Guid customerId,
        MyBookingsQueryRequest request,
        CancellationToken cancellationToken = default);
}
