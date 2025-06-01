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
/// <remarks>
/// Initializes a new instance of the ReportsModel class.
/// </remarks>
/// <param name="reportService">The report service.</param>
/// <param name="eventService">The event service.</param>
/// <param name="logger">The logger.</param>
[Authorize(Policy = "DjOnly")]
public class ReportsModel(
    ReportService reportService,
    EventService eventService,
    ILogger<ReportsModel> logger) : PageModel
{
    private readonly ReportService _reportService = reportService;
    private readonly EventService _eventService = eventService;
    private readonly ILogger<ReportsModel> _logger = logger;

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
    /// Handles GET requests to load the reports page.
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

            // Load user's events for the dropdown
            UserEvents = await _eventService.GetEventsByDjAsync(userId);
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading reports page for DJ");
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
        try
        {
            // Load user events first
            await OnGetAsync();

            // Generate the event report
            EventReport = await _reportService.GetEventPerformanceReportAsync(eventId);

            if (EventReport == null)
            {
                ErrorMessage = "Unable to generate event report. Please try again.";
                return Page();
            }

            SuccessMessage = "Event performance report generated successfully!";
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating event performance report for event {EventId}", eventId);
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
        try
        {
            // Load user events first
            await OnGetAsync();

            // Generate the DJ analytics report
            DjReport = await _reportService.GetDjAnalyticsReportAsync();

            if (DjReport == null)
            {
                ErrorMessage = "Unable to generate DJ analytics report. Please try again.";
                return Page();
            }

            SuccessMessage = "DJ analytics report generated successfully!";
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating DJ analytics report");
            ErrorMessage = "An error occurred while generating the report. Please try again.";
            await OnGetAsync(); // Reload page data
            return Page();
        }
    }
}