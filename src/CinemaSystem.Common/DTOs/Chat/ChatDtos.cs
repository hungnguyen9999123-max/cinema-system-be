namespace CinemaSystem.Common.DTOs.Chat;

public record ChatConversationDto(
    Guid Id,
    string Type,
    string? Title,
    string Status,
    DateTime CreatedAt,
    DateTime? ClosedAt,
    IReadOnlyList<ChatParticipantDto> Participants,
    ChatMessageDto? LastMessage,
    int UnreadCount
);

public record ChatParticipantDto(
    Guid Id,
    Guid UserId,
    string FullName,
    string? AvatarUrl,
    string Role,
    DateTime JoinedAt,
    DateTime? LastReadAt
);

public record ChatMessageDto(
    Guid Id,
    Guid ConversationId,
    Guid SenderId,
    string SenderName,
    string? SenderAvatar,
    string Content,
    string Type,
    DateTime SentAt,
    DateTime? ReadAt,
    bool IsPinned,
    Guid? ReplyToId,
    string? ReplyToContent,
    string? ReplyToSenderName,
    string? AttachmentUrl,
    string? AttachmentType
);

public record SendMessageRequestDto(
    string Content,
    string Type = "TEXT",
    Guid? ReplyToId = null,
    string? AttachmentUrl = null,
    string? AttachmentType = null
);

public record PagedMessagesResultDto(
    IReadOnlyList<ChatMessageDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages
);

public record PagedConversationsResultDto(
    IReadOnlyList<ChatConversationDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages
);

public record CreateConversationRequestDto(
    string Type,
    string? Title = null,
    IReadOnlyList<Guid>? ParticipantIds = null
);

public record MarkMessagesReadRequestDto(
    Guid ConversationId,
    Guid? LastMessageId = null
);
