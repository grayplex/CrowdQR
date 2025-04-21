using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CrowdQR.Web.Pages.Admin;

/// <summary>
/// Page model for the DJ admin dashboard interface.
/// </summary>
public class DashboardModel : PageModel
{
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
    public List<SongRequestModel> PendingRequests { get; set; } = [];

    /// <summary>
    /// List of song requests with approved status.
    /// </summary>
    public List<SongRequestModel> ApprovedRequests { get; set; } = [];

    /// <summary>
    /// List of song requests with rejected status.
    /// </summary>
    public List<SongRequestModel> RejectedRequests { get; set; } = [];

    /// <summary>
    /// Current search term used to filter requests.
    /// </summary>
    public string SearchTerm { get; set; } = string.Empty;

    /// <summary>
    /// Number of currently active users in the event.
    /// </summary>
    public int ActiveUsers { get; set; }

    /// <summary>
    /// The ID of the event to display on the dashboard.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? EventId { get; set; }

    /// <summary>
    /// The currently active tab (pending, approved, or rejected).
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string Tab { get; set; } = "pending";

    /// <summary>
    /// Handles GET requests to the dashboard page.
    /// </summary>
    /// <param name="searchTerm">Optional search term to filter requests.</param>
    public void OnGet(string searchTerm = "")
    {
        // In a real implementation, we would fetch data from the API
        // For now, just load demo data
        SearchTerm = searchTerm;
        LoadDemoData();
    }

    private void LoadDemoData()
    {
        EventName = "Saturday Night Fever";
        EventSlug = "saturday-night-fever";
        ActiveUsers = 24;

        PendingRequests = [
            new() {
                Id = 1,
                SongName = "Stayin' Alive",
                ArtistName = "Bee Gees",
                Requester = "partygoer1",
                VoteCount = 5,
                Status = "Pending",
                RequestTime = DateTime.Now.AddMinutes(-15)
            },
            new() {
                Id = 3,
                SongName = "Good Times",
                ArtistName = "Chic",
                Requester = "music_lover",
                VoteCount = 2,
                Status = "Pending",
                RequestTime = DateTime.Now.AddMinutes(-12)
            },
            new() {
                Id = 4,
                SongName = "Le Freak",
                ArtistName = "Chic",
                Requester = "rhythm_fanatic",
                VoteCount = 1,
                Status = "Pending",
                RequestTime = DateTime.Now.AddMinutes(-8)
            }
        ];

        ApprovedRequests = [
            new() {
                Id = 2,
                SongName = "Don't Stop 'Til You Get Enough",
                ArtistName = "Michael Jackson",
                Requester = "dancefloor_queen",
                VoteCount = 3,
                Status = "Approved",
                RequestTime = DateTime.Now.AddMinutes(-20),
                ActionTime = DateTime.Now.AddMinutes(-5)
            },
            new() {
                Id = 6,
                SongName = "Night Fever",
                ArtistName = "Bee Gees",
                Requester = "beat_enthusiast",
                VoteCount = 4,
                Status = "Approved",
                RequestTime = DateTime.Now.AddMinutes(-25),
                ActionTime = DateTime.Now.AddMinutes(-10)
            }
        ];

        RejectedRequests = [
            new() {
                Id = 5,
                SongName = "Boogie Wonderland",
                ArtistName = "Earth, Wind & Fire",
                Requester = "beat_enthusiast",
                VoteCount = 0,
                Status = "Rejected",
                RequestTime = DateTime.Now.AddMinutes(-30),
                ActionTime = DateTime.Now.AddMinutes(-15)
            }
        ];

        // Apply search filter if provided
        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            string search = SearchTerm.ToLower();
#pragma warning disable IDE0305 // Simplify collection initialization
            PendingRequests = PendingRequests
                .Where(r => r.SongName.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
                            (r.ArtistName?.ToLower().Contains(search, StringComparison.CurrentCultureIgnoreCase) ?? false) ||
                            r.Requester.Contains(search, StringComparison.CurrentCultureIgnoreCase))
                .ToList();
#pragma warning restore IDE0305 // Simplify collection initialization

#pragma warning disable IDE0305 // Simplify collection initialization
            ApprovedRequests = ApprovedRequests
                .Where(r => r.SongName.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
                            (r.ArtistName?.ToLower().Contains(search, StringComparison.CurrentCultureIgnoreCase) ?? false) ||
                            r.Requester.Contains(search, StringComparison.CurrentCultureIgnoreCase))
                .ToList();
#pragma warning restore IDE0305 // Simplify collection initialization

#pragma warning disable IDE0305 // Simplify collection initialization
            RejectedRequests = RejectedRequests
                .Where(r => r.SongName.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
                            (r.ArtistName?.ToLower().Contains(search, StringComparison.CurrentCultureIgnoreCase) ?? false) ||
                            r.Requester.Contains(search, StringComparison.CurrentCultureIgnoreCase))
                .ToList();
#pragma warning restore IDE0305 // Simplify collection initialization
        }
    }

    /// <summary>
    /// Represents a song request in the admin dashboard.
    /// </summary>
    public class SongRequestModel
    {
        /// <summary>
        /// The unique identifier for the request.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The name of the requested song.
        /// </summary>
        public string SongName { get; set; } = string.Empty;

        /// <summary>
        /// The name of the artist (optional).
        /// </summary>
        public string? ArtistName { get; set; }

        /// <summary>
        /// The username of the person who requested the song.
        /// </summary>
        public string Requester { get; set; } = string.Empty;

        /// <summary>
        /// The number of votes this request has received.
        /// </summary>
        public int VoteCount { get; set; }

        /// <summary>
        /// The current status of the request (Pending, Approved, Rejected).
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// When the song was initially requested.
        /// </summary>
        public DateTime RequestTime { get; set; }

        /// <summary>
        /// When the request was approved or rejected (null if still pending).
        /// </summary>
        public DateTime? ActionTime { get; set; }
    }
}