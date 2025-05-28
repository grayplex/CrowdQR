using CrowdQR.Api.Models;
using CrowdQR.Shared.Models.DTOs;

namespace CrowdQR.Api.Services;

/// <summary>
/// Interface for authentication service.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Authenticates a user by username or email and password.
    /// </summary>
    /// <param name="usernameOrEmail">The username or email to authenticate.</param>
    /// <param name="password">The password to verify (optional for audience users).</param>
    /// <returns>Authentication result with JWT token if successful, or error.</returns>
    Task<AuthResultDto> AuthenticateUser(string usernameOrEmail, string? password = null);

    /// <summary>
    /// Registers a new DJ user.
    /// </summary>
    /// <param name="registerDto">The registration data.</param>
    /// <returns>Registration result.</returns>
    Task<AuthResultDto> RegisterDj(AuthDjRegisterDto registerDto);

    /// <summary>
    /// Verifies a user's email.
    /// </summary>
    /// <param name="verifyEmailDto">The email verification data.</param>
    /// <returns>True if verification was successful, false otherwise.</returns>
    Task<bool> VerifyEmail(AuthVerifyEmailDto verifyEmailDto);

    /// <summary>
    /// Resends an email verification token.
    /// </summary>
    /// <param name="email">The email address.</param>
    /// <returns>True if successful, false otherwise.</returns>
    Task<bool> ResendVerificationEmail(string email);

    /// <summary>
    /// Validates JWT token and returns the associated user.
    /// </summary>
    /// <param name="token">The JWT token to validate.</param>
    /// <returns>The user if token is valid, null otherwise.</returns>
    Task<User?> ValidateToken(string token);

    /// <summary>
    /// Changes a user's password.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="currentPassword">The current password.</param>
    /// <param name="newPassword">The new password.</param>
    /// <returns>True if successful, false otherwise.</returns>
    Task<bool> ChangePassword(int userId, string currentPassword, string newPassword);

    /// <summary>
    /// Checks if a user can access a specific event.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <returns>True if the user can access the event, false otherwise.</returns>
    Task<bool> CanAccessEventAsync(int eventId, int userId);

    /// <summary>
    /// Checks if a user can modify a specific request.
    /// </summary>
    /// <param name="requestId">The request ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <returns>True if the user can modify the request, false otherwise.</returns>
    Task<bool> CanModifyRequestAsync(int requestId, int userId);

    /// <summary>
    /// Checks if a user is a DJ.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>True if the user is a DJ, false otherwise.</returns>
    Task<bool> IsDjAsync(int userId);

    /// <summary>
    /// Checks if a user needs to verify their email.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>True if the user needs to verify their email, false otherwise.</returns>
    Task<bool> NeedsEmailVerificationAsync(int userId);
}