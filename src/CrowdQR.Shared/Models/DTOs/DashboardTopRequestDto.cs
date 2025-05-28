using System;
using System.Collections.Generic;

namespace CrowdQR.Shared.Models.DTOs;

/// <summary>
/// Data transfer object representing the top requests.
/// </summary>
public class DashboardTopRequestDto
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