using System.Collections.Generic;

namespace CrowdQR.Shared.Models.DTOs;

/// <summary>
/// Data transfer object representing request counts by status.
/// </summary>
public class DashboardRequestCountsDto
{
    /// <summary>
    /// Total request count.
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    /// Count of pending requests.
    /// </summary>
    public int Pending { get; set; }

    /// <summary>
    /// Count of approved requests.
    /// </summary>
    public int Approved { get; set; }

    /// <summary>
    /// Count of rejected requests.
    /// </summary>
    public int Rejected { get; set; }
}