using CrowdQR.Shared.Models.DTOs;
using CrowdQR.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace CrowdQR.Web.Pages.Admin;

/// <summary>
/// Page model for managing DJ events.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EventsModel"/> class.
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="EventsModel"/> class.
/// </remarks>
/// <param name="eventService">The event service.</param>
/// <param name="logger">The logger.</param>
[Authorize(Policy = "DjOnly")]
public class EventsModel(EventService eventService, ILogger<EventsModel> logger) : PageModel
{
    private readonly EventService _eventService = eventService;
    private readonly ILogger<EventsModel> _logger = logger;

    /// <summary>
    /// List of events owned by the DJ.
    /// </summary>
    public List<EventDto> Events { get; set; } = [];

    /// <summary>
    /// Model for creating a new event.
    /// </summary>
    [BindProperty]
    public EventCreateModel NewEvent { get; set; } = new();

    /// <summary>
    /// Model for editing an existing event.
    /// </summary>
    [BindProperty]
    public EventEditModel EditEvent { get; set; } = new();

    /// <summary>
    /// Success message to display.
    /// </summary>
    [TempData]
    public string? SuccessMessage { get; set; }

    /// <summary>
    /// Error message to display.
    /// </summary>
    [TempData]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Handles GET requests to load the DJ's events.
    /// </summary>
    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            // Get the current user's ID
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                ErrorMessage = "Unable to determine user identity. Please try logging in again.";
                return RedirectToPage("/Login");
            }

            // Get the DJ's events
            Events = await _eventService.GetEventsByDjAsync(userId);
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading events for DJ");
            ErrorMessage = "An error occurred while loading your events. Please try again.";
            return Page();
        }
    }

    /// <summary>
    /// Handles POST requests to create a new event.
    /// </summary>
    public async Task<IActionResult> OnPostCreateAsync()
    {
        try
        {
            if (!ModelState.IsValid)
            {
                await OnGetAsync();
                return Page();
            }

            // Get the current user's ID
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                ErrorMessage = "Unable to determine user identity. Please try logging in again.";
                return RedirectToPage("/Login");
            }

            // Create the event
            var eventDto = new EventCreateDto
            {
                DjUserId = userId,
                Name = NewEvent.Name,
                Slug = NewEvent.Slug,
                IsActive = true
            };

            var (success, createdEvent) = await _eventService.CreateEventAsync(eventDto);

            if (!success || createdEvent == null)
            {
                ErrorMessage = "Failed to create event. Please check the event details and try again.";
                await OnGetAsync();
                return Page();
            }

            SuccessMessage = $"Event '{createdEvent.Name}' created successfully!";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating new event");
            ErrorMessage = "An error occurred while creating the event. Please try again.";
            await OnGetAsync();
            return Page();
        }
    }

    /// <summary>
    /// Handles POST requests to update an existing event.
    /// </summary>
    public async Task<IActionResult> OnPostUpdateAsync()
    {
        try
        {
            if (!ModelState.IsValid)
            {
                await OnGetAsync();
                return Page();
            }

            // Create the event update DTO
            var eventUpdateDto = new EventUpdateDto
            {
                Name = EditEvent.Name,
                Slug = EditEvent.Slug,
                IsActive = EditEvent.IsActive
            };

            var success = await _eventService.UpdateEventAsync(EditEvent.EventId, eventUpdateDto);

            if (!success)
            {
                ErrorMessage = "Failed to update event. Please try again.";
                await OnGetAsync();
                return Page();
            }

            SuccessMessage = "Event updated successfully!";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating event {EventId}", EditEvent.EventId);
            ErrorMessage = "An error occurred while updating the event. Please try again.";
            await OnGetAsync();
            return Page();
        }
    }

    /// <summary>
    /// Handles POST requests to delete an event.
    /// </summary>
    public async Task<IActionResult> OnPostDeleteAsync(int eventId)
    {
        try
        {
            var success = await _eventService.DeleteEventAsync(eventId);

            if (!success)
            {
                ErrorMessage = "Failed to delete event. Please try again.";
                await OnGetAsync();
                return Page();
            }

            SuccessMessage = "Event deleted successfully!";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting event {EventId}", eventId);
            ErrorMessage = "An error occurred while deleting the event. Please try again.";
            await OnGetAsync();
            return Page();
        }
    }

    /// <summary>
    /// Model for creating a new event.
    /// </summary>
    public class EventCreateModel
    {
        /// <summary>
        /// The name of the event.
        /// </summary>
        [Required(ErrorMessage = "Event name is required")]
        [StringLength(200, MinimumLength = 3, ErrorMessage = "Event name must be between 3 and 200 characters")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// URL-friendly slug for the event. Must be unique.
        /// </summary>
        [Required(ErrorMessage = "Event slug is required")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Slug must be between 3 and 100 characters")]
        [RegularExpression(@"^[a-z0-9-]+$", ErrorMessage = "Slug can only contain lowercase letters, numbers, and hyphens")]
        public string Slug { get; set; } = string.Empty;
    }

    /// <summary>
    /// Model for editing an existing event.
    /// </summary>
    public class EventEditModel
    {
        /// <summary>
        /// The unique identifier for the event.
        /// </summary>
        [Required]
        public int EventId { get; set; }

        /// <summary>
        /// The name of the event.
        /// </summary>
        [Required(ErrorMessage = "Event name is required")]
        [StringLength(200, MinimumLength = 3, ErrorMessage = "Event name must be between 3 and 200 characters")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// URL-friendly slug for the event. Must be unique.
        /// </summary>
        [Required(ErrorMessage = "Event slug is required")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Slug must be between 3 and 100 characters")]
        [RegularExpression(@"^[a-z0-9-]+$", ErrorMessage = "Slug can only contain lowercase letters, numbers, and hyphens")]
        public string Slug { get; set; } = string.Empty;

        /// <summary>
        /// Whether the event is currently active.
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}