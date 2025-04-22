using System.ComponentModel.DataAnnotations;

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

/// <summary>
/// Data transfer object for creating a new vote.
/// </summary>
public class VoteCreateDto
{
    /// <summary>
    /// The ID of the user casting the vote.
    /// </summary>
    [Required]
    public int UserId { get; set; }

    /// <summary>
    /// The ID of the request being voted for.
    /// </summary>
    [Required]
    public int RequestId { get; set; }
}