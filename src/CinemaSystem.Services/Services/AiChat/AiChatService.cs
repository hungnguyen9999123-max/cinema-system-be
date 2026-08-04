using CinemaSystem.DAL.Interfaces;
using CinemaSystem.Services.Services.Movies;
using Microsoft.EntityFrameworkCore;

namespace CinemaSystem.Services.Services.AiChat;

public sealed class AiChatService : IAiChatService
{
    private readonly IMovieRepository _movieRepository;
    private readonly IShowtimeRepository _showtimeRepository;
    private readonly IMovieService _movieService;
    private readonly IFnbItemRepository _fnbItemRepository;

    public AiChatService(
        IMovieRepository movieRepository,
        IShowtimeRepository showtimeRepository,
        IMovieService movieService,
        IFnbItemRepository fnbItemRepository)
    {
        _movieRepository = movieRepository;
        _showtimeRepository = showtimeRepository;
        _movieService = movieService;
        _fnbItemRepository = fnbItemRepository;
    }

    public async Task<AiChatResponse> ProcessMessageAsync(AiChatRequest request, CancellationToken cancellationToken = default)
    {
        var intent = RecognizeIntent(request.Message);
        var response = intent switch
        {
            ChatIntent.Greeting => await HandleGreetingAsync(request, cancellationToken),
            ChatIntent.Farewell => HandleFarewell(),
            ChatIntent.HumanHandoff => HandleHumanHandoff(),
            ChatIntent.MovieInquiry => await HandleMovieInquiryAsync(request.Message, cancellationToken),
            ChatIntent.ShowtimeInquiry => await HandleShowtimeInquiryAsync(request.Message, cancellationToken),
            ChatIntent.BookingHelp => HandleBookingHelp(),
            ChatIntent.PriceInquiry => await HandlePriceInquiryAsync(cancellationToken),
            ChatIntent.FnbInquiry => await HandleFnbInquiryAsync(cancellationToken),
            _ => await HandleUnknownAsync(request.Message, cancellationToken)
        };

        return response;
    }

    public ChatIntent RecognizeIntent(string message)
    {
        return IntentPatterns.RecognizeIntent(message);
    }

    public async Task<List<MovieSuggestionDto>> GetTrendingMoviesAsync(int count = 5, CancellationToken cancellationToken = default)
    {
        var movies = await _movieRepository.Query()
            .AsNoTracking()
            .Where(m => m.Status == "NOW_SHOWING")
            .OrderByDescending(m => m.ReleaseDate)
            .Take(count)
            .Select(m => new MovieSuggestionDto(
                m.Id,
                m.Title,
                m.PosterUrl,
                m.Genre,
                m.Status,
                m.DurationMin))
            .ToListAsync(cancellationToken);

        return movies;
    }

    public async Task<List<MovieSuggestionDto>> SearchMoviesAsync(string query, int count = 5, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return await GetTrendingMoviesAsync(count, cancellationToken);
        }

        var normalizedQuery = query.ToLowerInvariant();

        var movies = await _movieRepository.Query()
            .AsNoTracking()
            .Where(m => 
                m.Title.ToLower().Contains(normalizedQuery) ||
                m.Genre.ToLower().Contains(normalizedQuery))
            .OrderByDescending(m => m.Status == "NOW_SHOWING" ? 1 : 0)
            .ThenByDescending(m => m.ReleaseDate)
            .Take(count)
            .Select(m => new MovieSuggestionDto(
                m.Id,
                m.Title,
                m.PosterUrl,
                m.Genre,
                m.Status,
                m.DurationMin))
            .ToListAsync(cancellationToken);

