﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CrowdQR.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace CrowdQR.Api.Models;

/// <summary>
/// Represents a user in the system, either an audience member or DJ.
/// </summary>
public class User
{
    /// <summary>
    /// The unique identifier for the user.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int UserId { get; set; }

    /// <summary>
    /// The username of the user. Must be unique.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public required string Username { get; set; }

    /// <summary>
    /// The email address of the user. Required for DJ accounts.
    /// </summary>
    [MaxLength(255)]
    public string? Email { get; set; }

    /// <summary>
    /// The hashed password. Required for DJ accounts.
    /// </summary>
    [MaxLength(512)]
    public string? PasswordHash { get; set; }

    /// <summary>
    /// Security salt used for password hashing. Required for DJ accounts.
    /// </summary>
    [MaxLength(256)]
    public string? PasswordSalt { get; set; }

    /// <summary>
    /// Indicates whether the user's email has been verified.
    /// </summary>
    public bool IsEmailVerified { get; set; } = false;

    /// <summary>
    /// The email verification token.
    /// </summary>
    [MaxLength(100)]
    public string? EmailVerificationToken { get; set; }

    /// <summary>
    /// Expiration time for the email verification token.
    /// </summary>
    public DateTime? EmailTokenExpiry { get; set; }

    /// <summary>
    /// The role of the user. Default is Audience.
    /// </summary>
    [Required]
    public UserRole Role { get; set; } = UserRole.Audience;

    /// <summary>
    /// When the user account was created.
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties

    /// <summary>
    /// Events hosted by this user (if DJ).
    /// </summary>
    public virtual ICollection<Event> HostedEvents { get; set; } = [];

    /// <summary>
    /// Requests made by this user.
    /// </summary>
    public virtual ICollection<Request> Requests { get; set; } = [];

    /// <summary>
    /// Votes submitted by this user.
    /// </summary>
    public virtual ICollection<Vote> Votes { get; set; } = [];

    /// <summary>
    /// Sessions associated with this user.
    /// </summary>
    public virtual ICollection<Session> Sessions { get; set; } = [];
}
