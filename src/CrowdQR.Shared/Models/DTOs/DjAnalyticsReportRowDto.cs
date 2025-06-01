namespace CrowdQR.Shared.Models.DTOs;

/// <summary>
/// Individual row in the DJ analytics report representing one event.
/// </summary>
public class DjAnalyticsReportRowDto
{
    /// <summary>
    /// Name of the event.
    /// </summary>
    public string EventName { get; set; } = string.Empty;

    /// <summary>
    /// URL slug of the event.
    /// </summary>
    public string EventSlug { get; set; } = string.Empty;

    /// <summary>
    /// When the event was created.
    /// </summary>
    public DateTime EventDate { get; set; }

    /// <summary>
    /// Total number of requests for this event.
    /// </summary>
    public int TotalRequests { get; set; }

    /// <summary>
    /// Number of approved requests for this event.
    /// </summary>
    public int ApprovedRequests { get; set; }

    /// <summary>
    /// Number of rejected requests for this event.
    /// </summary>
    public int RejectedRequests { get; set; }

    /// <summary>
    /// Total votes received across all requests in this event.
    /// </summary>
    public int TotalVotes { get; set; }

    /// <summary>
    /// Number of unique participants in this event.
    /// </summary>
    public int UniqueParticipants { get; set; }

    /// <summary>
    /// Whether the event is currently active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Average votes per request for this event.
    /// </summary>
    public double AverageVotesPerRequest => TotalRequests > 0 ? (double)TotalVotes / TotalRequests : 0;

    /// <summary>
    /// Approval rate percentage for this event.
    /// </summary>
    public double ApprovalRate => TotalRequests > 0 ? (double)ApprovedRequests / TotalRequests * 100 : 0;
}