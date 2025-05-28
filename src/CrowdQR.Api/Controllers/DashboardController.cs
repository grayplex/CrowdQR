using CrowdQR.Api.Data;
using CrowdQR.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrowdQR.Api.Controllers;

/// <summary>
/// API controller for DJ dashboard operations.
/// </summary>
/// <param name="context">The database context.</param>
/// <param name="logger">The logger.</param>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "DJ")]
public class DashboardController(CrowdQRContext context, ILogger<DashboardController> logger) : ControllerBase
{
    private readonly CrowdQRContext _context = context;
    private readonly ILogger<DashboardController> _logger = logger;

    // GET: api/dashboard/event/5/summary
    /// <summary>
    /// Gets a summary of an event for the DJ dashboard.
    /// </summary>
    /// <param name="eventId">The ID of the event.</param>
    /// <returns>A summary of the event including requests, votes, and active users.</returns>
    [HttpGet("event/{eventId}/summary")]
    public async Task<ActionResult<object>> GetEventSummary(int eventId)
    {
        // Ensure event exists
        var eventExists = await _context.Events.AnyAsync(e => e.EventId == eventId);
        if (!eventExists)
        {
            return NotFound("Event not found");
        }

        // Get requests with their vote counts
        var requests = await _context.Requests
            .Where(r => r.EventId == eventId)
            .Include(r => r.User)
            .Include(r => r.Votes)
            .ToListAsync();

        // Get active user sessions
        var activeSessions = await _context.Sessions
            .Where(s => s.EventId == eventId && s.LastSeen > DateTime.UtcNow.AddMinutes(-15))
            .Include(s => s.User)
            .ToListAsync();

        var pendingRequests = requests
            .Where(r => r.Status == RequestStatus.Pending)
            .OrderByDescending(r => r.Votes.Count)
            .Select(r => new
            {
                r.RequestId,
                r.SongName,
                r.ArtistName,
                Requester = r.User.Username,
                VoteCount = r.Votes.Count,
                r.CreatedAt
            })
            .ToList();

        var approvedRequests = requests
            .Where(r => r.Status == RequestStatus.Approved)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.RequestId,
                r.SongName,
                r.ArtistName,
                Requester = r.User.Username,
                VoteCount = r.Votes.Count,
                r.CreatedAt
            })
            .ToList();

        var rejectedRequests = requests
            .Where(r => r.Status == RequestStatus.Rejected)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.RequestId,
                r.SongName,
                r.ArtistName,
                Requester = r.User.Username,
                VoteCount = r.Votes.Count,
                r.CreatedAt
            })
            .ToList();

        var activeUsers = activeSessions
            .Select(s => new
            {
                s.User.UserId,
                s.User.Username,
                s.LastSeen
            })
            .ToList();

        var summary = new
        {
            EventId = eventId,
            TotalRequests = requests.Count,
            PendingRequests = pendingRequests.Count,
            ApprovedRequests = approvedRequests.Count,
            RejectedRequests = rejectedRequests.Count,
            TotalVotes = requests.Sum(r => r.Votes.Count),
            ActiveUsers = activeUsers.Count,
            TopRequests = pendingRequests.Take(10).ToList(),
            RecentlyApproved = approvedRequests.Take(5).ToList(),
            RecentlyRejected = rejectedRequests.Take(5).ToList(),
            ActiveUsersList = activeUsers
        };

        return Ok(summary);
    }

    // GET: api/dashboard/event/5/top-requests
    /// <summary>
    /// Gets the top requests for an event.
    /// </summary>
    /// <param name="eventId">The ID of the event.</param>
    /// <param name="status">Filter by request status. Default is pending.</param>
    /// <param name="count">Number of requests to return. Default is 10.</param>
    /// <returns>The top requests for the event.</returns>
    [HttpGet("event/{eventId}/top-requests")]
    public async Task<ActionResult<object>> GetTopRequests(
        int eventId,
        [FromQuery] RequestStatus status = RequestStatus.Pending,
        [FromQuery] int count = 10)
    {
        // Ensure event exists
        var eventExists = await _context.Events.AnyAsync(e => e.EventId == eventId);
        if (!eventExists)
        {
            return NotFound("Event not found");
        }

        // Get top requests
        var requests = await _context.Requests
            .Where(r => r.EventId == eventId && r.Status == status)
            .Include(r => r.User)
            .Include(r => r.Votes)
            .OrderByDescending(r => r.Votes.Count)
            .ThenBy(r => r.CreatedAt)
            .Take(count)
            .ToListAsync();

        var formattedRequests = requests
            .Select(r => new
            {
                r.RequestId,
                r.SongName,
                r.ArtistName,
                Requester = r.User.Username,
                VoteCount = r.Votes.Count,
                r.CreatedAt
            })
            .ToList();

        return Ok(formattedRequests);
    }

    // GET: api/dashboard/dj/5/event-stats
    /// <summary>
    /// Gets statistics for all events of a DJ.
    /// </summary>
    /// <param name="djUserId">The ID of the DJ user.</param>
    /// <returns>Statistics for all events of the DJ.</returns>
    [HttpGet("dj/{djUserId}/event-stats")]
    public async Task<ActionResult<object>> GetDJEventStats(int djUserId)
    {
        // Ensure DJ exists
        var djExists = await _context.Users.AnyAsync(u => u.UserId == djUserId && u.Role == UserRole.DJ);
        if (!djExists)
        {
            return NotFound("DJ not found");
        }

        // Get all events for the DJ
        var events = await _context.Events
            .Where(e => e.DjUserId == djUserId)
            .ToListAsync();

        var eventIds = events.Select(e => e.EventId).ToList();

        // Get requests for all events
        var requests = await _context.Requests
            .Where(r => eventIds.Contains(r.EventId))
            .Include(r => r.Votes)
            .ToListAsync();

        // Group requests by event
        var eventStats = events.Select(e => new
        {
            e.EventId,
            e.Name,
            e.Slug,
            e.IsActive,
            e.CreatedAt,
            RequestCounts = new
            {
                Total = requests.Count(r => r.EventId == e.EventId),
                Pending = requests.Count(r => r.EventId == e.EventId && r.Status == RequestStatus.Pending),
                Approved = requests.Count(r => r.EventId == e.EventId && r.Status == RequestStatus.Approved),
                Rejected = requests.Count(r => r.EventId == e.EventId && r.Status == RequestStatus.Rejected)
            },
            TotalVotes = requests.Where(r => r.EventId == e.EventId).Sum(r => r.Votes.Count)
        }).ToList();

        return Ok(eventStats);
    }
}