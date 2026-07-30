using System;
using System.Collections.Generic;

namespace CinemaSystem.DAL.Models;

public partial class Cinema
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string Address { get; set; } = null!;

    public string City { get; set; } = null!;

    public string? Phone { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<PricingRule> PricingRules { get; set; } = new List<PricingRule>();

    public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();

    public virtual ICollection<Showtime> Showtimes { get; set; } = new List<Showtime>();

    public virtual ICollection<StaffAssignment> StaffAssignments { get; set; } = new List<StaffAssignment>();
}
