using System.Security.Claims;
using CinemaSystem.Common.DTOs.Chat;
using CinemaSystem.Services.Services.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CinemaSystem.API.Hubs;

[Authorize]
public sealed class ChatHub : Hub
{
    private readonly IChatService _chatService;
    private static readonly Dictionary<string, Guid> _connectionMapping = new();
    private static readonly Dictionary<Guid, HashSet<string>> _userConnections = new();

    public ChatHub(IChatService chatService)
    {
        _chatService = chatService;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetCurrentUserId();
        if (userId.HasValue)
        {
            _connectionMapping[Context.ConnectionId] = userId.Value;
            if (!_userConnections.ContainsKey(userId.Value))
                _userConnections[userId.Value] = new HashSet<string>();
            _userConnections[userId.Value].Add(Context.ConnectionId);

            await Clients.All.SendAsync("UserOnline", userId.Value);
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (_connectionMapping.TryGetValue(Context.ConnectionId, out var userId))
        {
            _connectionMapping.Remove(Context.ConnectionId);
            if (_userConnections.TryGetValue(userId, out var connections))
            {
                connections.Remove(Context.ConnectionId);
                if (connections.Count == 0)
                {
                    _userConnections.Remove(userId);
                    await Clients.All.SendAsync("UserOffline", userId);
                }
            }
        }
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinConversation(Guid conversationId)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return;

        // Cho phép join nếu:
        //  - user là participant, HOẶC
        //  - conv thuộc loại SUPPORT (staff/admin cần join mọi conv support
        //    để nhận realtime broadcast).
        var isParticipant = await _chatService.IsUserParticipantAsync(conversationId, userId.Value);
        if (!isParticipant)
        {
            var isSupport = await _chatService.IsSupportConversationAsync(conversationId);
            if (!isSupport) return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, conversationId.ToString());
        await Clients.Group(conversationId.ToString()).SendAsync("UserJoined", userId.Value);
    }

    public async Task LeaveConversation(Guid conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationId.ToString());
        var userId = GetCurrentUserId();
        if (userId.HasValue)
            await Clients.Group(conversationId.ToString()).SendAsync("UserLeft", userId.Value);
    }

    public async Task SendMessage(Guid conversationId, SendMessageRequestDto request)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return;

        try
        {
            var message = await _chatService.SendMessageAsync(conversationId, userId.Value, request);
            await Clients.Group(conversationId.ToString()).SendAsync("ReceiveMessage", message);

            var participants = await GetOtherParticipants(conversationId, userId.Value);
            foreach (var participantId in participants)
            {
                if (_userConnections.TryGetValue(participantId, out var connections))
                {
                    foreach (var connectionId in connections)
                    {
                        if (_connectionMapping.TryGetValue(connectionId, out var connectionUserId) && connectionUserId == participantId)
                        {
                            await Clients.Client(connectionId).SendAsync("NewMessageNotification", conversationId, message);
                        }
                    }
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            await Clients.Caller.SendAsync("Error", "You are not a participant of this conversation");
        }
        catch (InvalidOperationException ex)
        {
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    public async Task Typing(Guid conversationId)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return;

        var conversation = await _chatService.GetConversationAsync(conversationId, userId.Value);
        if (conversation is null) return;

        var participant = conversation.Participants.FirstOrDefault(p => p.UserId == userId.Value);
        await Clients.Group(conversationId.ToString()).SendAsync("UserTyping", userId.Value, participant?.FullName);
    }

    public async Task MarkAsRead(Guid conversationId, Guid lastMessageId)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return;

        await _chatService.MarkMessagesAsReadAsync(conversationId, userId.Value, lastMessageId);
        await Clients.Group(conversationId.ToString()).SendAsync("MessagesRead", userId.Value, lastMessageId);
    }

    public async Task JoinSupport()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return;

        await Groups.AddToGroupAsync(Context.ConnectionId, "support");
        await Clients.Group("support").SendAsync("SupportUserJoined", userId.Value);
    }

    public async Task LeaveSupport()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return;

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "support");
    }

    private Guid? GetCurrentUserId()
    {
        var claim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? Context.User?.FindFirst("sub")?.Value;

        if (Guid.TryParse(claim, out var userId))
            return userId;
        return null;
    }

    private async Task<IEnumerable<Guid>> GetOtherParticipants(Guid conversationId, Guid currentUserId)
    {
        var conversation = await _chatService.GetConversationAsync(conversationId, currentUserId);
        return conversation?.Participants.Where(p => p.UserId != currentUserId).Select(p => p.UserId) ?? [];
    }

    public static IReadOnlyList<string>? GetConnectionsForUser(Guid userId)
    {
        return _userConnections.TryGetValue(userId, out var connections) ? connections.ToList() : null;
    }

    public static bool IsUserOnline(Guid userId)
    {
        return _userConnections.ContainsKey(userId);
    }
}
