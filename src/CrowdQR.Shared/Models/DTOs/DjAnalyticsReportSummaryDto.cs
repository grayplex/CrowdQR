namespace CrowdQR.Shared.Models.DTOs;

/// <summary>
/// Summary statistics for the DJ analytics report across all events.
/// </summary>
public class DjAnalyticsReportSummaryDto
{
    /// <summary>
    /// Total number of events created by the DJ.
    /// </summary>
    public int TotalEvents { get; set; }

    /// <summary>
    /// Number of currently active events.
    /// </summary>
    public int ActiveEvents { get; set; }

    /// <summary>
    /// Total requests across all events.
    /// </summary>
    public int TotalRequests { get; set; }

    /// <summary>
    /// Total votes across all events.
    /// </summary>
    public int TotalVotes { get; set; }

    /// <summary>
    /// Average requests per event.
    /// </summary>
    public double AverageRequestsPerEvent => TotalEvents > 0 ? (double)TotalRequests / TotalEvents : 0;

    /// <summary>
    /// Average votes per event.
    /// </summary>
    public double AverageVotesPerEvent => TotalEvents > 0 ? (double)TotalVotes / TotalEvents : 0;

    /// <summary>
    /// Most popular event name (by request count).
    /// </summary>
    public string? MostPopularEvent { get; set; }

    /// <summary>
    /// Highest vote count in a single event.
    /// </summary>
    public int HighestEventVoteCount { get; set; }
}