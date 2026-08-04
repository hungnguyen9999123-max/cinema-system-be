using System;
using System.Collections.Generic;

namespace CinemaSystem.DAL.Models;

public class ChatMessage
{
    public Guid Id { get; set; }

    public Guid ConversationId { get; set; }

    public Guid SenderId { get; set; }

    public string Content { get; set; } = null!;

    public string Type { get; set; } = null!; // TEXT, IMAGE, FILE, SYSTEM

    public DateTime SentAt { get; set; }

    public DateTime? ReadAt { get; set; }

    public bool IsPinned { get; set; }

    public Guid? ReplyToId { get; set; }

    public string? AttachmentUrl { get; set; }

    public string? AttachmentType { get; set; }

    public virtual ChatConversation Conversation { get; set; } = null!;

    public virtual User Sender { get; set; } = null!;

    public virtual ChatMessage? ReplyTo { get; set; }

    public virtual ICollection<ChatMessage> Replies { get; set; } = new List<ChatMessage>();
}
