using System.Collections.Generic;

namespace CrowdQR.Shared.Models.DTOs;

/// <summary>
/// Data transfer object representing an event summary for the dashboard.
/// </summary>
public class EventSummaryDto
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
    public List<RequestDto> TopRequests { get; set; } = [];

    /// <summary>
    /// The recently approved requests.
    /// </summary>
    public List<RequestDto> RecentlyApproved { get; set; } = [];

    /// <summary>
    /// The recently rejected requests.
    /// </summary>
    public List<RequestDto> RecentlyRejected { get; set; } = [];

    /// <summary>
    /// The active users list.
    /// </summary>
    public List<UserDto> ActiveUsersList { get; set; } = [];
}

/// <summary>
/// Data transfer object representing the top requests.
/// </summary>
public class TopRequestDto
{
    /// <summary>
    /// The unique identifier for the request.
    /// </summary>
    public int RequestId { get; set; }

    /// <summary>
    /// The song name.
    /// </summary>
    public string SongName { get; set; } = string.Empty;

    /// <summary>
    /// The artist name.
    /// </summary>
    public string? ArtistName { get; set; }

    /// <summary>
    /// The requester's username.
    /// </summary>
    public string Requester { get; set; } = string.Empty;

    /// <summary>
    /// The vote count.
    /// </summary>
    public int VoteCount { get; set; }

    /// <summary>
    /// The creation time.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Data transfer object representing DJ event statistics.
/// </summary>
public class DjEventStatDto
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
    public RequestCountsDto RequestCounts { get; set; } = new RequestCountsDto();

    /// <summary>
    /// Total votes for this event.
    /// </summary>
    public int TotalVotes { get; set; }
}

/// <summary>
/// Data transfer object representing request counts by status.
/// </summary>
public class RequestCountsDto
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