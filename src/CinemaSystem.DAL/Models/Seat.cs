using System;
using System.Collections.Generic;

namespace CinemaSystem.DAL.Models;

public partial class Seat
{
    public Guid Id { get; set; }

    public Guid RoomId { get; set; }

    public Guid SeatTypeId { get; set; }

    public string SeatLabel { get; set; } = null!;

    public string RowLetter { get; set; } = null!;

    public byte ColNumber { get; set; }

    public string Status { get; set; } = null!;

    public virtual ICollection<BookingSeat> BookingSeats { get; set; } = new List<BookingSeat>();

    public virtual Room Room { get; set; } = null!;

    public virtual SeatType SeatType { get; set; } = null!;
}
