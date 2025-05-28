using System;
using System.Collections.Generic;

namespace CrowdQR.Shared.Models.DTOs;

/// <summary>
/// Data transfer object representing DJ event statistics.
/// </summary>
public class DashboardDjEventStatDto
{
    /// <summary>
    /// The unique identifier for the event.
    /// </summary>
    public int EventId { get; set; }

    /// <summary>
    /// The event name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The event slug.
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Whether the event is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Creation time of the event.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Request counts for this event.
    /// </summary>
    public DashboardRequestCountsDto RequestCounts { get; set; } = new DashboardRequestCountsDto();

    /// <summary>
    /// Total votes for this event.
    /// </summary>
    public int TotalVotes { get; set; }
}