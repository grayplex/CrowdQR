using CrowdQR.Shared.Models.Enums;

namespace CrowdQR.Shared.Models.DTOs;

/// <summary>
/// Data transfer object representing a song request.
/// </summary>
public class RequestDto
{
    /// <summary>
    /// The unique identifier for the request.
    /// </summary>
    public int RequestId { get; set; }

    /// <summary>
    /// The name of the requested song.
    /// </summary>
    public string SongName { get; set; } = string.Empty;

    /// <summary>
    /// The name of the artist for the requested song (optional).
    /// </summary>
    public string? ArtistName { get; set; }

    /// <summary>
    /// The ID of the event this request is for.
    /// </summary>
    public int EventId { get; set; }

    /// <summary>
    /// The ID of the user who made the request.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// The username of the requester.
    /// </summary>
    public string Requester { get; set; } = string.Empty;

    /// <summary>
    /// The number of votes this request has received.
    /// </summary>
    public int VoteCount { get; set; }

    /// <summary>
    /// Indicates whether the current user has voted for this request.
    /// </summary>
    public bool UserHasVoted { get; set; }

    /// <summary>
    /// List of votes for this request.
    /// </summary>
    public List<VoteDto>? Votes { get; set; }

    /// <summary>
    /// The current status of the request.
    /// </summary>
    public RequestStatus Status { get; set; } = RequestStatus.Pending;

    /// <summary>
    /// When the request was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the request was last acted upon (approval/rejection).
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}