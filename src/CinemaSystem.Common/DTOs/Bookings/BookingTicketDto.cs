using System;

namespace CinemaSystem.Common.DTOs.Bookings;

public sealed record BookingTicketDto(
    Guid TicketId,
    Guid BookingSeatId,
    Guid SeatId,
    string SeatLabel,
    string Token,
    string QrImageBase64);
