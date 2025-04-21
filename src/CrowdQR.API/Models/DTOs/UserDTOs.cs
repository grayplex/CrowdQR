using System.ComponentModel.DataAnnotations;
using CrowdQR.Api.Models.Enums;

namespace CrowdQR.Api.Models.DTOs;

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