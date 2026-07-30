using System;
using System.Collections.Generic;

namespace CinemaSystem.DAL.Models;

public partial class Room
{
    public Guid Id { get; set; }

    public Guid CinemaId { get; set; }

    public string Name { get; set; } = null!;

    public string RoomType { get; set; } = null!;

    public int TotalCapacity { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Cinema Cinema { get; set; } = null!;

    public virtual ICollection<Seat> Seats { get; set; } = new List<Seat>();

    public virtual ICollection<Showtime> Showtimes { get; set; } = new List<Showtime>();
}
