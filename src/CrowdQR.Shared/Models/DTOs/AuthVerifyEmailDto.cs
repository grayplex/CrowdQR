using System;
using System.ComponentModel.DataAnnotations;

namespace CrowdQR.Shared.Models.DTOs;

/// <summary>
/// Data transfer object for email verification.
/// </summary>
public class AuthVerifyEmailDto
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