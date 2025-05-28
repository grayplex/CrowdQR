using System;
using System.ComponentModel.DataAnnotations;

namespace CrowdQR.Shared.Models.DTOs;

/// <summary>
/// Data transfer object for changing a user's password.
/// </summary>
public class UserChangePasswordDto
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
