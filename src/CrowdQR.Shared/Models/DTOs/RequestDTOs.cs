using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Contracts;
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

/// <summary>
/// Data transfer object for creating a new song request.
/// </summary>
public class RequestCreateDto
{
    /// <summary>
    /// The ID of the user making the request.
    /// </summary>
    [Required]
    public int UserId { get; set; }

    /// <summary>
    /// The ID of the event the request is for.
    /// </summary>
    [Required]
    public int EventId { get; set; }

    /// <summary>
    /// The name of the requested song.
    /// </summary>
    [Required]
    [StringLength(255, MinimumLength = 1)]
    public required string SongName { get; set; }

    /// <summary>
    /// The name of the artist (optional).
    /// </summary>
    [StringLength(255)]
    public string? ArtistName { get; set; }
}

/// <summary>
/// Data transfer object for updating a request's status.
/// </summary>
public class RequestStatusUpdateDto
{
    /// <summary>
    /// The new status for the request.
    /// </summary>
    [Required]
    public RequestStatus Status { get; set; }
}

/// <summary>
/// Data transfer object for the top requests in an event.
/// </summary>
public class TopRequestDto
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
    /// The number of votes this request has received.
    /// </summary>
    public int VoteCount { get; set; }
}