using System;
using System.Collections.Generic;

namespace CrowdQR.Shared.Models.DTOs;

/// <summary>
/// Data transfer object representing an event summary for the dashboard.
/// </summary>
public class DashboardEventSummaryDto
{
    /// <summary>
    /// The unique identifier for the event.
    /// </summary>
    public int EventId { get; set; }

    /// <summary>
    /// The total number of requests.
    /// </summary>
    public int TotalRequests { get; set; }

    /// <summary>
    /// The number of pending requests.
    /// </summary>
    public int PendingRequests { get; set; }

    /// <summary>
    /// The number of approved requests.
    /// </summary>
    public int ApprovedRequests { get; set; }

    /// <summary>
    /// The number of rejected requests.
    /// </summary>
    public int RejectedRequests { get; set; }

    /// <summary>
    /// The total number of votes.
    /// </summary>
    public int TotalVotes { get; set; }

    /// <summary>
    /// The number of active users.
    /// </summary>
    public int ActiveUsers { get; set; }

    /// <summary>
    /// The top requests.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0028:Simplify collection initialization", Justification = "<Pending>")]
    public List<RequestDto> TopRequests { get; set; } = new List<RequestDto>();

    /// <summary>
    /// The recently approved requests.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0028:Simplify collection initialization", Justification = "<Pending>")]
    public List<RequestDto> RecentlyApproved { get; set; } = new List<RequestDto>();

    /// <summary>
    /// The recently rejected requests.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0028:Simplify collection initialization", Justification = "<Pending>")]
    public List<RequestDto> RecentlyRejected { get; set; } = new List<RequestDto>();

    /// <summary>
    /// The active users list.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0028:Simplify collection initialization", Justification = "<Pending>")]
    public List<UserDto> ActiveUsersList { get; set; } = new List<UserDto>();
}