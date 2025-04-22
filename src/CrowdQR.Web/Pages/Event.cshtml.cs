using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using CrowdQR.Shared.Models.DTOs;
using CrowdQR.Shared.Models.Enums;
using CrowdQR.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CrowdQR.Web.Pages;

/// <summary>
/// Page model for the event audience interface.
/// </summary>
/// <param name="eventService">The event service.</param>
/// <param name="requestService">The request service.</param>
/// <param name="voteService"> The vote service.</param>
/// <param name="userService"> The user service.</param>
/// <param name="sessionManager"> The session manager.</param>
/// <param name="sessionService"> The session service.</param>
/// <param name="logger"> The logger.</param>
public class EventModel(
    EventService eventService,
    RequestService requestService,
    VoteService voteService,
    UserService userService,
    SessionManager sessionManager,
    SessionService sessionService,
    ILogger<EventModel> logger) : PageModel
{
    private readonly EventService _eventService = eventService;
    private readonly RequestService _requestService = requestService;
    private readonly VoteService _voteService = voteService;
    private readonly UserService _userService = userService;
    private readonly SessionManager _sessionManager = sessionManager;
    private readonly SessionService _sessionService = sessionService;
    private readonly ILogger<EventModel> _logger = logger;

    /// <summary>
    /// The slug/code for the event.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? Slug { get; set; }

    /// <summary>
    /// The name of the event.
    /// </summary>
    public string EventName { get; set; } = string.Empty;

    /// <summary>
    /// The unique identifier for the event.
    /// </summary>
    public int EventId { get; set; }

    /// <summary>
    /// The list of song requests for this event.
    /// </summary>
    public List<RequestDto> Requests { get; set; } = [];

    /// <summary>
    /// The user's new song request input.
    /// </summary>
    [BindProperty]
    public SongRequestInputModel NewSongRequest { get; set; } = new();

    /// <summary>
    /// The unique identifier for the current user.
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// The username of the current user.
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Whether the user is authenticated.
    /// </summary>
    public bool IsAuthenticated { get; set; }

    /// <summary>
    /// The message to display in case of an error.
    /// </summary>
    [TempData]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// The message to display in case of a successful operation.
    /// </summary>
    [TempData]
    public string? SuccessMessage { get; set; }

    /// <summary>
    /// Handles GET requests to the page.
    /// </summary>
    public async Task<IActionResult> OnGetAsync()
    {
        // Check if user is authenticated
        UserId = _sessionManager.GetCurrentUserId();
        UserName = _sessionManager.GetCurrentUsername();
        IsAuthenticated = _sessionManager.IsLoggedIn();

        // If no user is logged in, automatically create a temporary user
        if (!IsAuthenticated && !string.IsNullOrEmpty(Slug))
        {
            string tempUsername = $"guest_{Guid.NewGuid().ToString()[..8]}";
            bool registered = await _sessionManager.RegisterAndLoginAsync(tempUsername);

            if (!registered)
            {
                ErrorMessage = "Failed to create temporary user. Please try again.";
                return Page();
            }

            UserId = _sessionManager.GetCurrentUserId();
            UserName = _sessionManager.GetCurrentUsername();
            IsAuthenticated = true;
        }

        // If no slug is provided, show the join form
        if (string.IsNullOrEmpty(Slug))
        {
            return Page();
        }

        // Get event data
        try
        {
            var eventData = await _eventService.GetEventBySlugAsync(Slug);
            if (eventData == null)
            {
                // If using demo slug, load demo data
                if (Slug.Equals("demo", StringComparison.CurrentCultureIgnoreCase))
                {
                    LoadDemoData();
                    return Page();
                }

                ErrorMessage = "Event not found. Please check the event code and try again.";
                Slug = null;
                return Page();
            }

            EventId = eventData.EventId;
            EventName = eventData.Name;

            // Join the event if authenticated
            if (IsAuthenticated && UserId.HasValue)
            {
                bool joined = await _sessionManager.JoinEventAsync(EventId, Slug);
                if (!joined)
                {
                    _logger.LogWarning("Failed to join event {EventId} for user {UserId}", EventId, UserId);
                }
            }

            // Get requests for this event
            var requests = await _requestService.GetRequestsByEventAsync(EventId);

            // Map API requests to view model
            Requests = [.. requests.Select(r => new RequestDto
            {
                RequestId = r.RequestId,
                SongName = r.SongName,
                ArtistName = r.ArtistName,
                UserId = r.UserId, // We'll need to fetch usernames in a real implementation
                VoteCount = r.VoteCount,
                Status = r.Status,
                // Store user's vote status to disable vote button if already voted
                UserHasVoted = r.Votes?.Any(v => v.UserId == UserId) ?? false
            })];

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading event {Slug}", Slug);
            ErrorMessage = "An error occurred while loading the event. Please try again.";
            return Page();
        }
    }

    /// <summary>
    /// Handles POST requests for submitting a new song request.
    /// </summary>
    /// <returns>Redirect to the event page.</returns>
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return await OnGetAsync();
        }

        UserId = _sessionManager.GetCurrentUserId();
        if (!UserId.HasValue)
        {
            ErrorMessage = "You must be logged in to submit a request.";
            return RedirectToPage(new { slug = Slug });
        }

        // Get event ID from slug
        if (string.IsNullOrEmpty(Slug))
        {
            ErrorMessage = "Invalid event code.";
            return RedirectToPage();
        }

        var eventData = await _eventService.GetEventBySlugAsync(Slug);
        if (eventData == null)
        {
            ErrorMessage = "Event not found.";
            return RedirectToPage();
        }

        // Create the request
        var requestDto = new RequestCreateDto
        {
            UserId = UserId.Value,
            EventId = eventData.EventId,
            SongName = NewSongRequest.SongName,
            ArtistName = NewSongRequest.ArtistName
        };

        try
        {
            var (success, request) = await _requestService.CreateRequestAsync(requestDto);

            if (!success || request == null)
            {
                ErrorMessage = "Failed to submit your request. Please try again.";
                return RedirectToPage(new { slug = Slug });
            }

            // Increment session request count
            var sessionId = _sessionManager.GetApiSessionId();
            if (sessionId.HasValue)
            {
                await _sessionService.IncrementRequestCountAsync(sessionId.Value);
            }

            SuccessMessage = "Your request has been submitted successfully!";
            return RedirectToPage(new { slug = Slug });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating request for user {UserId} in event {EventId}", UserId, eventData.EventId);
            ErrorMessage = "An error occurred while submitting your request. Please try again.";
            return RedirectToPage(new { slug = Slug });
        }
    }

    /// <summary>
    /// Handles POST requests for voting on a song request.
    /// </summary>
    /// <param name="requestId">The ID of the request to vote for.</param>
    /// <returns>Redirect to the event page.</returns>
    public async Task<IActionResult> OnPostVoteAsync(int requestId)
    {
        UserId = _sessionManager.GetCurrentUserId();
        if (!UserId.HasValue)
        {
            ErrorMessage = "You must be logged in to vote.";
            return RedirectToPage(new { slug = Slug });
        }

        try
        {
            var voteDto = new VoteCreateDto
            {
                UserId = UserId.Value,
                RequestId = requestId
            };

            var (success, vote) = await _voteService.CreateVoteAsync(voteDto);

            if (!success)
            {
                ErrorMessage = "Failed to submit your vote. You may have already voted for this request.";
                return RedirectToPage(new { slug = Slug });
            }

            SuccessMessage = "Your vote has been counted!";
            return RedirectToPage(new { slug = Slug });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error voting for request {RequestId} by user {UserId}", requestId, UserId);
            ErrorMessage = "An error occurred while submitting your vote. Please try again.";
            return RedirectToPage(new { slug = Slug });
        }
    }

    /// <summary>
    /// Handles POST requests for removing a vote from a song request.
    /// </summary>
    /// <param name="requestId">The ID of the request to remove vote from.</param>
    /// <returns>Redirect to the event page.</returns>
    public async Task<IActionResult> OnPostRemoveVoteAsync(int requestId)
    {
        UserId = _sessionManager.GetCurrentUserId();
        if (!UserId.HasValue)
        {
            ErrorMessage = "You must be logged in to remove your vote.";
            return RedirectToPage(new { slug = Slug });
        }

        try
        {
            bool success = await _voteService.DeleteVoteByUserAndRequestAsync(UserId.Value, requestId);

            if (!success)
            {
                ErrorMessage = "Failed to remove your vote.";
                return RedirectToPage(new { slug = Slug });
            }

            SuccessMessage = "Your vote has been removed.";
            return RedirectToPage(new { slug = Slug });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing vote for request {RequestId} by user {UserId}", requestId, UserId);
            ErrorMessage = "An error occurred while removing your vote. Please try again.";
            return RedirectToPage(new { slug = Slug });
        }
    }

    private void LoadDemoData()
    {
        EventName = "Saturday Night Fever";
        EventId = 1;

        Requests =
        [
            new() {
                RequestId = 1,
                SongName = "Stayin' Alive",
                ArtistName = "Bee Gees",
                Requester = "partygoer1",
                VoteCount = 5,
                Status = RequestStatus.Pending,
                UserHasVoted = true
            },
            new() {
                RequestId = 2,
                SongName = "Don't Stop 'Til You Get Enough",
                ArtistName = "Michael Jackson",
                Requester = "dancefloor_queen",
                VoteCount = 3,
                Status = RequestStatus.Approved,
                UserHasVoted = false
            },
            new() {
                RequestId = 3,
                SongName = "Good Times",
                ArtistName = "Chic",
                Requester = "music_lover",
                VoteCount = 2,
                Status = RequestStatus.Pending,
                UserHasVoted = false
            },
            new() {
                RequestId = 4,
                SongName = "Le Freak",
                ArtistName = "Chic",
                Requester = "rhythm_fanatic",
                VoteCount = 1,
                Status = RequestStatus.Pending,
                UserHasVoted = false
            },
            new() {
                RequestId = 5,
                SongName = "Boogie Wonderland",
                ArtistName = "Earth, Wind & Fire",
                Requester = "beat_enthusiast",
                VoteCount = 0,
                Status = RequestStatus.Rejected,
                UserHasVoted = false
            }
        ];
    }

    /// <summary>
    /// Model for new song request input.
    /// </summary>
    public class SongRequestInputModel
    {
        /// <summary>
        /// The name of the requested song.
        /// </summary>
        [Required]
        [StringLength(255, MinimumLength = 1)]
        [Display(Name = "Song Name")]
        public string SongName { get; set; } = string.Empty;

        /// <summary>
        /// The name of the artist (optional).
        /// </summary>
        [StringLength(255)]
        [Display(Name = "Artist Name (Optional)")]
        public string? ArtistName { get; set; }

        /// <summary>
        /// Additional notes for the DJ (optional).
        /// </summary>
        [StringLength(500)]
        [Display(Name = "Notes (Optional)")]
        public string? Notes { get; set; }
    }
}