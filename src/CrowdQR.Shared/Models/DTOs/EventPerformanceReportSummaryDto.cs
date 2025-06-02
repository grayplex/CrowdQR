namespace CrowdQR.Shared.Models.DTOs;

/// <summary>
/// Summary statistics for the event performance report.
/// </summary>
public class EventPerformanceReportSummaryDto
{
    /// <summary>
    /// Total number of requests.
    /// </summary>
    public int TotalRequests { get; set; }

    /// <summary>
    /// Number of approved requests.
    /// </summary>
    public int ApprovedRequests { get; set; }

    /// <summary>
    /// Number of rejected requests.
    /// </summary>
    public int RejectedRequests { get; set; }

    /// <summary>
    /// Total votes cast.
    /// </summary>
    public int TotalVotes { get; set; }

    /// <summary>
    /// Number of unique participants.
    /// </summary>
    public int UniqueParticipants { get; set; }
}