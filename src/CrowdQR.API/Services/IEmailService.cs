namespace CrowdQR.Api.Services;

/// <summary>
/// Interface for email sending service.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an email verification message.
    /// </summary>
    /// <param name="email">The recipient email address.</param>
    /// <param name="username">The username.</param>
    /// <param name="token">The verification token.</param>
    /// <returns>True if the email was sent successfully, false otherwise.</returns>
    Task<bool> SendVerificationEmailAsync(string email, string username, string token);

    /// <summary>
    /// Sends a password reset email.
    /// </summary>
    /// <param name="email">The recipient email address.</param>
    /// <param name="username">The username.</param>
    /// <param name="token">The password reset token.</param>
    /// <returns>True if the email was sent successfully, false otherwise.</returns>
    Task<bool> SendPasswordResetEmailAsync(string email, string username, string token);
}