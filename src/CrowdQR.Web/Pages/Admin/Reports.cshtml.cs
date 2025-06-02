using CrowdQR.Shared.Models.DTOs;
using CrowdQR.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace CrowdQR.Web.Pages.Admin;

/// <summary>
/// Page model for the reports administration interface.
/// </summary>
[Authorize(Policy = "DjOnly")]
public class ReportsModel(EventService eventService, ILogger<ReportsModel> logger, ReportService? reportService = null) : PageModel
{

    /// <summary>
    /// List of events owned by the current DJ.
    /// </summary>
    public List<EventDto> UserEvents { get; set; } = [];

    /// <summary>
    /// Event performance report data.
    /// </summary>
    public EventPerformanceReportDto? EventReport { get; set; }

    /// <summary>
    /// DJ analytics report data.
    /// </summary>
    public DjAnalyticsReportDto? DjReport { get; set; }

    /// <summary>
    /// Error message to display.
    /// </summary>
    [TempData]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Success message to display.
    /// </summary>
    [TempData]
    public string? SuccessMessage { get; set; }

    /// <summary>
    /// Debug information.
    /// </summary>
    public string DebugInfo { get; set; } = "";

    /// <summary>
    /// Handles GET requests to load the reports page.
    /// </summary>
    public async Task<IActionResult> OnGetAsync()
    {
        logger.LogInformation("=== Reports OnGetAsync called ===");
        Console.WriteLine("=== Reports OnGetAsync called ===");

        try
        {
            // Get the current user's ID
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                logger.LogError("Unable to determine user identity");
                Console.WriteLine("Unable to determine user identity");
                ErrorMessage = "Unable to determine user identity. Please try logging in again.";
                return RedirectToPage("/Login");
            }

            logger.LogInformation("Loading events for user {UserId}", userId);
            Console.WriteLine($"Loading events for user {userId}");

            // Load user's events for the dropdown
            UserEvents = await eventService.GetEventsByDjAsync(userId);

            logger.LogInformation("Loaded {EventCount} events", UserEvents.Count);
            Console.WriteLine($"Loaded {UserEvents.Count} events");

            DebugInfo = $"User ID: {userId}, Events loaded: {UserEvents.Count}, ReportService is null: {reportService == null}";

            return Page();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error loading reports page for DJ");
            Console.WriteLine($"Error loading reports page: {ex.Message}");
            ErrorMessage = "An error occurred while loading the reports page. Please try again.";
            return Page();
        }
    }

    /// <summary>
    /// Handles requests to generate an event performance report.
    /// </summary>
    /// <param name="eventId">The ID of the event to generate a report for.</param>
    public async Task<IActionResult> OnGetEventPerformanceAsync(int eventId)
    {
        logger.LogInformation("=== OnGetEventPerformanceAsync called with eventId: {EventId} ===", eventId);
        Console.WriteLine($"=== OnGetEventPerformanceAsync called with eventId: {eventId} ===");

        if (reportService == null)
        {
            logger.LogError("ReportService is null - not registered in DI container");
            Console.WriteLine("ReportService is null - not registered in DI container");
            ErrorMessage = "Report service is not available. Please contact support.";
            await OnGetAsync();
            return Page();
        }

        try
        {
            // Load user events first
            await OnGetAsync();

            // Verify the event ID is valid for this user
            if (!UserEvents.Any(e => e.EventId == eventId))
            {
                logger.LogWarning("User attempted to access event {EventId} which they don't own", eventId);
                Console.WriteLine($"User attempted to access event {eventId} which they don't own");
                ErrorMessage = "You don't have permission to generate a report for this event.";
                return Page();
            }

            logger.LogInformation("Calling GetEventPerformanceReportAsync for event {EventId}", eventId);
            Console.WriteLine($"Calling GetEventPerformanceReportAsync for event {eventId}");

            // Generate the event report
            EventReport = await reportService.GetEventPerformanceReportAsync(eventId);

            if (EventReport == null)
            {
                logger.LogWarning("Event performance report was null for event {EventId}", eventId);
                Console.WriteLine($"Event performance report was null for event {eventId}");
                ErrorMessage = "Unable to generate event report. The event may not exist or you may not have permission to access it.";
                return Page();
            }

            logger.LogInformation("Successfully generated event performance report for event {EventId} with {RowCount} rows",
                eventId, EventReport.Rows.Count);
            Console.WriteLine($"Successfully generated event performance report for event {eventId} with {EventReport.Rows.Count} rows");

            SuccessMessage = "Event performance report generated successfully!";
            return Page();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating event performance report for event {EventId}", eventId);
            Console.WriteLine($"Error generating event performance report for event {eventId}: {ex.Message}");
            ErrorMessage = "An error occurred while generating the report. Please try again.";
            await OnGetAsync(); // Reload page data
            return Page();
        }
    }

    /// <summary>
    /// Handles requests to generate a DJ analytics report.
    /// </summary>
    public async Task<IActionResult> OnGetDjAnalyticsAsync()
    {
        logger.LogInformation("=== OnGetDjAnalyticsAsync called ===");
        Console.WriteLine("=== OnGetDjAnalyticsAsync called ===");

        if (reportService == null)
        {
            logger.LogError("ReportService is null - not registered in DI container");
            Console.WriteLine("ReportService is null - not registered in DI container");
            ErrorMessage = "Report service is not available. Please contact support.";
            await OnGetAsync();
            return Page();
        }

        try
        {
            // Load user events first
            await OnGetAsync();

            logger.LogInformation("Calling GetDjAnalyticsReportAsync");
            Console.WriteLine("Calling GetDjAnalyticsReportAsync");

            // Generate the DJ analytics report
            DjReport = await reportService.GetDjAnalyticsReportAsync();

            if (DjReport == null)
            {
                logger.LogWarning("DJ analytics report was null");
                Console.WriteLine("DJ analytics report was null");
                ErrorMessage = "Unable to generate DJ analytics report. Please try again.";
                return Page();
            }

            logger.LogInformation("Successfully generated DJ analytics report with {RowCount} rows", DjReport.Rows.Count);
            Console.WriteLine($"Successfully generated DJ analytics report with {DjReport.Rows.Count} rows");

            SuccessMessage = "DJ analytics report generated successfully!";
            return Page();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating DJ analytics report");
            Console.WriteLine($"Error generating DJ analytics report: {ex.Message}");
            ErrorMessage = "An error occurred while generating the report. Please try again.";
            await OnGetAsync(); // Reload page data
            return Page();
        }
    }
}