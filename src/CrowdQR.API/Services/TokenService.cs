using System.Security.Cryptography;

namespace CrowdQR.Api.Services;

/// <summary>
/// Service for token generation and validation.
/// </summary>
public class TokenService : ITokenService
{
    /// <inheritdoc />
    public string GenerateVerificationToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(randomBytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .Replace("=", "");
    }

    /// <inheritdoc />
    public DateTime GenerateTokenExpiry(int hours = 24)
    {
        return DateTime.UtcNow.AddHours(hours);
    }
}