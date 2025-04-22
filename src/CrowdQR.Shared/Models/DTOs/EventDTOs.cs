using System.ComponentModel.DataAnnotations;

namespace CrowdQR.Shared.Models.DTOs;

/// <summary>
/// Data transfer object representing an event.
/// </summary>
public class EventDto
{
    /// <summary>
    /// The unique identifier for the event.
    /// </summary>
    public int EventId { get; set; }

    /// <summary>
    /// The name of the event.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The slug for the event, used in URLs.
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Whether the event is currently active.
    /// </summary>
    public bool IsActive { get; set; }
}

/// <summary>
/// Data transfer object for creating a new event.
/// </summary>
public class EventCreateDto
{
    /// <summary>
    /// The ID of the DJ user who hosts the event.
    /// </summary>
    [Required]
    public int DjUserId { get; set; }

    /// <summary>
    /// The name of the event.
    /// </summary>
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public required string Name { get; set; }

    /// <summary>
    /// URL-friendly slug for the event. Must be unique.
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    [RegularExpression(@"^[a-z0-9-]+$", ErrorMessage = "Slug can only contain lowercase letters, numbers, and hyphens.")]
    public required string Slug { get; set; }

    /// <summary>
    /// Whether the event is active. Defaults to true.
    /// </summary>
    public bool? IsActive { get; set; }
}

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

/// <summary>
/// Data transfer object summarizing an event.
/// </summary>
public class EventSummaryDto
{
    /// <summary>
    /// The unique identifier for the event.
    /// </summary>
    public int EventId { get; set; }

    /// <summary>
    /// The name of the event.
    /// </summary>
    public string EventName { get; set; } = string.Empty;

    /// <summary>
    /// The total number of requests made during the event.
    /// </summary>
    public int TotalRequests { get; set; }

    /// <summary>
    /// The total number of unique users who participated in the event.
    /// </summary>
    public int UniqueUsers { get; set; }
}

/// <summary>
/// Data transfer object for DJ event statistics.
/// </summary>
public class DjEventStatDto
{
    /// <summary>
    /// The unique identifier for the event.
    /// </summary>
    public int EventId { get; set; }

    /// <summary>
    /// The name of the event.
    /// </summary>
    public string EventName { get; set; } = string.Empty;

    /// <summary>
    /// The total number of requests made during the event.
    /// </summary>
    public int RequestCount { get; set; }

    /// <summary>
    /// The timestamp when the event was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}