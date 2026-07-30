using System.Security.Cryptography;

namespace CinemaSystem.Common.Helpers;

public static class QrTokenGenerator
{
    public static string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}