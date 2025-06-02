namespace CrowdQR.Shared.Models.DTOs;

/// <summary>
/// Data transfer object for event performance reports.
/// </summary>
public class EventPerformanceReportDto
{
    /// <summary>
    /// Report title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Report generation timestamp.
    /// </summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>
    /// Event details.
    /// </summary>
    public EventDto Event { get; set; } = new();

    /// <summary>
    /// Report data rows.
    /// </summary>
    public List<EventPerformanceReportRowDto> Rows { get; set; } = [];

    /// <summary>
    /// Summary statistics.
    /// </summary>
    public EventPerformanceReportSummaryDto Summary { get; set; } = new();
}