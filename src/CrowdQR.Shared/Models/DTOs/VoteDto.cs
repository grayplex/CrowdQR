namespace CrowdQR.Shared.Models.DTOs;

/// <summary>
/// Data transfer object representing a vote on a request.
/// </summary>
public class VoteDto
{
    /// <summary>
    /// The unique identifier for the vote.
    /// </summary>
    public int VoteId { get; set; }

    /// <summary>
    /// The ID of the user who cast the vote.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// The ID of the request being voted for.
    /// </summary>
    public int RequestId { get; set; }

    /// <summary>
    /// When the vote was cast.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

