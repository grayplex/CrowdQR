using CrowdQR.Shared.Models.DTOs;
using CrowdQR.Shared.Models.Enums;
using CrowdQR.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
    ApiService apiService,
    ILogger<ApiTestModel> logger) : PageModel
{
    private readonly EventService _eventService = eventService;
    private readonly RequestService _requestService = requestService;
    private readonly UserService _userService = userService;
    private readonly VoteService _voteService = voteService;
    private readonly SessionService _sessionService = sessionService;
    private readonly DashboardService _dashboardService = dashboardService;
    private readonly ApiService _apiService = apiService;
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
        // Initialize test results
        TestResults = [];

        // Test basic ping endpoint
        try
        {
            var pingResult = await _apiService.GetAsync<object>("api/ping");
            TestResults["API Connection (Ping)"] = pingResult != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing Ping API");
            TestResults["API Connection (Ping)"] = false;
        }

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

            if (requests.Count > 0)
            {
                var requestId = requests[0].RequestId;
                var requestDetails = await _requestService.GetRequestByIdAsync(requestId);
                TestResults["GetRequestById"] = requestDetails != null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing GetRequests API");
            TestResults["GetRequests"] = false;
        }

        // Test vote endpoints
        try
        {
            var votes = await _voteService.GetVotesByRequestAsync(1);
            TestResults["GetVotesByRequest"] = votes != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing GetVotesByRequest API");
            TestResults["GetVotesByRequest"] = false;
        }

        // Test session endpoints
        try
        {
            var sessions = await _sessionService.GetSessionByEventAndUserAsync(_eventService.GetEventsAsync().Result[0].EventId, _userService.GetUsersAsync().Result[0].UserId);
            TestResults["GetSessionByEventAndUserAsync"] = sessions != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing GetSessionByEventAndUserAsync API");
            TestResults["GetSessionByEventAndUserAsync"] = false;
        }

        // Test dashboard endpoints
        try
        {
            var eventSummary = await _dashboardService.GetEventSummaryAsync(_eventService.GetEventsAsync().Result[0].EventId);
            TestResults["GetEventSummary"] = eventSummary != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing GetEventSummary API");
            TestResults["GetEventSummary"] = false;
        }
    }
}
