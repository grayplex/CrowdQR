using CrowdQR.Shared.Models.DTOs;
using CrowdQR.Shared.Models.Enums;
using CrowdQR.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace CrowdQR.Web.Pages.Admin;

/// <summary>
/// Page model for the DJ admin dashboard interface.
/// </summary>
/// <remarks>
/// Initializes a new instance of the DashboardModel class.
/// </remarks>
/// <param name="eventService">The event service.</param>
/// <param name="requestService">The request service.</param>
/// <param name="dashboardService">The dashboard service.</param>
/// <param name="sessionManager">The session manager.</param>
/// <param name="logger">The logger.</param>
public class DashboardModel(
    EventService eventService,
    RequestService requestService,
    DashboardService dashboardService,
    SessionManager sessionManager,
    ILogger<DashboardModel> logger) : PageModel
{
    private readonly EventService _eventService = eventService;
    private readonly RequestService _requestService = requestService;
    private readonly DashboardService _dashboardService = dashboardService;
    private readonly SessionManager _sessionManager = sessionManager;
    private readonly ILogger<DashboardModel> _logger = logger;

    /// <summary>
    /// The name of the current event.
    /// </summary>
    public string EventName { get; set; } = string.Empty;

    /// <summary>
    /// The URL-friendly slug for the current event.
    /// </summary>
    public string EventSlug { get; set; } = string.Empty;

    /// <summary>
    /// List of song requests with pending status.
    /// </summary>
    public List<RequestDto> PendingRequests { get; set; } = [];

    /// <summary>
    /// List of song requests with approved status.
    /// </summary>
    public List<RequestDto> ApprovedRequests { get; set; } = [];

    /// <summary>
    /// List of song requests with rejected status.
    /// </summary>
    public List<RequestDto> RejectedRequests { get; set; } = [];

    /// <summary>
    /// Current search term used to filter requests.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string SearchTerm { get; set; } = string.Empty;

    /// <summary>
    /// Number of currently active users in the event.
    /// </summary>
    public int ActiveUsers { get; set; }

    /// <summary>
    /// The ID of the event to display on the dashboard.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public int? EventId { get; set; }

    /// <summary>
    /// The currently active tab (pending, approved, or rejected).
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string Tab { get; set; } = "pending";

    /// <summary>
    /// The ID of the current user
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// Indicates whether the user is authenticated.
    /// </summary>
    public bool IsAuthenticated { get; set; }

    /// <summary>
    /// Indicates whether the user is a DJ.
    /// </summary>
    public bool IsDj { get; set; }

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
    /// Handles GET requests to the dashboard page.
    /// </summary>
    public async Task<IActionResult> OnGetAsync()
    {
        // Check if user is authenticated
        UserId = _sessionManager.GetCurrentUserId();
        IsAuthenticated = _sessionManager.IsLoggedIn();
        IsDj = _sessionManager.IsDj();

        // Redirect non-authenticated or non-DJ users
        if (!IsAuthenticated)
        {
            // Instead of redirecting, we could show a login page in a real implementation
            ErrorMessage = "You must be logged in to access the DJ dashboard.";
            return RedirectToPage("/Index");
        }

        if (!IsDj)
        {
            ErrorMessage = "You do not have permission to access the DJ dashboard.";
            return RedirectToPage("/Index");
        }

        try
        {
            // If no event ID is provided, get the DJ's events and use the first one
            if (EventId == null)
            {
                if (UserId == null)
                {
                    ErrorMessage = "You must be logged in to access the DJ dashboard.";
                    return RedirectToPage("/Index");
                }

                var events = await _eventService.GetEventsByDjAsync(UserId.Value);
                if (events.Count == 0)
                {
                    // For demo purposes, use a default event
                    LoadDemoData();
                    return Page();
                }

                EventId = events[0].EventId;
                EventName = events[0].Name;
                EventSlug = events[0].Slug;
            }
            else
            {
                // Get event details
                var eventData = await _eventService.GetEventByIdAsync(EventId.Value);
                if (eventData == null)
                {
                    ErrorMessage = "Event not found.";
                    return RedirectToPage("/Index");
                }

                EventName = eventData.Name;
                EventSlug = eventData.Slug;
            }

            // Get dashboard data
            var summary = await _dashboardService.GetEventSummaryAsync(EventId.Value);
            if (summary != null)
            {
                ActiveUsers = summary.ActiveUsers;

                // Map requests based on status
                MapRequestsFromSummary(summary);
            }
            else
            {
                // Fallback to direct request API if summary fails
                var requests = await _requestService.GetRequestsByEventAsync(EventId.Value);
                MapRequestsFromList(requests);
            }

            // Apply search filter if provided
            ApplySearch();

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard for event {EventId}", EventId);

            // For demo purposes, load demo data on error
            LoadDemoData();
            return Page();
        }
    }

    /// <summary>
    /// Handles POST requests for approving a request.
    /// </summary>
    /// <param name="requestId">The ID of the request to approve.</param>
    /// <returns>Redirect to the dashboard page.</returns>
    public async Task<IActionResult> OnPostApproveAsync(int requestId)
    {
        if (!_sessionManager.IsDj())
        {
            ErrorMessage = "You do not have permission to approve requests.";
            return RedirectToPage();
        }

        try
        {
            var statusDto = new RequestStatusUpdateDto
            {
                Status = RequestStatus.Approved
            };

            bool success = await _requestService.UpdateRequestStatusAsync(requestId, statusDto);

            if (!success)
            {
                ErrorMessage = "Failed to approve the request. Please try again.";
                return RedirectToPage(new { eventId = EventId, tab = Tab, searchTerm = SearchTerm });
            }

            SuccessMessage = "Request approved successfully!";
            return RedirectToPage(new { eventId = EventId, tab = "approved", searchTerm = SearchTerm });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving request {RequestId}", requestId);
            ErrorMessage = "An error occurred while approving the request. Please try again.";
            return RedirectToPage(new { eventId = EventId, tab = Tab, searchTerm = SearchTerm });
        }
    }

    /// <summary>
    /// Handles POST requests for rejecting a request.
    /// </summary>
    /// <param name="requestId">The ID of the request to reject.</param>
    /// <returns>Redirect to the dashboard page.</returns>
    public async Task<IActionResult> OnPostRejectAsync(int requestId)
    {
        if (!_sessionManager.IsDj())
        {
            ErrorMessage = "You do not have permission to reject requests.";
            return RedirectToPage();
        }

        try
        {
            var statusDto = new RequestStatusUpdateDto
            {
                Status = RequestStatus.Rejected
            };

            bool success = await _requestService.UpdateRequestStatusAsync(requestId, statusDto);

            if (!success)
            {
                ErrorMessage = "Failed to reject the request. Please try again.";
                return RedirectToPage(new { eventId = EventId, tab = Tab, searchTerm = SearchTerm });
            }

            SuccessMessage = "Request rejected successfully!";
            return RedirectToPage(new { eventId = EventId, tab = "rejected", searchTerm = SearchTerm });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting request {RequestId}", requestId);
            ErrorMessage = "An error occurred while rejecting the request. Please try again.";
            return RedirectToPage(new { eventId = EventId, tab = Tab, searchTerm = SearchTerm });
        }
    }

    /// <summary>
    /// Handles POST requests for moving a request back to pending.
    /// </summary>
    /// <param name="requestId">The ID of the request to move to pending.</param>
    /// <returns>Redirect to the dashboard page.</returns>
    public async Task<IActionResult> OnPostMoveToPendingAsync(int requestId)
    {
        if (!_sessionManager.IsDj())
        {
            ErrorMessage = "You do not have permission to modify requests.";
            return RedirectToPage();
        }

        try
        {
            var statusDto = new RequestStatusUpdateDto
            {
                Status = RequestStatus.Pending
            };

            bool success = await _requestService.UpdateRequestStatusAsync(requestId, statusDto);

            if (!success)
            {
                ErrorMessage = "Failed to move the request to pending. Please try again.";
                return RedirectToPage(new { eventId = EventId, tab = Tab, searchTerm = SearchTerm });
            }

            SuccessMessage = "Request moved to pending successfully!";
            return RedirectToPage(new { eventId = EventId, tab = "pending", searchTerm = SearchTerm });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving request {RequestId} to pending", requestId);
            ErrorMessage = "An error occurred while updating the request. Please try again.";
            return RedirectToPage(new { eventId = EventId, tab = Tab, searchTerm = SearchTerm });
        }
    }

    private void LoadDemoData()
    {
        EventName = "Saturday Night Fever";
        EventSlug = "saturday-night-fever";
        ActiveUsers = 24;

        PendingRequests = [
            new() {
                RequestId = 1,
                SongName = "Stayin' Alive",
                ArtistName = "Bee Gees",
                Requester = "partygoer1",
                VoteCount = 5,
                Status = RequestStatus.Pending,
                CreatedAt = DateTime.Now.AddMinutes(-15)
            },
            new() {
                RequestId = 3,
                SongName = "Good Times",
                ArtistName = "Chic",
                Requester = "music_lover",
                VoteCount = 2,
                Status = RequestStatus.Pending,
                CreatedAt = DateTime.Now.AddMinutes(-12)
            },
            new() {
                RequestId = 4,
                SongName = "Le Freak",
                ArtistName = "Chic",
                Requester = "rhythm_fanatic",
                VoteCount = 1,
                Status = RequestStatus.Pending,
                CreatedAt = DateTime.Now.AddMinutes(-8)
            }
        ];

        ApprovedRequests = [
            new() {
                RequestId = 2,
                SongName = "Don't Stop 'Til You Get Enough",
                ArtistName = "Michael Jackson",
                Requester = "dancefloor_queen",
                VoteCount = 3,
                Status = RequestStatus.Approved,
                CreatedAt = DateTime.Now.AddMinutes(-20),
                UpdatedAt = DateTime.Now.AddMinutes(-5)
            },
            new() {
                RequestId = 6,
                SongName = "Night Fever",
                ArtistName = "Bee Gees",
                Requester = "beat_enthusiast",
                VoteCount = 4,
                Status = RequestStatus.Approved,
                CreatedAt = DateTime.Now.AddMinutes(-25),
                UpdatedAt = DateTime.Now.AddMinutes(-10)
            }
        ];

        RejectedRequests = [
            new() {
                RequestId = 5,
                SongName = "Boogie Wonderland",
                ArtistName = "Earth, Wind & Fire",
                Requester = "beat_enthusiast",
                VoteCount = 0,
                Status = RequestStatus.Rejected,
                CreatedAt = DateTime.Now.AddMinutes(-30),
                UpdatedAt = DateTime.Now.AddMinutes(-15)
            }
        ];

        // Apply search filter if provided
        ApplySearch();
    }

    private void MapRequestsFromSummary(EventSummaryDto summary)
    {
        // Map pending requests
#pragma warning disable IDE0305 // Simplify collection initialization
        PendingRequests = summary.TopRequests.Select(r => new RequestDto
        {
            RequestId = r.RequestId,
            SongName = r.SongName,
            ArtistName = r.ArtistName,
            Requester = r.Requester,
            VoteCount = r.VoteCount,
            Status = RequestStatus.Pending,
            CreatedAt = r.CreatedAt
        }).ToList();

        // Map approved requests
        ApprovedRequests = summary.RecentlyApproved.Select(r => new RequestDto
        {
            RequestId = r.RequestId,
            SongName = r.SongName,
            ArtistName = r.ArtistName,
            Requester = r.Requester,
            VoteCount = r.VoteCount,
            Status = RequestStatus.Approved,
            CreatedAt = r.CreatedAt,
            // In the real API, we would have the approval time
            UpdatedAt = DateTime.Now.AddMinutes(-5)
        }).ToList();

        // Map rejected requests
        RejectedRequests = summary.RecentlyRejected.Select(r => new RequestDto
        {
            RequestId = r.RequestId,
            SongName = r.SongName,
            ArtistName = r.ArtistName,
            Requester = r.Requester,
            VoteCount = r.VoteCount,
            Status = RequestStatus.Rejected,
            CreatedAt = r.CreatedAt,
            // In the real API, we would have the rejection time
            UpdatedAt = DateTime.Now.AddMinutes(-10)
        }).ToList();
#pragma warning restore IDE0305 // Simplify collection initialization
    }

    private void MapRequestsFromList(List<RequestDto> requests)
    {
        // Group requests by status
        foreach (var request in requests)
        {
            var model = new RequestDto
            {
                RequestId = request.RequestId,
                SongName = request.SongName,
                ArtistName = request.ArtistName,
                Requester = request.Requester, // We would fetch username in a real implementation
                VoteCount = request.VoteCount,
                Status = request.Status,
                CreatedAt = request.CreatedAt
            };

            if (request.Status == RequestStatus.Pending)
            {
                PendingRequests.Add(model);
            }
            else if (request.Status == RequestStatus.Approved)
            {
                model.UpdatedAt = DateTime.Now.AddMinutes(-5); // Mock data
                ApprovedRequests.Add(model);
            }
            else if (request.Status == RequestStatus.Rejected)
            {
                model.UpdatedAt = DateTime.Now.AddMinutes(-10); // Mock data
                RejectedRequests.Add(model);
            }
        }

        // Order pending requests by vote count
        PendingRequests = [.. PendingRequests.OrderByDescending(r => r.VoteCount)];

        // Order approved and rejected requests by time
        ApprovedRequests = [.. ApprovedRequests.OrderByDescending(r => r.UpdatedAt)];
        RejectedRequests = [.. RejectedRequests.OrderByDescending(r => r.UpdatedAt)];

        // Set active users (mock data)
        ActiveUsers = 10;
    }

    private void ApplySearch()
    {
        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            string search = SearchTerm.ToLower();

#pragma warning disable IDE0305 // Simplify collection initialization
            PendingRequests = PendingRequests
                .Where(r => r.SongName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                            (r.ArtistName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                            r.Requester.Contains(search, StringComparison.OrdinalIgnoreCase))
                .ToList();

            ApprovedRequests = ApprovedRequests
                .Where(r => r.SongName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                            (r.ArtistName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                            r.Requester.Contains(search, StringComparison.OrdinalIgnoreCase))
                .ToList();

            RejectedRequests = RejectedRequests
                .Where(r => r.SongName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                            (r.ArtistName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                            r.Requester.Contains(search, StringComparison.OrdinalIgnoreCase))
                .ToList();
#pragma warning restore IDE0305 // Simplify collection initialization
        }
    }
}