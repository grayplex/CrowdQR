using CrowdQR.Api.Data;
using CrowdQR.Api.Models;
using CrowdQR.Shared.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;

namespace CrowdQR.Api.Controllers;

/// <summary>
/// API controller for generating reports.
/// </summary>
/// <param name="context">The database context.</param>
/// <param name="logger">The logger.</param>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "DJ")]
public class ReportsController(CrowdQRContext context, ILogger<ReportsController> logger) : ControllerBase
{
    private readonly CrowdQRContext _context = context;
    private readonly ILogger<ReportsController> _logger = logger;

    /// <summary>
    /// Generates an event performance report.
    /// </summary>
    /// <param name="eventId">The ID of the event to generate a report for.</param>
    /// <returns>Event performance report with multiple columns, rows, and timestamps.</returns>
    [HttpGet("event-performance/{eventId}")]
    public async Task<ActionResult<EventPerformanceReportDto>> GetEventPerformanceReport(int eventId)
    {
        // Verify the event exists and the DJ owns it
        var djUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var eventEntity = await _context.Events
            .Include(e => e.DJ)
            .FirstOrDefaultAsync(e => e.EventId == eventId && e.DjUserId == djUserId);

        if (eventEntity == null)
        {
            return NotFound("Event not found or access denied");
        }

        // Get all requests with their votes and users
        var requests = await _context.Requests
            .Where(r => r.EventId == eventId)
            .Include(r => r.User)
            .Include(r => r.Votes)
            .OrderByDescending(r => r.Votes.Count)
            .ThenBy(r => r.CreatedAt)
            .ToListAsync();

        // Build report rows
        var reportRows = requests.Select(r => new EventPerformanceReportRowDto
        {
            SongName = r.SongName,
            ArtistName = r.ArtistName,
            Requester = r.User.Username,
            VoteCount = r.Votes.Count,
            Status = r.Status.ToString(),
            RequestedAt = r.CreatedAt,
            StatusUpdatedAt = r.Status != Shared.Models.Enums.RequestStatus.Pending ? DateTime.UtcNow : null // Mock status update time
        }).ToList();

        // Calculate summary statistics
        var uniqueParticipants = requests.Select(r => r.UserId).Distinct().Count();
        var totalVotes = requests.Sum(r => r.Votes.Count);

        var summary = new EventPerformanceReportSummaryDto
        {
            TotalRequests = requests.Count,
            ApprovedRequests = requests.Count(r => r.Status == Shared.Models.Enums.RequestStatus.Approved),
            RejectedRequests = requests.Count(r => r.Status == Shared.Models.Enums.RequestStatus.Rejected),
            TotalVotes = totalVotes,
            UniqueParticipants = uniqueParticipants
        };

        // Build the report
        var report = new EventPerformanceReportDto
        {
            Title = $"Event Performance Report - {eventEntity.Name}",
            GeneratedAt = DateTime.UtcNow,
            Event = new EventDto
            {
                EventId = eventEntity.EventId,
                Name = eventEntity.Name,
                Slug = eventEntity.Slug,
                IsActive = eventEntity.IsActive
            },
            Rows = reportRows,
            Summary = summary
        };

        return Ok(report);
    }

    /// <summary>
    /// Generates a DJ analytics report across all events.
    /// </summary>
    /// <returns>DJ analytics report with performance metrics across all events.</returns>
    [HttpGet("dj-analytics")]
    public async Task<ActionResult<DjAnalyticsReportDto>> GetDjAnalyticsReport()
    {
        var djUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var djUser = await _context.Users.FindAsync(djUserId);

        if (djUser == null)
        {
            return NotFound("DJ not found");
        }

        // Get all events for this DJ
        var events = await _context.Events
            .Where(e => e.DjUserId == djUserId)
            .Include(e => e.Requests)
                .ThenInclude(r => r.Votes)
            .ToListAsync();

        // Build report rows - one per event
        var reportRows = events.Select(e => new DjAnalyticsReportRowDto
        {
            EventName = e.Name,
            EventSlug = e.Slug,
            EventDate = e.CreatedAt,
            TotalRequests = e.Requests.Count,
            ApprovedRequests = e.Requests.Count(r => r.Status == Shared.Models.Enums.RequestStatus.Approved),
            RejectedRequests = e.Requests.Count(r => r.Status == Shared.Models.Enums.RequestStatus.Rejected),
            TotalVotes = e.Requests.Sum(r => r.Votes.Count),
            UniqueParticipants = e.Requests.Select(r => r.UserId).Distinct().Count(),
            IsActive = e.IsActive
        }).OrderByDescending(r => r.EventDate).ToList();

        var report = new DjAnalyticsReportDto
        {
            Title = $"DJ Analytics Report - {djUser.Username}",
            GeneratedAt = DateTime.UtcNow,
            DjName = djUser.Username,
            Rows = reportRows,
            Summary = new DjAnalyticsReportSummaryDto
            {
                TotalEvents = events.Count,
                ActiveEvents = events.Count(e => e.IsActive),
                TotalRequests = events.Sum(e => e.Requests.Count),
                TotalVotes = events.SelectMany<Event, Vote>(e => e.Requests.SelectMany<Request, Vote>(r => r.Votes)).Count(),
                MostPopularEvent = reportRows.OrderByDescending(r => r.TotalRequests).FirstOrDefault()?.EventName,
                HighestEventVoteCount = reportRows.Count != 0 ? reportRows.Max(r => r.TotalVotes) : 0
            }
        };

        return Ok(report);
    }
}