using System.ComponentModel.DataAnnotations;

namespace CrowdQR.Shared.Models.DTOs;

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