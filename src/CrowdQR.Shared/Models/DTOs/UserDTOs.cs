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
    /// The role of the user.
    /// </summary>
    public UserRole? Role { get; set; }
}