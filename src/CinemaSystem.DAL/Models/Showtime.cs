using System;
using System.Collections.Generic;

namespace CinemaSystem.DAL.Models;

public partial class Showtime
{
    public Guid Id { get; set; }

    public Guid MovieId { get; set; }

    public Guid RoomId { get; set; }

    public Guid CinemaId { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public string TimeSlot { get; set; } = null!;

    public string LanguageType { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<BookingSeat> BookingSeats { get; set; } = new List<BookingSeat>();

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual Cinema Cinema { get; set; } = null!;

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual Movie Movie { get; set; } = null!;

    public virtual Room Room { get; set; } = null!;
}
