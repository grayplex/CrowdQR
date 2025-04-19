using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrowdQR.Api.Models;

/// <summary>
/// Represents a user's active session during an event.
/// </summary>
public class Session
{
    /// <summary>
    /// The unique identifier for the session.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int SessionId { get; set; }

    /// <summary>
    /// ID of the user associated with this session.
    /// </summary>
    [Required]
    public int UserId { get; set; }

    /// <summary>
    /// ID of the event this session is for.
    /// </summary>
    [Required]
    public int EventId { get; set; }

    /// <summary>
    /// Client IP address for rate limiting and security.
    /// </summary>
    [MaxLength(45)]
    public string? ClientIP { get; set; }

    /// <summary>
    /// When the session was last active.
    /// </summary>
    [Required]
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Number of requests made in this session (for rate limiting).
    /// </summary>
    [Required]
    public int RequestCount { get; set; } = 0;

    // Navigation properties

    /// <summary>
    /// Reference to the user associated with this session.
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// Reference to the event this session is for.
    /// </summary>
    [ForeignKey(nameof(EventId))]
    public virtual Event Event { get; set; } = null!;
}
