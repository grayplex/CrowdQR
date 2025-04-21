using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CrowdQR.Web.Pages;

/// <summary>
/// Page model for the event audience interface.
/// </summary>
public class EventModel : PageModel
{
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
    /// The list of song requests for this event.
    /// </summary>
    public List<SongRequestModel> Requests { get; set; } = [];

    /// <summary>
    /// The user's new song request input.
    /// </summary>
    [BindProperty]
    public SongRequestInputModel NewSongRequest { get; set; } = new();

    /// <summary>
    /// Handles GET requests to the page.
    /// </summary>
    public void OnGet()
    {
        // In a real implementation, we would fetch the event and its requests 
        // from the API based on the slug
        if (Slug == "demo")
        {
            LoadDemoData();
        }
    }

    /// <summary>
    /// Handles POST requests for submitting a new song request.
    /// </summary>
    /// <returns>Redirect to the event page.</returns>
    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
        {
            LoadDemoData();
            return Page();
        }

        // In a real implementation, we would post the request to the API
        // For now, just redirect back to the page
        return RedirectToPage(new { slug = Slug });
    }

    private void LoadDemoData()
    {
        EventName = "Saturday Night Fever";

        Requests =
        [
            new() {
                Id = 1,
                SongName = "Stayin' Alive",
                ArtistName = "Bee Gees",
                Requester = "partygoer1",
                VoteCount = 5,
                Status = "Pending"
            },
            new() {
                Id = 2,
                SongName = "Don't Stop 'Til You Get Enough",
                ArtistName = "Michael Jackson",
                Requester = "dancefloor_queen",
                VoteCount = 3,
                Status = "Approved"
            },
            new() {
                Id = 3,
                SongName = "Good Times",
                ArtistName = "Chic",
                Requester = "music_lover",
                VoteCount = 2,
                Status = "Pending"
            },
            new() {
                Id = 4,
                SongName = "Le Freak",
                ArtistName = "Chic",
                Requester = "rhythm_fanatic",
                VoteCount = 1,
                Status = "Pending"
            },
            new() {
                Id = 5,
                SongName = "Boogie Wonderland",
                ArtistName = "Earth, Wind & Fire",
                Requester = "beat_enthusiast",
                VoteCount = 0,
                Status = "Rejected"
            }
        ];
    }

    /// <summary>
    /// Represents a song request in the system.
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