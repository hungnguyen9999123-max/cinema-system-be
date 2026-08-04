using System.Security.Claims;
using CinemaSystem.Common.DTOs.Chat;
using CinemaSystem.Common.DTOs.Responses;
using CinemaSystem.Services.Services.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaSystem.API.Controllers;

[ApiController]
[Route("api/chat")]
[Authorize]
public sealed class ChatController : ControllerBase
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
    {
        _chatService = chatService;
    }

    [HttpGet("conversations")]
    public async Task<ActionResult<ApiResponse<PagedConversationsResultDto>>> GetConversations(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<PagedConversationsResultDto>.Fail("Unauthorized"));

        var result = await _chatService.GetConversationsAsync(userId.Value, page, pageSize, cancellationToken);
        return Ok(ApiResponse.Ok(result));
    }

    /// <summary>
    /// Staff/admin xem tất cả conversation SUPPORT đang ACTIVE.
    /// Trả về inbox-style list, không phụ thuộc vào bảng ChatParticipant.
    /// </summary>
    [HttpGet("conversations/support-list")]
    public async Task<ActionResult<ApiResponse<PagedConversationsResultDto>>> GetSupportConversations(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<PagedConversationsResultDto>.Fail("Unauthorized"));

        var result = await _chatService.GetSupportConversationsAsync(page, pageSize, cancellationToken);
        return Ok(ApiResponse.Ok(result));
    }

    [HttpGet("conversations/support")]
    public async Task<ActionResult<ApiResponse<ChatConversationDto>>> GetOrCreateSupportConversation(
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<ChatConversationDto>.Fail("Unauthorized"));

        var result = await _chatService.GetOrCreateSupportConversationAsync(userId.Value, cancellationToken);
        return Ok(ApiResponse.Ok(result));
    }

    [HttpGet("conversations/{conversationId:guid}")]
    public async Task<ActionResult<ApiResponse<ChatConversationDto>>> GetConversation(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<ChatConversationDto>.Fail("Unauthorized"));

        var result = await _chatService.GetConversationAsync(conversationId, userId.Value, cancellationToken);
        if (result is null)
            return NotFound(ApiResponse<ChatConversationDto>.Fail("Conversation not found or access denied"));

        return Ok(ApiResponse.Ok(result));
    }

    [HttpPost("conversations")]
    public async Task<ActionResult<ApiResponse<ChatConversationDto>>> CreateConversation(
        [FromBody] CreateConversationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<ChatConversationDto>.Fail("Unauthorized"));

        var result = await _chatService.CreateConversationAsync(userId.Value, request, cancellationToken);
        return Created(string.Empty, ApiResponse.Ok(result));
    }

    [HttpPost("conversations/{conversationId:guid}/close")]
    public async Task<ActionResult<ApiResponse<ChatConversationDto>>> CloseConversation(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<ChatConversationDto>.Fail("Unauthorized"));

        var result = await _chatService.CloseConversationAsync(conversationId, userId.Value, cancellationToken);
        if (result is null)
            return NotFound(ApiResponse<ChatConversationDto>.Fail("Conversation not found or access denied"));

        return Ok(ApiResponse.Ok(result));
    }

    [HttpGet("conversations/{conversationId:guid}/messages")]
    public async Task<ActionResult<ApiResponse<PagedMessagesResultDto>>> GetMessages(
        Guid conversationId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<PagedMessagesResultDto>.Fail("Unauthorized"));

        // Cho phép staff/admin xem message của bất kỳ SUPPORT conv nào.
        var isSupport = await _chatService.IsSupportConversationAsync(conversationId, cancellationToken);
        var isParticipant = !isSupport
            ? await _chatService.IsUserParticipantAsync(conversationId, userId.Value, cancellationToken)
            : true;
        if (!isParticipant)
            return Forbid();

        var result = await _chatService.GetMessagesAsync(conversationId, userId.Value, page, pageSize, cancellationToken);
        return Ok(ApiResponse.Ok(result));
    }

    [HttpPost("conversations/{conversationId:guid}/messages")]
    public async Task<ActionResult<ApiResponse<ChatMessageDto>>> SendMessage(
        Guid conversationId,
        [FromBody] SendMessageRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<ChatMessageDto>.Fail("Unauthorized"));

        try
        {
            // Staff/admin được phép gửi message vào bất kỳ SUPPORT conv nào,
            // không yêu cầu có row trong ChatParticipant.
            var isSupport = await _chatService.IsSupportConversationAsync(conversationId, cancellationToken);
            if (!isSupport)
            {
                var isParticipant = await _chatService.IsUserParticipantAsync(conversationId, userId.Value, cancellationToken);
                if (!isParticipant)
                    return Forbid();
            }

            var result = await _chatService.SendMessageAsync(conversationId, userId.Value, request, cancellationToken);
            return Ok(ApiResponse.Ok(result));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<ChatMessageDto>.Fail(ex.Message));
        }
    }

    [HttpPost("conversations/{conversationId:guid}/read")]
    public async Task<ActionResult<ApiResponse<string>>> MarkAsRead(
        Guid conversationId,
        [FromBody] MarkMessagesReadRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<string>.Fail("Unauthorized"));

        await _chatService.MarkMessagesAsReadAsync(conversationId, userId.Value, request.LastMessageId, cancellationToken);
        return Ok(ApiResponse.Ok("Messages marked as read"));
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (Guid.TryParse(claim, out var userId))
            return userId;
        return null;
    }
}
