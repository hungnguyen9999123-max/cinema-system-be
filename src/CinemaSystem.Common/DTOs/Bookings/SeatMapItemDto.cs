using System;

namespace CinemaSystem.Common.DTOs.Bookings;

public class SeatMapItemDto
{
    public Guid SeatId { get; set; }
    public string SeatLabel { get; set; } = null!;
    public string SeatType { get; set; } = null!;
    public string Status { get; set; } = null!;
}
