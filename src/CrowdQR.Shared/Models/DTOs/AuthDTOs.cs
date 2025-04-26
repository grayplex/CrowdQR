using System.ComponentModel.DataAnnotations;

namespace CrowdQR.Shared.Models.DTOs;

/// <summary>
/// Data transfer object for login requests.
/// </summary>
public class LoginDto
{
    /// <summary>
    /// Gets or sets the username or email for login.
    /// </summary>
    [Required]
    public required string UsernameOrEmail { get; set; }

    /// <summary>
    /// Gets or sets the password for login.
    /// </summary>
    public string? Password { get; set; }
}

/// <summary>
/// Data transfer object for DJ registration requests.
/// </summary>
public class DjRegisterDto
{
    /// <summary>
    /// Gets or sets the username for registration.
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 3)]
    public required string Username { get; set; }

    /// <summary>
    /// Gets or sets the email for registration.
    /// </summary>
    [Required]
    [EmailAddress]
    [StringLength(255)]
    public required string Email { get; set; }

    /// <summary>
    /// Gets or sets the password for registration.
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 6)]
    public required string Password { get; set; }

    /// <summary>
    /// Gets or sets the password confirmation for validation.
    /// </summary>
    [Required]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    public required string ConfirmPassword { get; set; }
}

/// <summary>
/// Data transfer object for email verification.
/// </summary>
public class VerifyEmailDto
{
    /// <summary>
    /// Gets or sets the email address to verify.
    /// </summary>
    [Required]
    [EmailAddress]
    public required string Email { get; set; }

    /// <summary>
    /// Gets or sets the verification token.
    /// </summary>
    [Required]
    public required string Token { get; set; }
}

/// <summary>
/// Data transfer object for authentication results.
/// </summary>
public class AuthResultDto
{
    /// <summary>
    /// Gets or sets a value indicating whether the authentication was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the JWT token if authentication was successful.
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// Gets or sets the authenticated user information.
    /// </summary>
    public UserDto? User { get; set; }

    /// <summary>
    /// Gets or sets the error message if authentication failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether email verification is required.
    /// </summary>
    public bool EmailVerificationRequired { get; set; }
}