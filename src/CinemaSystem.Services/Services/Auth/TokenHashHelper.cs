using System.Security.Cryptography;
using System.Text;

namespace CinemaSystem.Services.Services.Auth;

public static class TokenHashHelper
{
    public static string Sha256(string token)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
