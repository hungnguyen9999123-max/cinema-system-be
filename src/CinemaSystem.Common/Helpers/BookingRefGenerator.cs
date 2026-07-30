using System.Security.Cryptography;

namespace CinemaSystem.Common.Helpers;

public static class BookingRefGenerator
{
    public static string Generate(DateTime utcNow)
    {
        var suffix = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        return $"BK{utcNow:yyyyMMdd}{suffix}";
    }
}