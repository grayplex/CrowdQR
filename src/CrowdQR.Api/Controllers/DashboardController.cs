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
        // Verify event exists
        var eventExists = await _context.Events.AsNoTracking().AnyAsync(e => e.EventId == eventId);
        if (!eventExists)
        {
            return NotFound("Event not found");
        }

        // Get requests with vote counts via projection (single efficient query)
        var requests = await _context.Requests
            .AsNoTracking()
            .Where(r => r.EventId == eventId)
            .Select(r => new
            {
                r.RequestId,
                r.SongName,
                r.ArtistName,
                Requester = r.User.Username,
                VoteCount = r.Votes.Count,
                r.Status,
                r.CreatedAt
            })
            .ToListAsync();

        // Get active sessions separately (avoids cartesian with requests)
        var activeUsers = await _context.Sessions
            .AsNoTracking()
            .Where(s => s.EventId == eventId && s.LastSeen > DateTime.UtcNow.AddMinutes(-15))
            .Select(s => new
            {
                s.User.UserId,
                s.User.Username,
                s.LastSeen
            })
            .ToListAsync();

        // Build summary from projected results (in-memory filtering is fine on already-fetched data)
        var pendingRequests = requests
            .Where(r => r.Status == RequestStatus.Pending)
            .OrderByDescending(r => r.VoteCount)
            .ToList();

        var approvedRequests = requests
            .Where(r => r.Status == RequestStatus.Approved)
            .OrderByDescending(r => r.CreatedAt)
            .ToList();

        var rejectedRequests = requests
            .Where(r => r.Status == RequestStatus.Rejected)
            .OrderByDescending(r => r.CreatedAt)
            .ToList();

        var summary = new
        {
            EventId = eventId,
            TotalRequests = requests.Count,
            PendingRequests = pendingRequests.Count,
            ApprovedRequests = approvedRequests.Count,
            RejectedRequests = rejectedRequests.Count,
            TotalVotes = requests.Sum(r => r.VoteCount),
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
        var eventExists = await _context.Events.AsNoTracking().AnyAsync(e => e.EventId == eventId);
        if (!eventExists)
        {
            return NotFound("Event not found");
        }

        // Get top requests with server-side projection and SQL ordering
        var requests = await _context.Requests
            .AsNoTracking()
            .Where(r => r.EventId == eventId && r.Status == status)
            .Select(r => new
            {
                r.RequestId,
                r.SongName,
                r.ArtistName,
                Requester = r.User.Username,
                VoteCount = r.Votes.Count,
                r.CreatedAt
            })
            .OrderByDescending(r => r.VoteCount)
            .ThenBy(r => r.CreatedAt)
            .Take(count)
            .ToListAsync();

        return Ok(requests);
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
        var djExists = await _context.Users.AsNoTracking().AnyAsync(u => u.UserId == djUserId && u.Role == UserRole.DJ);
        if (!djExists)
        {
            return NotFound("DJ not found");
        }

        // Single projection query with SQL subqueries for counts
        var eventStats = await _context.Events
            .AsNoTracking()
            .Where(e => e.DjUserId == djUserId)
            .Select(e => new
            {
                e.EventId,
                e.Name,
                e.Slug,
                e.IsActive,
                e.CreatedAt,
                RequestCounts = new
                {
                    Total = e.Requests.Count,
                    Pending = e.Requests.Count(r => r.Status == RequestStatus.Pending),
                    Approved = e.Requests.Count(r => r.Status == RequestStatus.Approved),
                    Rejected = e.Requests.Count(r => r.Status == RequestStatus.Rejected)
                },
                TotalVotes = e.Requests.SelectMany(r => r.Votes).Count()
            })
            .ToListAsync();

        return Ok(eventStats);
    }
}