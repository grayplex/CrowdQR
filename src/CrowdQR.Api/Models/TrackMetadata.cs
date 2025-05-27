using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrowdQR.Api.Models;

/// <summary>
/// Represents additional metadata for a song request.
/// </summary>
public class TrackMetadata
{
    /// <summary>
    /// The unique identifier for the track metadata.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int TrackId { get; set; }

    /// <summary>
    /// ID of the request this metadata is for.
    /// </summary>
    [Required]
    public int RequestId { get; set; }

    /// <summary>
    /// Spotify track ID if available.
    /// </summary>
    [MaxLength(50)]
    public string? SpotifyId { get; set; }

    /// <summary>
    /// YouTube video ID if available.
    /// </summary>
    [MaxLength(50)]
    public string? YoutubeId { get; set; }

    /// <summary>
    /// Duration of the song in seconds.
    /// </summary>
    public int? Duration { get; set; }

    /// <summary>
    /// URL to the album art image.
    /// </summary>
    [MaxLength(1000)]
    public string? AlbumArtUrl { get; set; }

    // Navigation properties

    /// <summary>
    /// Reference to the request this metadata is for.
    /// </summary>
    [ForeignKey(nameof(RequestId))]
    public virtual Request Request { get; set; } = null!;
}
