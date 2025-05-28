using CrowdQR.Shared.Models.Enums;

namespace CrowdQR.Shared.Models.DTOs;

/// <summary>
/// Data transfer object for a detailed request with votes.
/// </summary>
public class RequestDetailsDto
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
    /// The current status of the request.
    /// </summary>
    public RequestStatus Status { get; set; }

    /// <summary>
    /// The votes for this request.
    /// </summary>
    public List<VoteDto> Votes { get; set; } = [];

    /// <summary>
    /// The number of votes this request has received.
    /// </summary>
    public int VoteCount => Votes.Count;
}