using CinemaSystem.Services.Services.AiChat;

namespace CinemaSystem.Services.Services.AiChat;

public interface IAiChatService
{
    Task<AiChatResponse> ProcessMessageAsync(AiChatRequest request, CancellationToken cancellationToken = default);
    
    ChatIntent RecognizeIntent(string message);
    
    Task<List<MovieSuggestionDto>> GetTrendingMoviesAsync(int count = 5, CancellationToken cancellationToken = default);
    
    Task<List<MovieSuggestionDto>> SearchMoviesAsync(string query, int count = 5, CancellationToken cancellationToken = default);
    
    Task<List<FnbItemDto>> GetActiveFnbItemsAsync(CancellationToken cancellationToken = default);
}
