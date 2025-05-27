namespace CrowdQR.Api.Services;

/// <summary>
/// Interface for token generation and validation service.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates a random verification token.
    /// </summary>
    /// <returns>A random token string.</returns>
    string GenerateVerificationToken();

    /// <summary>
    /// Generates an expiry time for tokens.
    /// </summary>
    /// <param name="hours">The number of hours until expiry.</param>
    /// <returns>The expiry time.</returns>
    DateTime GenerateTokenExpiry(int hours = 24);
}