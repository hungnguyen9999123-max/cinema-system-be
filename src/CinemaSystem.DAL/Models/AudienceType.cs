using System;
using System.Collections.Generic;

namespace CinemaSystem.DAL.Models;

public partial class AudienceType
{
    public Guid Id { get; set; }

    public string Code { get; set; } = null!;

    public string DisplayName { get; set; } = null!;

    public decimal AudienceMultiplier { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<BookingSeat> BookingSeats { get; set; } = new List<BookingSeat>();
}
