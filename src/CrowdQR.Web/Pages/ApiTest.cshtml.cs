using CrowdQR.Shared.Models.DTOs;
using CrowdQR.Shared.Models.Enums;
using CrowdQR.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CrowdQR.Web.Pages;

/// <summary>
/// Page model for testing API connectivity.
/// </summary>
/// <remarks>
/// Initializes a new instance of the ApiTestModel class.
/// </remarks>
public class ApiTestModel(
    EventService eventService,
    RequestService requestService,
    UserService userService,
    VoteService voteService,
    SessionService sessionService,
    DashboardService dashboardService,
    ILogger<ApiTestModel> logger) : PageModel
{
    private readonly EventService _eventService = eventService;
    private readonly RequestService _requestService = requestService;
    private readonly UserService _userService = userService;
    private readonly VoteService _voteService = voteService;
    private readonly SessionService _sessionService = sessionService;
    private readonly DashboardService _dashboardService = dashboardService;
    private readonly ILogger<ApiTestModel> _logger = logger;

    /// <summary>
    /// Results of the API tests.
    /// </summary>
    public Dictionary<string, bool> TestResults { get; set; } = [];

    /// <summary>
    /// Handles GET requests to the page.
    /// </summary>
    public async Task OnGetAsync()
    {
        // Test user endpoints
        try
        {
            var users = await _userService.GetUsersAsync();
            TestResults["GetUsers"] = users.Count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing GetUsers API");
            TestResults["GetUsers"] = false;
        }

        // Test event endpoints
        try
        {
            var events = await _eventService.GetEventsAsync();
            TestResults["GetEvents"] = true;

            // If we have events, test related endpoints
            if (events.Count > 0)
            {
                var eventId = events[0].EventId;
                var eventDetails = await _eventService.GetEventByIdAsync(eventId);
                TestResults["GetEventById"] = eventDetails != null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing Event APIs");
            TestResults["GetEvents"] = false;
        }

        // Test request endpoints
        try
        {
            var requests = await _requestService.GetRequestsAsync();
            TestResults["GetRequests"] = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing GetRequests API");
            TestResults["GetRequests"] = false;
        }

        // Add more tests for other endpoints
    }
}
