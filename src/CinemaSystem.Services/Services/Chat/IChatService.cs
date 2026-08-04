using CinemaSystem.Common.DTOs.Chat;
using CinemaSystem.Common;

namespace CinemaSystem.Services.Services.Chat;

public interface IChatService
{
    Task<ChatConversationDto?> GetConversationAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default);
    Task<ChatConversationDto> GetOrCreateSupportConversationAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<PagedConversationsResultDto> GetConversationsAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedConversationsResultDto> GetSupportConversationsAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedMessagesResultDto> GetMessagesAsync(Guid conversationId, Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ChatMessageDto> SendMessageAsync(Guid conversationId, Guid senderId, SendMessageRequestDto request, CancellationToken cancellationToken = default);
    Task MarkMessagesAsReadAsync(Guid conversationId, Guid userId, Guid? lastMessageId = null, CancellationToken cancellationToken = default);
    Task<ChatConversationDto> CreateConversationAsync(Guid creatorId, CreateConversationRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> IsUserParticipantAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> IsSupportConversationAsync(Guid conversationId, CancellationToken cancellationToken = default);
    Task<ChatConversationDto?> CloseConversationAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default);
}
