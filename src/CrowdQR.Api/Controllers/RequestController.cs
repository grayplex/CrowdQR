using CrowdQR.Api.Data;
using CrowdQR.Api.Models;
using CrowdQR.Api.Services;
using CrowdQR.Shared.Models.DTOs;
using CrowdQR.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CrowdQR.Api.Controllers;

/// <summary>
/// API controller for managing song requests.
/// </summary>
/// <param name="context">The database context.</param>
/// <param name="logger">The logger.</param>
/// <param name="hubNotificationService">The hub notification service.</param>
[ApiController]
[Route("api/[controller]")]
public class RequestController(
    CrowdQRContext context, 
    ILogger<RequestController> logger,
    IHubNotificationService hubNotificationService) : ControllerBase
{
    private readonly CrowdQRContext _context = context;
    private readonly ILogger<RequestController> _logger = logger;
    private readonly IHubNotificationService _hubNotificationService = hubNotificationService;

    // GET: api/request
    /// <summary>
    /// Gets all requests
    /// </summary>
    /// <returns>A list of all requests.</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetRequests()
    {
        var requests = await _context.Requests
            .AsNoTracking()
            .Select(r => new
            {
                r.RequestId,
                r.UserId,
                r.EventId,
                r.SongName,
                r.ArtistName,
                r.Status,
                r.CreatedAt
            })
            .ToListAsync();

        return Ok(requests);
    }

    // GET: api/request/5
    /// <summary>
    /// Gets a specific request by ID.
    /// </summary>
    /// <param name="id">The ID of the request to be retrieved.</param>
    /// <returns>The requested request or a 404 Not Found response.</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetRequest(int id)
    {
        var formattedRequest = await _context.Requests
            .AsNoTracking()
            .Where(r => r.RequestId == id)
            .Select(r => new
            {
                r.RequestId,
                r.UserId,
                r.EventId,
                r.SongName,
                r.ArtistName,
                r.Status,
                r.CreatedAt,
                VoteCount = r.Votes.Count,
                Votes = r.Votes.Select(v => new { v.VoteId, v.UserId, v.CreatedAt }).ToList()
            })
            .FirstOrDefaultAsync();

        if (formattedRequest == null)
        {
            return NotFound();
        }

        return Ok(formattedRequest);
    }

    // GET: api/request/event/5
    /// <summary>
    /// Gets all requests for a specific event.
    /// </summary>
    /// <param name="eventId">The ID of the event.</param>
    /// <returns>A list of requests for the specified event.</returns>
    [HttpGet("event/{eventId}")]
    public async Task<ActionResult<IEnumerable<object>>> GetRequestsByEvent(int eventId)
    {
        var requests = await _context.Requests
            .AsNoTracking()
            .Where(r => r.EventId == eventId)
            .Select(r => new
            {
                r.RequestId,
                r.UserId,
                r.EventId,
                r.SongName,
                r.ArtistName,
                r.Status,
                r.CreatedAt,
                VoteCount = r.Votes.Count,
                Votes = r.Votes.Select(v => new { v.VoteId, v.UserId, v.CreatedAt }).ToList()
            })
            .ToListAsync();

        return Ok(requests);
    }

    // POST: api/request
    /// <summary>
    /// Creates a new request
    /// </summary>
    /// <param name="requestDto">The request data.</param>
    /// <returns>The created request and a 201 Created response, or an error.</returns>
    [HttpPost]
    public async Task<ActionResult<Request>> CreateRequest (RequestCreateDto requestDto)
    {
        // Check if the user is creating a request as themselves
        if (!User.IsInRole("DJ") && requestDto.UserId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0"))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Check if event exists
        var eventExists = await _context.Events.AnyAsync(e => e.EventId == requestDto.EventId);
        if (!eventExists)
        {
            return BadRequest("Event does not exist");
        }

        // Check if user exists
        var userExists = await _context.Users.AnyAsync(u => u.UserId == requestDto.UserId);
        if (!userExists)
        {
            return BadRequest("User does not exist");
        }

        var request = new Request
        {
            UserId = requestDto.UserId,
            EventId = requestDto.EventId,
            SongName = requestDto.SongName,
            ArtistName = requestDto.ArtistName,
            Status = RequestStatus.Pending
        };

        _context.Requests.Add(request);
        await _context.SaveChangesAsync();

        // Get username for notification
        var username = await _context.Users
            .Where(u => u.UserId == requestDto.UserId)
            .Select(u => u.Username)
            .FirstOrDefaultAsync();

        // Send SignalR notification
        try
        {
            await _hubNotificationService.NotifyRequestAdded(request.EventId, request.RequestId, username ?? "Unknown");
            _logger.LogInformation("SignalR notification sent for new request {RequestId} in event {EventId}",
                request.RequestId, request.EventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SignalR notification for new request {RequestId}", request.RequestId);
            // Don't fail the request if SignalR notification fails
        }

        return CreatedAtAction(nameof(GetRequest), new { id = request.RequestId }, request );
    }

    // PUT: api/request/5/status
    /// <summary>
    /// Updates the status of a request.
    /// </summary>
    /// <param name="id">The ID of the request to update.</param>
    /// <param name="statusDto">The new status data.</param>
    /// <returns>A 204 No Content response, or an error.</returns>
    [HttpPut("{id}/status")]
    [Authorize(Roles = "DJ")]
    public async Task<IActionResult> UpdateRequestStatus(int id, RequestStatusUpdateDto statusDto)
    {
        var request = await _context.Requests.FindAsync(id);
        if (request == null)
        {
            return NotFound();
        }

        // Store old status for logging
        var oldStatus = request.Status;

        request.Status = statusDto.Status;
        await _context.SaveChangesAsync();

        // Send SignalR notification
        try
        {
            await _hubNotificationService.NotifyRequestStatusUpdated(
                request.EventId,
                request.RequestId,
                request.Status.ToString());

            _logger.LogInformation(
                "SignalR notification sent for request {RequestId} status change from {OldStatus} to {NewStatus}",
                request.RequestId, oldStatus, request.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send SignalR notification for request {RequestId} status update",
                request.RequestId);
            // Don't fail the request if SignalR notification fails
        }

        return NoContent();
    }

    // DELETE: api/request/5
    /// <summary>
    /// Deletes a request.
    /// </summary>
    /// <param name="id">The ID of the request to delete.</param>
    /// <returns>A 204 No Content response, or an error.</returns>
    [HttpDelete("{id}")]
    [Authorize(Roles = "DJ")]
    public async Task<IActionResult> DeleteRequest(int id)
    {
        var request = await _context.Requests.FindAsync(id);
        if (request == null)
        {
            return NotFound();
        }

        // Store event ID for notification after deletion
        // var eventId = request.EventId;
        // var requestId = request.RequestId;

        _context.Requests.Remove(request);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
