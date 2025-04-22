using System.ComponentModel.DataAnnotations;
using CrowdQR.Shared.Models.Enums;

namespace CrowdQR.Shared.Models.DTOs;

/// <summary>
/// Data transfer object representing a user.
/// </summary>
public class UserDto
{
    /// <summary>
    /// The unique identifier for the user.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// The username of the user.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// The email address of the user. Required for DJ accounts.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Indicates whether the user's email has been verified.
    /// </summary>
    public bool IsEmailVerified { get; set; }

    /// <summary>
    /// The role of the user. Default is Audience.
    /// </summary>
    public UserRole Role { get; set; } = UserRole.Audience;

    /// <summary>
    /// When the user account was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Data transfer object for creating a new user.
/// </summary>
public class UserCreateDto
{
    /// <summary>
    /// The username of the user. Must be unique.
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 3)]
    public required string Username { get; set; }

    /// <summary>
    /// The email address of the user. Required for DJ accounts.
    /// </summary>
    [EmailAddress]
    [StringLength(255)]
    public string? Email { get; set; }

    /// <summary>
    /// The password for the user. Required for DJ accounts.
    /// </summary>
    [StringLength(100, MinimumLength = 6)]
    public string? Password { get; set; }

    /// <summary>
    /// The role of the user. Default is Audience.
    /// </summary>
    [Required]
    public UserRole Role { get; set; } = UserRole.Audience;
}

/// <summary>
/// Data transfer object for updating a user.
/// </summary>
public class UserUpdateDto
{
    /// <summary>
    /// The username of the user. Must be unique.
    /// </summary>
    [StringLength(100, MinimumLength = 3)]
    public string? Username { get; set; }

    /// <summary>
    /// The email address of the user.
    /// </summary>
    [EmailAddress]
    [StringLength(255)]
    public string? Email { get; set; }

    /// <summary>
    /// The role of the user.
    /// </summary>
    public UserRole? Role { get; set; }
}

/// <summary>
/// Data transfer object for changing a user's password.
/// </summary>
public class ChangePasswordDto
{
    /// <summary>
    /// The current password.
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 6)]
    public required string CurrentPassword { get; set; }

    /// <summary>
    /// The new password.
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 6)]
    public required string NewPassword { get; set; }

    /// <summary>
    /// Confirmation of the new password.
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 6)]
    [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
    public required string ConfirmPassword { get; set; }
}