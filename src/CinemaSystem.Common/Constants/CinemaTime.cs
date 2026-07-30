namespace CinemaSystem.Common.Constants;

/// <summary>
/// Timezone helpers for the cinema domain.
/// Database values for <see cref="CinemaSystem.DAL.Models.Showtime.StartTime"/> /
/// <see cref="CinemaSystem.DAL.Models.Showtime.EndTime"/> / <see cref="CinemaSystem.DAL.Models.Ticket.ExpiredAt"/>
/// are persisted with <see cref="DateTimeKind.Unspecified"/> representing the local time of the cinema (Asia/Ho_Chi_Minh,
/// UTC+7). Use <see cref="ToUtc"/> before comparing with <see cref="DateTime.UtcNow"/>,
/// and <see cref="ToLocal"/> before returning values to the client.
/// </summary>
public static class CinemaTime
{
    public const string VietnamTimeZoneId = "SE Asia Standard Time"; // Windows id; IANA "Asia/Ho_Chi_Minh"

    private static readonly TimeZoneInfo VietnamTimeZone = ResolveVietnamTimeZone();

    private static TimeZoneInfo ResolveVietnamTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(VietnamTimeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.CreateCustomTimeZone("CinemaVN", TimeSpan.FromHours(7), "CinemaVN", "CinemaVN");
            }
        }
    }

    /// <summary>
    /// Converts a persisted local-VN datetime (Kind=Unspecified) to UTC for comparison with <see cref="DateTime.UtcNow"/>.
    /// If <paramref name="value"/> already has Kind=Utc it is returned unchanged.
    /// If Kind=Local it is converted to UTC.
    /// </summary>
    public static DateTime ToUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(value, DateTimeKind.Unspecified), VietnamTimeZone)
        };
    }

    /// <summary>
    /// Converts a persisted local-VN datetime to its representation in the Vietnam timezone, suitable for API responses.
    /// Returned as <see cref="DateTimeKind.Unspecified"/> with the local wall-clock values.
    /// </summary>
    public static DateTime ToLocal(DateTime value)
    {
        var local = value.Kind switch
        {
            DateTimeKind.Utc => TimeZoneInfo.ConvertTimeFromUtc(value, VietnamTimeZone),
            DateTimeKind.Local => TimeZoneInfo.ConvertTime(value, VietnamTimeZone),
            _ => value
        };
        return DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
    }
}
