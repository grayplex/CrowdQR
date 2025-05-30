using System.ComponentModel.DataAnnotations;

namespace CrowdQR.Shared.Models.DTOs;

/// <summary>
/// Data transfer object for updating an event.
/// </summary>
public class EventUpdateDto
{
    /// <summary>
    /// The name of the event.
    /// </summary>
    [StringLength(200, MinimumLength = 1)]
    public string? Name { get; set; }

    /// <summary>
    /// URL-friendly slug for the event. Must be unique.
    /// </summary>
    [StringLength(100, MinimumLength = 1)]
    [RegularExpression(@"^[a-z0-9-]+$", ErrorMessage = "Slug can only contain lowercase letters, numbers, and hyphens.")]
    public string? Slug { get; set; }

    /// <summary>
    /// Whether the event is active.
    /// </summary>
    public bool? IsActive { get; set; }
}