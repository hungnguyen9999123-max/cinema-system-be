using System;
using System.Collections.Generic;

namespace CinemaSystem.DAL.Models;

public partial class AuditLog
{
    public Guid Id { get; set; }

    public Guid? ActorId { get; set; }

    public string ActionType { get; set; } = null!;

    public string EntityName { get; set; } = null!;

    public Guid? EntityId { get; set; }

    public string? BeforeData { get; set; }

    public string? AfterData { get; set; }

    public string? IpAddress { get; set; }

    public string? Endpoint { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User? Actor { get; set; }
}
