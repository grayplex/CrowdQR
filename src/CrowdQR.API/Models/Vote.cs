using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrowdQR.Api.Models;

/// <summary>
/// Represents a user's vote for a song request.
/// </summary>

public class Vote
{
    /// <summary>
    /// The unique identifier for the vote.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int VoteId { get; set; }

    /// <summary>
    /// ID of the request being voted on.
    /// </summary>
    [Required]
    public int RequestId { get; set; }

    /// <summary>
    /// ID of the user who cast the vote.
    /// </summary>
    [Required]
    public int UserId { get; set; }

    /// <summary>
    /// When the vote was cast.
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties

    /// <summary>
    /// Reference to the request being voted on.
    /// </summary>
    [ForeignKey(nameof(RequestId))]
    public virtual Request Request { get; set; } = null!;

    /// <summary>
    /// Reference to the user who cast the vote.
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;
}
