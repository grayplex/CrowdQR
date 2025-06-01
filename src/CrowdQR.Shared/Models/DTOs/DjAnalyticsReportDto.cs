namespace CrowdQR.Shared.Models.DTOs;

/// <summary>
/// Data transfer object for DJ analytics reports.
/// </summary>
public class DjAnalyticsReportDto
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
    /// DJ username.
    /// </summary>
    public string DjName { get; set; } = string.Empty;

    /// <summary>
    /// Report data rows - one per event.
    /// </summary>
    public List<DjAnalyticsReportRowDto> Rows { get; set; } = [];

    /// <summary>
    /// Summary statistics across all events.
    /// </summary>
    public DjAnalyticsReportSummaryDto Summary { get; set; } = new();
}