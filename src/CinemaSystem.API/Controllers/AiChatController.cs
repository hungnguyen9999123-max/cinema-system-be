using System.Security.Claims;
using CinemaSystem.Common.DTOs.Responses;
using CinemaSystem.Services.Services.AiChat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaSystem.API.Controllers;

[ApiController]
[Route("api/chat")]
[Authorize]
public sealed class AiChatController : ControllerBase
{
    private readonly IAiChatService _aiChatService;
    private readonly ILogger<AiChatController> _logger;

    public AiChatController(
        IAiChatService aiChatService,
        ILogger<AiChatController> logger)
    {
        _aiChatService = aiChatService;
        _logger = logger;
    }

    [HttpPost("ai/respond")]
    public async Task<ActionResult<ApiResponse<AiChatResponse>>> GetAiResponse(
        [FromBody] AiChatRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();

        _logger.LogInformation("[AI Chat] User {UserId} sent message: {Message}", userId, request.Message);

        try
        {
            var aiRequest = new AiChatRequest(
                request.Message,
                request.ConversationId,
                userId,
                request.Language ?? "vi");

            var response = await _aiChatService.ProcessMessageAsync(aiRequest, cancellationToken);

            _logger.LogInformation(
                "[AI Chat] Intent: {Intent}, RequiresHumanHandoff: {Handoff}",
                response.Intent,
                response.RequiresHumanHandoff);

            return Ok(ApiResponse.Ok(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AI Chat] Error processing message for user {UserId}", userId);

            // Return a friendly error message
            var errorResponse = new AiChatResponse(
                "Xin lỗi, mình đang gặp chút trục trặc. Bạn có thể thử lại sau không?",
                ChatIntent.Unknown,
                null,
                null,
                true);

            return Ok(ApiResponse.Ok(errorResponse));
        }
    }

    [HttpGet("ai/movies")]
    public async Task<ActionResult<ApiResponse<List<MovieSuggestionDto>>>> GetTrendingMovies(
        [FromQuery] int count = 5,
        CancellationToken cancellationToken = default)
    {
        var movies = await _aiChatService.GetTrendingMoviesAsync(count, cancellationToken);
        return Ok(ApiResponse.Ok(movies));
    }

    [HttpGet("ai/movies/search")]
    public async Task<ActionResult<ApiResponse<List<MovieSuggestionDto>>>> SearchMovies(
        [FromQuery] string query,
        [FromQuery] int count = 5,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest(ApiResponse<List<MovieSuggestionDto>>.Fail("Query is required"));
        }

        var movies = await _aiChatService.SearchMoviesAsync(query, count, cancellationToken);
        return Ok(ApiResponse.Ok(movies));
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

public sealed class AiChatRequestDto
{
    public string Message { get; init; } = string.Empty;
    public Guid ConversationId { get; init; }
    public string? Language { get; init; }
}
