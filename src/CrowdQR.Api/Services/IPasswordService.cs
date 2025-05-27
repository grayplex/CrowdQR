namespace CrowdQR.Api.Services;

/// <summary>
/// Interface for password hashing and verification service.
/// </summary>
public interface IPasswordService
{
    /// <summary>
    /// Hashes a password with a randomly generated salt.
    /// </summary>
    /// <param name="password">The password to hash.</param>
    /// <returns>A tuple containing the password hash and salt.</returns>
    (string Hash, string Salt) HashPassword(string password);

    /// <summary>
    /// Hashes a password with a specified salt.
    /// </summary>
    /// <param name="password">The password to hash.</param>
    /// <param name="salt">The salt to use for hashing.</param>
    /// <returns>The hashed password.</returns>
    string HashPassword(string password, string salt);

    /// <summary>
    /// Verifies a password against a stored hash and salt.
    /// </summary>
    /// <param name="password">The password to verify.</param>
    /// <param name="hash">The stored password hash.</param>
    /// <param name="salt">The stored salt.</param>
    /// <returns>True if the password is valid, false otherwise.</returns>
    bool VerifyPassword(string password, string hash, string salt);
}