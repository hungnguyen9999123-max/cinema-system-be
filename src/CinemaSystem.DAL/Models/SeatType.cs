using System;
using System.Collections.Generic;

namespace CinemaSystem.DAL.Models;

public partial class SeatType
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public decimal SeatMultiplier { get; set; }

    public string? Description { get; set; }

    public string Status { get; set; } = null!;

    public virtual ICollection<Seat> Seats { get; set; } = new List<Seat>();
}
