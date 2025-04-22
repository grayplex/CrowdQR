using System.ComponentModel.DataAnnotations;

namespace CrowdQR.Shared.Models.DTOs;

/// <summary>
/// Data transfer object representing a user session.
/// </summary>
public class SessionDto
{
    /// <summary>
    /// The unique identifier for the session.
    /// </summary>
    public int SessionId { get; set; }

    /// <summary>
    /// The ID of the user associated with this session.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// The ID of the event this session is for.
    /// </summary>
    public int EventId { get; set; }

    /// <summary>
    /// The client IP address for rate limiting and security purposes.
    /// </summary>
    public string? ClientIP { get; set; }

    /// <summary>
    /// The number of requests made in this session, used for rate limiting.
    /// </summary>
    public int RequestCount { get; set; }

    /// <summary>
    /// When the session was last active. This is used to determine session validity and for cleanup purposes.
    /// </summary>
    public DateTime LastSeen { get; set; }
}

/// <summary>
/// Data transfer object for creating or updating a session.
/// </summary>
public class SessionCreateDto
{
    /// <summary>
    /// The ID of the user for this session.
    /// </summary>
    [Required]
    public int UserId { get; set; }

    /// <summary>
    /// The ID of the event for this session.
    /// </summary>
    [Required]
    public int EventId { get; set; }

    /// <summary>
    /// The client IP address (for rate limiting and security).
    /// </summary>
    [StringLength(45)]
    public string? ClientIP { get; set; }
}