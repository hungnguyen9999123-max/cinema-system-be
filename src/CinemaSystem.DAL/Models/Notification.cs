using System;
using System.Collections.Generic;

namespace CinemaSystem.DAL.Models;

public partial class Notification
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Type { get; set; } = null!;

    public string Channel { get; set; } = null!;

    public string? Subject { get; set; }

    public string Body { get; set; } = null!;

    public string Status { get; set; } = null!;

    public byte RetryCount { get; set; }

    public DateTime? SentAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
