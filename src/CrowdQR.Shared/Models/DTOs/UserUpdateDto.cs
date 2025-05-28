using System;
using System.ComponentModel.DataAnnotations;
using CrowdQR.Shared.Models.Enums;

namespace CrowdQR.Shared.Models.DTOs;

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