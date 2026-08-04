using System;
using System.Collections.Generic;

namespace CinemaSystem.DAL.Models;

public class ChatConversation
{
    public Guid Id { get; set; }

    public string Type { get; set; } = null!; // DIRECT, SUPPORT, GROUP

    public string? Title { get; set; }

    public string Status { get; set; } = null!; // ACTIVE, CLOSED

    public DateTime CreatedAt { get; set; }

    public DateTime? ClosedAt { get; set; }

    public virtual ICollection<ChatParticipant> Participants { get; set; } = new List<ChatParticipant>();

    public virtual ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}
