using System;
using System.ComponentModel.DataAnnotations;

namespace CrowdQR.Shared.Models.DTOs;

/// <summary>
/// Data transfer object for DJ registration requests.
/// </summary>
public class AuthDjRegisterDto
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