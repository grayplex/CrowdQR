using System;
using System.ComponentModel.DataAnnotations;

namespace CrowdQR.Shared.Models.DTOs;

/// <summary>
/// Data transfer object for login requests.
/// </summary>
public class AuthLoginDto
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

