using CinemaSystem.Common;
using CinemaSystem.Common.DTOs.Chat;
using CinemaSystem.DAL.Models;
using CinemaSystem.DAL.Repository.Chat;

namespace CinemaSystem.Services.Services.Chat;

public sealed class ChatService : IChatService
{
    private readonly IChatRepository _chatRepository;

    public ChatService(IChatRepository chatRepository)
    {
        _chatRepository = chatRepository;
    }

    public async Task<ChatConversationDto?> GetConversationAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default)
    {
        var conversation = await _chatRepository.GetConversationWithParticipantsAsync(conversationId, cancellationToken);
        if (conversation is null) return null;

        var participant = conversation.Participants.FirstOrDefault(p => p.UserId == userId);
        if (participant is null) return null;

        var unreadCount = await _chatRepository.GetUnreadCountAsync(conversationId, userId, cancellationToken);

        return ToConversationDto(conversation, unreadCount);
    }

    public async Task<ChatConversationDto> GetOrCreateSupportConversationAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var conversation = await _chatRepository.GetOrCreateSupportConversationAsync(customerId, cancellationToken);
        var unreadCount = await _chatRepository.GetUnreadCountAsync(conversation!.Id, customerId, cancellationToken);
        var fullConversation = await _chatRepository.GetConversationWithParticipantsAsync(conversation.Id, cancellationToken);
        return ToConversationDto(fullConversation!, unreadCount);
    }

    public async Task<PagedConversationsResultDto> GetConversationsAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var (items, total) = await _chatRepository.GetConversationsForUserAsync(userId, page, pageSize, cancellationToken);

        var dtos = new List<ChatConversationDto>();
        foreach (var conv in items)
        {
            var unreadCount = await _chatRepository.GetUnreadCountAsync(conv.Id, userId, cancellationToken);
            dtos.Add(ToConversationDto(conv, unreadCount));
        }

        var totalPages = (int)Math.Ceiling((double)total / pageSize);

        return new PagedConversationsResultDto(dtos, page, pageSize, total, totalPages);
    }

    public async Task<PagedConversationsResultDto> GetSupportConversationsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        // Trả về tất cả conversation SUPPORT đang ACTIVE.
        // Staff/admin cần thấy mọi hội thoại hỗ trợ, kể cả khi họ chưa
        // được thêm vào bảng ChatParticipant.
        var (items, total) = await _chatRepository.GetActiveSupportConversationsAsync(page, pageSize, cancellationToken);

        var dtos = new List<ChatConversationDto>();
        foreach (var conv in items)
        {
            // unreadCount cho staff xem = tổng tin nhắn chưa ai đọc
            // (đơn giản hoá: đếm tất cả message có ReadAt == null)
            var unreadCount = await _chatRepository.GetTotalUnreadCountAsync(conv.Id, cancellationToken);
            dtos.Add(ToConversationDto(conv, unreadCount));
        }

        var totalPages = (int)Math.Ceiling((double)total / pageSize);

        return new PagedConversationsResultDto(dtos, page, pageSize, total, totalPages);
    }

    public async Task<PagedMessagesResultDto> GetMessagesAsync(Guid conversationId, Guid userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        // Cho phép user xem messages nếu:
        // 1. Là participant của conversation, HOẶC
        // 2. Là conversation loại SUPPORT (staff/admin hỗ trợ khách hàng)
        var conversation = await _chatRepository.GetConversationByIdAsync(conversationId, cancellationToken);
        var isSupportConversation = conversation is not null && conversation.Type == "SUPPORT";
        var participant = await _chatRepository.GetParticipantAsync(conversationId, userId, cancellationToken);

        if (participant is null && !isSupportConversation)
            return new PagedMessagesResultDto([], page, pageSize, 0, 0);

        var (items, total) = await _chatRepository.GetMessagesAsync(conversationId, page, pageSize, cancellationToken);

        var dtos = items.Select(ToMessageDto).ToList();
        var totalPages = (int)Math.Ceiling((double)total / pageSize);

        return new PagedMessagesResultDto(dtos, page, pageSize, total, totalPages);
    }

    public async Task<ChatMessageDto> SendMessageAsync(Guid conversationId, Guid senderId, SendMessageRequestDto request, CancellationToken cancellationToken = default)
    {
        var conversation = await _chatRepository.GetConversationByIdAsync(conversationId, cancellationToken);
        if (conversation is null || conversation.Status == "CLOSED") throw new InvalidOperationException("Conversation is closed");

        // Cho phép gửi nếu:
        //  - user là participant của conv, HOẶC
        //  - conv thuộc loại SUPPORT (staff/admin hỗ trợ mọi hội thoại support).
        var isParticipant = await _chatRepository.GetParticipantAsync(conversationId, senderId, cancellationToken) is not null;
        if (!isParticipant && conversation.Type != "SUPPORT")
            throw new UnauthorizedAccessException("User is not a participant of this conversation");

        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            SenderId = senderId,
            Content = request.Content,
            Type = request.Type,
            SentAt = DateTime.UtcNow,
            ReplyToId = request.ReplyToId,
            AttachmentUrl = request.AttachmentUrl,
            AttachmentType = request.AttachmentType
        };

        await _chatRepository.AddMessageAsync(message, cancellationToken);
        await _chatRepository.SaveChangesAsync(cancellationToken);

        // Đảm bảo sender có 1 row participant (để lần sau GetConversations của
        // sender trả về conv này). Với staff/admin thì sẽ tạo thêm row ở
        // đây, hỗ trợ việc "đã phụ trách" hội thoại.
        if (!isParticipant)
        {
            var staffParticipant = new ChatParticipant
            {
                Id = Guid.NewGuid(),
                ConversationId = conversationId,
                UserId = senderId,
                Role = "SUPPORT",
                JoinedAt = DateTime.UtcNow
            };
            await _chatRepository.AddParticipantAsync(staffParticipant, cancellationToken);
            await _chatRepository.SaveChangesAsync(cancellationToken);
        }

        var fullMessage = await _chatRepository.GetMessageByIdAsync(message.Id, cancellationToken);
        return ToMessageDto(fullMessage!);
    }

    public async Task MarkMessagesAsReadAsync(Guid conversationId, Guid userId, Guid? lastMessageId = null, CancellationToken cancellationToken = default)
    {
        var participant = await _chatRepository.GetParticipantAsync(conversationId, userId, cancellationToken);
        if (participant is null) return;

        participant.LastReadAt = DateTime.UtcNow;
        await _chatRepository.UpdateParticipantAsync(participant, cancellationToken);
        await _chatRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<ChatConversationDto> CreateConversationAsync(Guid creatorId, CreateConversationRequestDto request, CancellationToken cancellationToken = default)
    {
        var conversation = new ChatConversation
        {
            Id = Guid.NewGuid(),
            Type = request.Type,
            Title = request.Title ?? (request.Type == "SUPPORT" ? "Hỗ trợ khách hàng" : null),
            Status = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        };

        await _chatRepository.AddConversationAsync(conversation, cancellationToken);

        var creatorParticipant = new ChatParticipant
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            UserId = creatorId,
            Role = creatorId == creatorId ? "CUSTOMER" : "SUPPORT",
            JoinedAt = DateTime.UtcNow
        };
        await _chatRepository.AddParticipantAsync(creatorParticipant, cancellationToken);

        if (request.ParticipantIds is not null)
        {
            foreach (var participantId in request.ParticipantIds.Where(id => id != creatorId))
            {
                var participant = new ChatParticipant
                {
                    Id = Guid.NewGuid(),
                    ConversationId = conversation.Id,
                    UserId = participantId,
                    Role = "SUPPORT",
                    JoinedAt = DateTime.UtcNow
                };
                await _chatRepository.AddParticipantAsync(participant, cancellationToken);
            }
        }

        await _chatRepository.SaveChangesAsync(cancellationToken);

        var fullConversation = await _chatRepository.GetConversationWithParticipantsAsync(conversation.Id, cancellationToken);
        return ToConversationDto(fullConversation!, 0);
    }

    public async Task<bool> IsUserParticipantAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default)
    {
        var participant = await _chatRepository.GetParticipantAsync(conversationId, userId, cancellationToken);
        return participant is not null;
    }

    public async Task<bool> IsSupportConversationAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        var conv = await _chatRepository.GetConversationByIdAsync(conversationId, cancellationToken);
        return conv is not null && conv.Type == "SUPPORT" && conv.Status == "ACTIVE";
    }

    public async Task<ChatConversationDto?> CloseConversationAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default)
    {
        var conversation = await _chatRepository.GetConversationByIdAsync(conversationId, cancellationToken);
        if (conversation is null) return null;

        conversation.Status = "CLOSED";
        conversation.ClosedAt = DateTime.UtcNow;

        await _chatRepository.UpdateConversationAsync(conversation, cancellationToken);
        await _chatRepository.SaveChangesAsync(cancellationToken);

        var fullConversation = await _chatRepository.GetConversationWithParticipantsAsync(conversationId, cancellationToken);
        var unreadCount = await _chatRepository.GetUnreadCountAsync(conversationId, userId, cancellationToken);
        return ToConversationDto(fullConversation!, unreadCount);
    }

    private static ChatConversationDto ToConversationDto(ChatConversation conversation, int unreadCount)
    {
        var lastMessage = conversation.Messages?.OrderByDescending(m => m.SentAt).FirstOrDefault();
        var participants = conversation.Participants?.Select(ToParticipantDto).ToList() ?? [];

        return new ChatConversationDto(
            conversation.Id,
            conversation.Type,
            conversation.Title,
            conversation.Status,
            conversation.CreatedAt,
            conversation.ClosedAt,
            participants,
            lastMessage is not null ? ToMessageDto(lastMessage) : null,
            unreadCount
        );
    }

    private static ChatParticipantDto ToParticipantDto(ChatParticipant participant)
    {
        return new ChatParticipantDto(
            participant.Id,
            participant.UserId,
            participant.User?.FullName ?? "Unknown",
            participant.User?.AvatarUrl,
            participant.Role,
            participant.JoinedAt,
            participant.LastReadAt
        );
    }

    private static ChatMessageDto ToMessageDto(ChatMessage message)
    {
        return new ChatMessageDto(
            message.Id,
            message.ConversationId,
            message.SenderId,
            message.Sender?.FullName ?? "Unknown",
            message.Sender?.AvatarUrl,
            message.Content,
            message.Type,
            message.SentAt,
            message.ReadAt,
            message.IsPinned,
            message.ReplyToId,
            message.ReplyTo?.Content,
            message.ReplyTo?.Sender?.FullName,
            message.AttachmentUrl,
            message.AttachmentType
        );
    }
}
