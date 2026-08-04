using CinemaSystem.DAL.Interfaces;
using CinemaSystem.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaSystem.DAL.Repository.Chat;

public sealed class ChatRepository : IChatRepository
{
    private readonly CinemaDbContext _dbContext;

    public ChatRepository(CinemaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<ChatConversation?> GetConversationByIdAsync(Guid conversationId, CancellationToken cancellationToken = default) =>
        _dbContext.ChatConversations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);

    public Task<ChatConversation?> GetConversationWithParticipantsAsync(Guid conversationId, CancellationToken cancellationToken = default) =>
        _dbContext.ChatConversations
            .AsNoTracking()
            .Include(c => c.Participants)
                .ThenInclude(p => p.User)
            .Include(c => c.Messages)
                .ThenInclude(m => m.Sender)
            .FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);

    public async Task<ChatConversation?> GetOrCreateSupportConversationAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var conversation = await _dbContext.ChatConversations
            .Include(c => c.Participants)
            .Where(c => c.Type == "SUPPORT" && c.Status == "ACTIVE")
            .Where(c => c.Participants.Any(p => p.UserId == customerId && p.Role == "CUSTOMER"))
            .FirstOrDefaultAsync(cancellationToken);

        if (conversation is not null) return conversation;

        conversation = new ChatConversation
        {
            Id = Guid.NewGuid(),
            Type = "SUPPORT",
            Title = "Hỗ trợ khách hàng",
            Status = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        };

        await _dbContext.ChatConversations.AddAsync(conversation, cancellationToken);

        var customerParticipant = new ChatParticipant
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            UserId = customerId,
            Role = "CUSTOMER",
            JoinedAt = DateTime.UtcNow
        };
        await _dbContext.ChatParticipants.AddAsync(customerParticipant, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return conversation;
    }

    public async Task<(IReadOnlyList<ChatConversation> Items, int TotalCount)> GetConversationsForUserAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = _dbContext.ChatConversations
            .AsNoTracking()
            .Include(c => c.Participants)
                .ThenInclude(p => p.User)
            .Include(c => c.Messages.OrderByDescending(m => m.SentAt).Take(1))
                .ThenInclude(m => m.Sender)
            .Where(c => c.Participants.Any(p => p.UserId == userId))
            .OrderByDescending(c => c.Messages.Max(m => m.SentAt));

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<(IReadOnlyList<ChatConversation> Items, int TotalCount)> GetActiveSupportConversationsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        // Lấy tất cả conversation SUPPORT đang ACTIVE, bất kể staff đã là
        // participant hay chưa. Staff cần inbox-style view của tất cả hội
        // thoại hỗ trợ.
        var baseQuery = _dbContext.ChatConversations
            .AsNoTracking()
            .Include(c => c.Participants)
                .ThenInclude(p => p.User)
            .Include(c => c.Messages.OrderByDescending(m => m.SentAt).Take(1))
                .ThenInclude(m => m.Sender)
            .Where(c => c.Type == "SUPPORT" && c.Status == "ACTIVE");

        var total = await baseQuery.CountAsync(cancellationToken);
        var items = await baseQuery
            .OrderByDescending(c => c.Messages.Any()
                ? c.Messages.Max(m => m.SentAt)
                : c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public Task<int> GetTotalUnreadCountAsync(Guid conversationId, CancellationToken cancellationToken = default) =>
        _dbContext.ChatMessages
            .CountAsync(m => m.ConversationId == conversationId && m.ReadAt == null, cancellationToken);

    public Task<ChatMessage?> GetMessageByIdAsync(Guid messageId, CancellationToken cancellationToken = default) =>
        _dbContext.ChatMessages
            .AsNoTracking()
            .Include(m => m.Sender)
            .Include(m => m.ReplyTo)
            .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);

    public async Task<(IReadOnlyList<ChatMessage> Items, int TotalCount)> GetMessagesAsync(Guid conversationId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = _dbContext.ChatMessages
            .AsNoTracking()
            .Include(m => m.Sender)
            .Include(m => m.ReplyTo)
                .ThenInclude(r => r!.Sender)
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.SentAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items.OrderBy(m => m.SentAt).ToList(), total);
    }

    public Task<ChatParticipant?> GetParticipantAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default) =>
        _dbContext.ChatParticipants
            .AsNoTracking()
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.ConversationId == conversationId && p.UserId == userId, cancellationToken);

    public Task<int> GetUnreadCountAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default) =>
        _dbContext.ChatMessages
            .CountAsync(m => m.ConversationId == conversationId
                && m.SenderId != userId
                && (m.ReadAt == null || m.SentAt > _dbContext.ChatParticipants
                    .Where(p => p.ConversationId == conversationId && p.UserId == userId)
                    .Select(p => p.LastReadAt ?? DateTime.MinValue)
                    .FirstOrDefault()), cancellationToken);

    public Task AddConversationAsync(ChatConversation conversation, CancellationToken cancellationToken = default) =>
        _dbContext.ChatConversations.AddAsync(conversation, cancellationToken).AsTask();

    public Task AddMessageAsync(ChatMessage message, CancellationToken cancellationToken = default) =>
        _dbContext.ChatMessages.AddAsync(message, cancellationToken).AsTask();

    public Task AddParticipantAsync(ChatParticipant participant, CancellationToken cancellationToken = default) =>
        _dbContext.ChatParticipants.AddAsync(participant, cancellationToken).AsTask();

    public Task UpdateParticipantAsync(ChatParticipant participant, CancellationToken cancellationToken = default)
    {
        if (_dbContext.Entry(participant).State == EntityState.Detached)
            _dbContext.ChatParticipants.Update(participant);
        return Task.CompletedTask;
    }

    public Task UpdateConversationAsync(ChatConversation conversation, CancellationToken cancellationToken = default)
    {
        if (_dbContext.Entry(conversation).State == EntityState.Detached)
            _dbContext.ChatConversations.Update(conversation);
        return Task.CompletedTask;
    }

    public Task UpdateMessageAsync(ChatMessage message, CancellationToken cancellationToken = default)
    {
        if (_dbContext.Entry(message).State == EntityState.Detached)
            _dbContext.ChatMessages.Update(message);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => _dbContext.SaveChangesAsync(cancellationToken);
}
