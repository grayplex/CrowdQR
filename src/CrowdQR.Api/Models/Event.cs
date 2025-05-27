using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrowdQR.Api.Models;

/// <summary>
/// Represents a DJ event or set.
/// </summary>
public class Event
{
    /// <summary>
    /// The unique identifier for the event.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int EventId { get; set; }

    /// <summary>
    /// ID of the DJ who hosts this event.
    /// </summary>
    [Required]
    public int DjUserId { get; set; }

    /// <summary>
    /// Name of the event.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public required string Name { get; set; }

    /// <summary>
    /// URL-friendly slug for the event. Must be unique.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public required string Slug { get; set; }

    /// <summary>
    /// When the event was created.
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether the event is currently active.
    /// </summary>
    [Required]
    public bool IsActive { get; set; } = true;

    // Navigation properties

    /// <summary>
    /// Reference to the DJ who hosts this event.
    /// </summary>
    [ForeignKey(nameof(DjUserId))]
    public virtual User DJ { get; set; } = null!;

    /// <summary>
    /// Song requests associated with this event.
    /// </summary>
    public virtual ICollection<Request> Requests { get; set; } = [];

    /// <summary>
    /// Sessions associated with this event.
    /// </summary>
    public virtual ICollection<Session> Sessions { get; set; } = [];
}