using System;
using System.Collections.Generic;

namespace CinemaSystem.DAL.Models;

public partial class StaffAssignment
{
    public Guid Id { get; set; }

    public Guid StaffId { get; set; }

    public Guid CinemaId { get; set; }

    public DateOnly ShiftDate { get; set; }

    public string ShiftTime { get; set; } = null!;

    public DateTime AssignedAt { get; set; }

    public virtual Cinema Cinema { get; set; } = null!;

    public virtual User Staff { get; set; } = null!;
}
