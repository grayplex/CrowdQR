using System.ComponentModel.DataAnnotations;

namespace CrowdQR.Api.Models.DTOs;

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