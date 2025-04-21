using System.ComponentModel.DataAnnotations;
using CrowdQR.Shared.Models.Enums;

namespace CrowdQR.Shared.Models.DTOs;

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
