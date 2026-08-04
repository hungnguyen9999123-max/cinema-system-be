using System.Text.RegularExpressions;

namespace CinemaSystem.Services.Services.AiChat;

public static partial class IntentPatterns
{
    public static ChatIntent RecognizeIntent(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return ChatIntent.Unknown;

        var normalizedMessage = message.Trim().ToLowerInvariant();
        
        // DEBUG LOG
        Console.WriteLine($"[INTENT DEBUG] Message: '{message}' -> Normalized: '{normalizedMessage}'");
        Console.WriteLine($"[INTENT DEBUG] Showtime match: {ShowtimeRegex().IsMatch(normalizedMessage)}");
        Console.WriteLine($"[INTENT DEBUG] Movie match: {MovieRegex().IsMatch(normalizedMessage)}");

        // Farewell - check first to avoid misclassification
        if (FarewellRegex().IsMatch(normalizedMessage))
            return ChatIntent.Farewell;

        // Human Handoff - customer wants to talk to human
        if (HumanHandoffRegex().IsMatch(normalizedMessage))
            return ChatIntent.HumanHandoff;

        // Greeting
        if (GreetingRegex().IsMatch(normalizedMessage))
            return ChatIntent.Greeting;

        // Showtime - check BEFORE Movie because "xem lịch chiếu" matches both
        if (ShowtimeRegex().IsMatch(normalizedMessage))
            return ChatIntent.ShowtimeInquiry;

        // Movie Inquiry - check AFTER Showtime
        if (MovieRegex().IsMatch(normalizedMessage))
            return ChatIntent.MovieInquiry;

        // Booking Help
        if (BookingRegex().IsMatch(normalizedMessage))
            return ChatIntent.BookingHelp;

        // Price Inquiry
        if (PriceRegex().IsMatch(normalizedMessage))
            return ChatIntent.PriceInquiry;

        // F&B Inquiry
        if (FnbRegex().IsMatch(normalizedMessage))
            return ChatIntent.FnbInquiry;

        return ChatIntent.Unknown;
    }

    public static string ExtractMovieQuery(string message)
    {
        var normalizedMessage = message.Trim().ToLowerInvariant();
        
        // Remove intent keywords to get movie name
        var result = MovieQueryRemoveRegex().Replace(normalizedMessage, " ");
        result = IntentRemoveRegex().Replace(result, " ");
        
        // Clean up extra spaces
        result = MultipleSpacesRegex().Replace(result.Trim(), " ");
        
        return result;
    }

    [GeneratedRegex(@"^(bye|tạm\s*biệt|cảm\s*ơn|cảm\s*ơn\s*bạn|goodbye|see\s*you|thanks|thank\s*you|hẹn\s*gặp\s*lại)$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex FarewellRegex();

    [GeneratedRegex(@"(nhân\s*viên|staff|người\s*thật|người\s*thật|真人|real\s*person|talk\s*to\s*human|need\s*human|human\s*support|agent|live\s*chat|chat\s*với\s*người)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex HumanHandoffRegex();

    [GeneratedRegex(@"^(chào|xin\s*chào|hi|hello|hey|alo|chào\s+bạn|chào\s+buổi|good\s*morning|good\s*afternoon|good\s*evening)$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex GreetingRegex();

    [GeneratedRegex(@"(phim|nào|tìm|xem|hay|hot|mới|đang\s*chiếu|recommend|suggest|movie|film|thể\s*loại|genre)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex MovieRegex();

    [GeneratedRegex(@"(lịch\s*chiếu|giờ\s*chiếu|suất\s*chiếu|thời\s*gian\s*chiếu|chiếu\s*mấy\s*giờ|when\s*show|showtime|schedule)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex ShowtimeRegex();

    [GeneratedRegex(@"(đặt\s*vé|mua\s*vé|book\s*vé|làm\s*sao\s*đặt|hướng\s*dẫn\s*đặt|cách\s*đặt|how\s*to\s*book|book\s*ticket|booking)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex BookingRegex();

    [GeneratedRegex(@"(giá\s*vé|giá\s*bao\s*nhiêu|bao\s*nhiêu\s*tiền|price|cost|chi\s*phí|ticket\s*price)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex PriceRegex();

    [GeneratedRegex(@"(đồ\s*ăn|thức\s*ăn|bắp|nước|fnb|food|drink|beverage|combo|popcorn|nước\s*uống)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex FnbRegex();

    [GeneratedRegex(@"^(phim\s+|tìm\s+phim\s+|xem\s+phim\s+|phim\s+nào|xem\s+|tìm\s+|recommend\s+|suggest\s+)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex MovieQueryRemoveRegex();

    [GeneratedRegex(@"(phim|nào|tìm|xem|hay|hot|mới|đang\s*chiếu|recommend|suggest|show|me|tell|me|about)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex IntentRemoveRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex MultipleSpacesRegex();
}
