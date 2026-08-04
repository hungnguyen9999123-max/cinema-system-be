using System;
using System.Collections.Generic;

namespace CinemaSystem.DAL.Models;

public class ChatParticipant
{
    public Guid Id { get; set; }

    public Guid ConversationId { get; set; }

    public Guid UserId { get; set; }

    public string Role { get; set; } = null!; // CUSTOMER, SUPPORT, ADMIN

    public DateTime JoinedAt { get; set; }

    public DateTime? LastReadAt { get; set; }

    public virtual ChatConversation Conversation { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
