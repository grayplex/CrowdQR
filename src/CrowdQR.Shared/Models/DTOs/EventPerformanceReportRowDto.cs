namespace CrowdQR.Shared.Models.DTOs;

/// <summary>
/// Individual row in the event performance report.
/// </summary>
public class EventPerformanceReportRowDto
{
    /// <summary>
    /// Song name.
    /// </summary>
    public string SongName { get; set; } = string.Empty;

    /// <summary>
    /// Artist name.
    /// </summary>
    public string? ArtistName { get; set; }

    /// <summary>
    /// Username who requested the song.
    /// </summary>
    public string Requester { get; set; } = string.Empty;

    /// <summary>
    /// Number of votes received.
    /// </summary>
    public int VoteCount { get; set; }

    /// <summary>
    /// Current status of the request.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// When the request was submitted.
    /// </summary>
    public DateTime RequestedAt { get; set; }

    /// <summary>
    /// When the status was last updated.
    /// </summary>
    public DateTime? StatusUpdatedAt { get; set; }
}