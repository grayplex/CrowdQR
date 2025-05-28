using System;
using System.ComponentModel.DataAnnotations;
using CrowdQR.Shared.Models.Enums;

namespace CrowdQR.Shared.Models.DTOs;

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
