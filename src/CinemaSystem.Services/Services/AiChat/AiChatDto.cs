namespace CinemaSystem.Services.Services.AiChat;

public enum ChatIntent
{
    Unknown,
    Greeting,
    MovieInquiry,
    ShowtimeInquiry,
    BookingHelp,
    PriceInquiry,
    FnbInquiry,
    HumanHandoff,
    Farewell
}

public sealed record AiChatRequest(
    string Message,
    Guid ConversationId,
    Guid? UserId,
    string? Language = "vi");

public sealed record AiChatResponse(
    string Message,
    ChatIntent Intent,
    List<MovieSuggestionDto>? MovieSuggestions,
    List<ShowtimeSuggestionDto>? ShowtimeSuggestions,
    bool RequiresHumanHandoff);

public sealed record MovieSuggestionDto(
    Guid Id,
    string Title,
    string? PosterUrl,
    string Genre,
    string Status,
    int DurationMin);

public sealed record FnbItemDto(
    Guid Id,
    string Name,
    string Category,
    string? Description,
    decimal Price,
    string? ImageUrl);

public sealed record ShowtimeSuggestionDto(
    Guid MovieId,
    string MovieTitle,
    DateTime ShowDateTime,
    string RoomName,
    string CinemaName);
