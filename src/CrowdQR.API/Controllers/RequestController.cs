using CrowdQR.Api.Data;
using CrowdQR.Api.Models;
using CrowdQR.Shared.Models.DTOs;
using CrowdQR.Shared.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrowdQR.Api.Controllers;

/// <summary>
/// API controller for managing song requests.
/// </summary>
/// <param name="context">The database context.</param>
/// <param name="logger">The logger.</param>
[ApiController]
[Route("api/[controller]")]
public class RequestController(CrowdQRContext context, ILogger<RequestController> logger) : ControllerBase
{
    private readonly CrowdQRContext _context = context;
    private readonly ILogger<RequestController> _logger = logger;

    // GET: api/request
    /// <summary>
    /// Gets all requests
    /// </summary>
    /// <returns>A list of all requests.</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetRequests()
    {
        var requests = await _context.Requests.ToListAsync();

        // Format the response to avoid circular references
        var formattedRequests = requests.Select(r => new
        {
            r.RequestId,
            r.UserId,
            r.EventId,
            r.SongName,
            r.ArtistName,
            r.Status,
            r.CreatedAt
        }).ToList();

        return Ok(formattedRequests);
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
        var request = await _context.Requests
            .Include(r => r.Votes)
            .FirstOrDefaultAsync(r => r.RequestId == id);

        if (request == null)
        {
            return NotFound();
        }

        // Create a version of the request with vote count but without circular references
        var formattedRequest = new
        {
            request.RequestId,
            request.UserId,
            request.EventId,
            request.SongName,
            request.ArtistName,
            request.Status,
            request.CreatedAt,
            VoteCount = request.Votes.Count,
            Votes = request.Votes.Select(v => new { v.VoteId, v.UserId, v.CreatedAt }).ToList()
        };

        return formattedRequest;
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
            .Where(r => r.EventId == eventId)
            .Include(r => r.Votes)
            .ToListAsync();

        // Create a version of the requests with vote counts but without circular references
        var formattedRequests = requests.Select(r => new
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
        }).ToList(); // Make sure to call ToList() to materialize the query

        return Ok(formattedRequests);
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
    public async Task<IActionResult> UpdateRequestStatus(int id, RequestStatusUpdateDto statusDto)
    {
        var request = await _context.Requests.FindAsync(id);
        if (request == null)
        {
            return NotFound();
        }

        request.Status = statusDto.Status;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/request/5
    /// <summary>
    /// Deletes a request.
    /// </summary>
    /// <param name="id">The ID of the request to delete.</param>
    /// <returns>A 204 No Content response, or an error.</returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRequest(int id)
    {
        var request = await _context.Requests.FindAsync(id);
        if (request == null)
        {
            return NotFound();
        }

        _context.Requests.Remove(request);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
