using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CrowdQR.Shared.Models.Enums;

namespace CrowdQR.Api.Models;

/// <summary>
/// Represents a song request made by an audience member.
/// </summary>
public class Request
{
    /// <summary>
    /// The unique identifier for the request.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int RequestId { get; set; }

    /// <summary>
    /// ID of the user who made the request.
    /// </summary>
    [Required]
    public int UserId { get; set; }

    /// <summary>
    /// ID of the event this request is for.
    /// </summary>
    [Required]
    public int EventId { get; set; }

    /// <summary>
    /// Name of the requested song.
    /// </summary>
    [Required]
    [MaxLength(255)]
    public required string SongName { get; set; }

    /// <summary>
    /// Name of the artist (optional).
    /// </summary>
    [MaxLength(255)]
    public string? ArtistName { get; set; }

    /// <summary>
    /// Current status of the request.
    /// </summary>
    [Required]
    public RequestStatus Status { get; set; } = RequestStatus.Pending;

    /// <summary>
    /// When the request was created.
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties

    /// <summary>
    /// Reference to the user who made the request.
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// Reference to the event this request is for.
    /// </summary>
    [ForeignKey(nameof(EventId))]
    public virtual Event Event { get; set; } = null!;

    /// <summary>
    /// Votes received for this request.
    /// </summary>
    public virtual ICollection<Vote> Votes { get; set; } = [];

    /// <summary>
    /// Additional metadata for the requested track.
    /// </summary>
    public virtual TrackMetadata? TrackMetadata { get; set; }
}
