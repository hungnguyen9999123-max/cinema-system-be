using System;
using System.Collections.Generic;

namespace CinemaSystem.DAL.Models;

public partial class Feedback
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }

    public Guid MovieId { get; set; }

    public Guid BookingId { get; set; }

    public byte Rating { get; set; }

    public string? Comment { get; set; }

    public bool IsApproved { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Booking Booking { get; set; } = null!;

    public virtual User Customer { get; set; } = null!;

    public virtual Movie Movie { get; set; } = null!;
}
