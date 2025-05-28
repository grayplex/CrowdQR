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
