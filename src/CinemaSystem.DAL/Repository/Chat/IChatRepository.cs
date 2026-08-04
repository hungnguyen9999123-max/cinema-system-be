using CinemaSystem.DAL.Models;

namespace CinemaSystem.DAL.Repository.Chat;

public interface IChatRepository
{
    Task<ChatConversation?> GetConversationByIdAsync(Guid conversationId, CancellationToken cancellationToken = default);
    Task<ChatConversation?> GetConversationWithParticipantsAsync(Guid conversationId, CancellationToken cancellationToken = default);
    Task<ChatConversation?> GetOrCreateSupportConversationAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<ChatConversation> Items, int TotalCount)> GetConversationsForUserAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<ChatConversation> Items, int TotalCount)> GetActiveSupportConversationsAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetTotalUnreadCountAsync(Guid conversationId, CancellationToken cancellationToken = default);
    Task<ChatMessage?> GetMessageByIdAsync(Guid messageId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<ChatMessage> Items, int TotalCount)> GetMessagesAsync(Guid conversationId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ChatParticipant?> GetParticipantAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default);
    Task AddConversationAsync(ChatConversation conversation, CancellationToken cancellationToken = default);
    Task AddMessageAsync(ChatMessage message, CancellationToken cancellationToken = default);
    Task AddParticipantAsync(ChatParticipant participant, CancellationToken cancellationToken = default);
    Task UpdateParticipantAsync(ChatParticipant participant, CancellationToken cancellationToken = default);
    Task UpdateConversationAsync(ChatConversation conversation, CancellationToken cancellationToken = default);
    Task UpdateMessageAsync(ChatMessage message, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
