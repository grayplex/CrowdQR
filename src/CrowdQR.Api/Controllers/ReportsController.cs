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
        var eventInfo = await _context.Events
            .AsNoTracking()
            .Where(e => e.EventId == eventId && e.DjUserId == djUserId)
            .Select(e => new { e.EventId, e.Name, e.Slug, e.IsActive })
            .FirstOrDefaultAsync();

        if (eventInfo == null)
        {
            return NotFound("Event not found or access denied");
        }

        // Get all requests with vote counts using server-side projection
        var reportRows = await _context.Requests
            .AsNoTracking()
            .Where(r => r.EventId == eventId)
            .OrderByDescending(r => r.Votes.Count)
            .ThenBy(r => r.CreatedAt)
            .Select(r => new EventPerformanceReportRowDto
            {
                SongName = r.SongName,
                ArtistName = r.ArtistName,
                Requester = r.User.Username,
                VoteCount = r.Votes.Count,
                Status = r.Status.ToString(),
                RequestedAt = r.CreatedAt,
                StatusUpdatedAt = r.Status != Shared.Models.Enums.RequestStatus.Pending ? (DateTime?)DateTime.UtcNow : null
            })
            .ToListAsync();

        // Calculate summary statistics from projected data
        var uniqueParticipants = await _context.Requests
            .AsNoTracking()
            .Where(r => r.EventId == eventId)
            .Select(r => r.UserId)
            .Distinct()
            .CountAsync();

        var summary = new EventPerformanceReportSummaryDto
        {
            TotalRequests = reportRows.Count,
            ApprovedRequests = reportRows.Count(r => r.Status == Shared.Models.Enums.RequestStatus.Approved.ToString()),
            RejectedRequests = reportRows.Count(r => r.Status == Shared.Models.Enums.RequestStatus.Rejected.ToString()),
            TotalVotes = reportRows.Sum(r => r.VoteCount),
            UniqueParticipants = uniqueParticipants
        };

        // Build the report
        var report = new EventPerformanceReportDto
        {
            Title = $"Event Performance Report - {eventInfo.Name}",
            GeneratedAt = DateTime.UtcNow,
            Event = new EventDto
            {
                EventId = eventInfo.EventId,
                Name = eventInfo.Name,
                Slug = eventInfo.Slug,
                IsActive = eventInfo.IsActive
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
        var djUsername = await _context.Users
            .Where(u => u.UserId == djUserId)
            .Select(u => u.Username)
            .FirstOrDefaultAsync();

        if (djUsername == null)
        {
            return NotFound("DJ not found");
        }

        // Get all events for this DJ with aggregated metrics using server-side projection
        var reportRows = await _context.Events
            .AsNoTracking()
            .Where(e => e.DjUserId == djUserId)
            .Select(e => new DjAnalyticsReportRowDto
            {
                EventName = e.Name,
                EventSlug = e.Slug,
                EventDate = e.CreatedAt,
                TotalRequests = e.Requests.Count,
                ApprovedRequests = e.Requests.Count(r => r.Status == Shared.Models.Enums.RequestStatus.Approved),
                RejectedRequests = e.Requests.Count(r => r.Status == Shared.Models.Enums.RequestStatus.Rejected),
                TotalVotes = e.Requests.SelectMany(r => r.Votes).Count(),
                UniqueParticipants = e.Requests.Select(r => r.UserId).Distinct().Count(),
                IsActive = e.IsActive
            })
            .OrderByDescending(r => r.EventDate)
            .ToListAsync();

        var report = new DjAnalyticsReportDto
        {
            Title = $"DJ Analytics Report - {djUsername}",
            GeneratedAt = DateTime.UtcNow,
            DjName = djUsername,
            Rows = reportRows,
            Summary = new DjAnalyticsReportSummaryDto
            {
                TotalEvents = reportRows.Count,
                ActiveEvents = reportRows.Count(e => e.IsActive),
                TotalRequests = reportRows.Sum(r => r.TotalRequests),
                TotalVotes = reportRows.Sum(r => r.TotalVotes),
                MostPopularEvent = reportRows.OrderByDescending(r => r.TotalRequests).FirstOrDefault()?.EventName,
                HighestEventVoteCount = reportRows.Count != 0 ? reportRows.Max(r => r.TotalVotes) : 0
            }
        };

        return Ok(report);
    }
}