        return movies;
    }

    public async Task<List<FnbItemDto>> GetActiveFnbItemsAsync(CancellationToken cancellationToken = default)
    {
        var items = await _fnbItemRepository.Query()
            .AsNoTracking()
            .Where(item => item.Status == "ACTIVE")
            .OrderBy(item => item.Category)
            .ThenBy(item => item.Price)
            .Select(item => new FnbItemDto(
                item.Id,
                item.Name,
                item.Category,
                item.Description,
                item.Price,
                item.ImageUrl))
            .ToListAsync(cancellationToken);

        return items;
    }

    private async Task<AiChatResponse> HandleGreetingAsync(AiChatRequest request, CancellationToken cancellationToken)
    {
        var trendingMovies = await GetTrendingMoviesAsync(3, cancellationToken);
        var message = AiResponseTemplates.GetGreetingResponse();

        if (trendingMovies.Count > 0)
        {
            var topMovie = trendingMovies.First();
            message += $"\n\n🔥 Phim hot hiện tại: **{topMovie.Title}**";
        }

        return new AiChatResponse(message, ChatIntent.Greeting, null, null, false);
    }

    private AiChatResponse HandleFarewell()
    {
        var message = AiResponseTemplates.GetFarewellResponse();
        return new AiChatResponse(message, ChatIntent.Farewell, null, null, false);
    }

    private AiChatResponse HandleHumanHandoff()
    {
        var message = AiResponseTemplates.GetHumanHandoffResponse();
        return new AiChatResponse(message, ChatIntent.HumanHandoff, null, null, true);
    }

    private async Task<AiChatResponse> HandleMovieInquiryAsync(string message, CancellationToken cancellationToken)
    {
        var query = IntentPatterns.ExtractMovieQuery(message);

        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
        {
            var trendingMovies = await GetTrendingMoviesAsync(5, cancellationToken);
            var responseMessage = trendingMovies.Count > 0
                ? "Đây là các phim đang chiếu hot nhất:\n\n"
                : "Hiện tại không có phim nào đang chiếu.";

            return new AiChatResponse(responseMessage, ChatIntent.MovieInquiry, trendingMovies, null, false);
        }

        var movies = await SearchMoviesAsync(query, 5, cancellationToken);

        string responseMessage2;
        if (movies.Count == 0)
        {
            responseMessage2 = $"Mình không tìm thấy phim nào liên quan đến \"{query}\". Bạn thử tìm từ khóa khác xem sao?";
        }
        else if (movies.Count == 1)
        {
            var movie = movies.First();
            responseMessage2 = $"Tìm thấy phim: **{movie.Title}**!\n\n📽️ Thể loại: {movie.Genre}\n⏱️ Thời lượng: {movie.DurationMin} phút\n\nBạn muốn xem lịch chiếu của phim này không?";
        }
        else
        {
            responseMessage2 = $"Tìm thấy {movies.Count} phim liên quan đến \"{query}\":";
        }

        return new AiChatResponse(responseMessage2, ChatIntent.MovieInquiry, movies, null, false);
    }

    private async Task<AiChatResponse> HandleShowtimeInquiryAsync(string message, CancellationToken cancellationToken)
    {
        var query = IntentPatterns.ExtractMovieQuery(message);

        if (string.IsNullOrWhiteSpace(query))
        {
            var trendingMovies = await GetTrendingMoviesAsync(3, cancellationToken);
            var showtimes = new List<ShowtimeSuggestionDto>();

            foreach (var movie in trendingMovies.Take(2))
            {
                var movieShowtimes = await _showtimeRepository.Query()
                    .AsNoTracking()
                    .Where(s => s.MovieId == movie.Id && s.StartTime >= DateTime.UtcNow)
                    .OrderBy(s => s.StartTime)
                    .Take(2)
                    .Select(s => new ShowtimeSuggestionDto(
                        s.MovieId,
                        movie.Title,
                        s.StartTime,
                        s.Room != null ? s.Room.Name : "Phòng chiếu",
                        s.Room != null && s.Room.Cinema != null ? s.Room.Cinema.Name : "Rạp"))
                    .ToListAsync(cancellationToken);

                showtimes.AddRange(movieShowtimes);
            }

            if (showtimes.Count == 0)
            {
                return new AiChatResponse(
                    "Hiện tại chưa có suất chiếu nào trong thời gian tới. Bạn có thể xem phim sắp ra mắt nhé!",
                    ChatIntent.ShowtimeInquiry,
                    null, null,
                    false);
            }

            var responseMessage = AiResponseTemplates.GetShowtimeResponse(showtimes);
            return new AiChatResponse(responseMessage, ChatIntent.ShowtimeInquiry, null, showtimes, false);
        }

        var movies = await SearchMoviesAsync(query, 1, cancellationToken);

        if (movies.Count == 0)
        {
            return new AiChatResponse(
                $"Mình không tìm thấy phim nào liên quan đến \"{query}\" để xem lịch chiếu.",
                ChatIntent.ShowtimeInquiry,
                null, null,
                false);
        }

        var movieId = movies.First().Id;
        var specificMovieShowtimes = await _showtimeRepository.Query()
            .AsNoTracking()
            .Where(s => s.MovieId == movieId && s.StartTime >= DateTime.UtcNow)
            .OrderBy(s => s.StartTime)
            .Take(6)
            .Select(s => new ShowtimeSuggestionDto(
                s.MovieId,
                movies.First().Title,
                s.StartTime,
                s.Room != null ? s.Room.Name : "Phòng chiếu",
                s.Room != null && s.Room.Cinema != null ? s.Room.Cinema.Name : "Rạp"))
            .ToListAsync(cancellationToken);

        var showtimeMessage = specificMovieShowtimes.Count > 0
            ? AiResponseTemplates.GetShowtimeResponse(specificMovieShowtimes)
            : $"Hiện tại chưa có lịch chiếu cho phim \"{movies.First().Title}\" trong thời gian tới.";

        return new AiChatResponse(showtimeMessage, ChatIntent.ShowtimeInquiry, null, specificMovieShowtimes, false);
    }

    private AiChatResponse HandleBookingHelp()
    {
        var message = AiResponseTemplates.GetBookingHelpResponse();
        return new AiChatResponse(message, ChatIntent.BookingHelp, null, null, false);
    }

    private async Task<AiChatResponse> HandlePriceInquiryAsync(CancellationToken cancellationToken)
    {
        var fnbItems = await GetActiveFnbItemsAsync(cancellationToken);
        var message = AiResponseTemplates.GetPriceResponse(fnbItems);
        return new AiChatResponse(message, ChatIntent.PriceInquiry, null, null, false);
    }

    private async Task<AiChatResponse> HandleFnbInquiryAsync(CancellationToken cancellationToken)
    {
        var fnbItems = await GetActiveFnbItemsAsync(cancellationToken);
        var message = AiResponseTemplates.GetFnbResponse(fnbItems);
        return new AiChatResponse(message, ChatIntent.FnbInquiry, null, null, false);
    }

    private async Task<AiChatResponse> HandleUnknownAsync(string message, CancellationToken cancellationToken)
    {
        var suggestions = await GetTrendingMoviesAsync(3, cancellationToken);
        var responseMessage = AiResponseTemplates.GetFallbackResponse(suggestions);
        return new AiChatResponse(responseMessage, ChatIntent.Unknown, suggestions, null, false);
    }
}
