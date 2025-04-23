using CrowdQR.Api.Data;
using CrowdQR.Api.Models;
using CrowdQR.Api.Services;
using CrowdQR.Shared.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CrowdQR.Api.Controllers;

/// <summary>
/// API controller for managing votes on song requests.
/// </summary>
/// <param name="context">The database context.</param>
/// <param name="logger">The logger.</param>
/// /// <param name="hubNotificationService">The hub notification service.</param>
[ApiController]
[Route("api/[controller]")]
public class VoteController(
    CrowdQRContext context, 
    ILogger<VoteController> logger,
    IHubNotificationService hubNotificationService) : ControllerBase
{
    private readonly CrowdQRContext _context = context;
    private readonly ILogger<VoteController> _logger = logger;
    private readonly IHubNotificationService _hubNotificationService = hubNotificationService;

    // GET: api/vote
    /// <summary>
    /// Gets all votes.
    /// </summary>
    /// <returns>A list of all votes.</returns>
    [HttpGet]
    [Authorize(Roles = "DJ")]
    public async Task<ActionResult<IEnumerable<object>>> GetVotes()
    {
        var votes = await _context.Votes.ToListAsync();

        // Convert to plain objects without reference tracking
        var plainVotes = votes.Select(v => new
        {
            v.VoteId,
            v.RequestId,
            v.UserId,
            v.CreatedAt
        }).ToList();

        return Ok(plainVotes);
    }

    // GET: api/vote/5
    /// <summary>
    /// Gets a specific vote by ID.
    /// </summary>
    /// <param name="id">The ID of the vote to retrieve.</param>
    /// <returns>The requested vote or a 404 Not Found response.</returns>
    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<object>> GetVote(int id)
    {
        var vote = await _context.Votes.FindAsync(id);

        if (vote == null)
        {
            return NotFound();
        }

        // Convert to plain object
        var plainVote = new
        {
            vote.VoteId,
            vote.RequestId,
            vote.UserId,
            vote.CreatedAt
        };

        return plainVote;
    }

    // GET: api/vote/request/5
    /// <summary>
    /// Gets all votes for a specific request.
    /// </summary>
    /// <param name="requestId">The ID of the request.</param>
    /// <returns>A list of votes for the specified request.</returns>
    [HttpGet("request/{requestId}")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<object>>> GetVotesByRequest(int requestId)
    {
        var votes = await _context.Votes
            .Where(v => v.RequestId == requestId)
            .ToListAsync();

        // Convert to plain objects without reference tracking
        var plainVotes = votes.Select(v => new
        {
            v.VoteId,
            v.RequestId,
            v.UserId,
            v.CreatedAt
        }).ToList();

        return Ok(plainVotes);
    }

    // POST: api/vote
    /// <summary>
    /// Creates a new vote.
    /// </summary>
    /// <param name="voteDto">The vote data.</param>
    /// <returns>The created vote and a 201 Created response, or an error.</returns>
    [HttpPost]
    public async Task<ActionResult<Vote>> CreateVote(VoteCreateDto voteDto)
    {
        // Check to ensure users can only vote as themselves
        if (!User.IsInRole("DJ") && voteDto.UserId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0"))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Check if request exists and load its votes and event ID
        var request = await _context.Requests
            .Include(r => r.Votes)
            .FirstOrDefaultAsync(r => r.RequestId == voteDto.RequestId);

        if (request == null)
        {
            return BadRequest("Request does not exist");
        }

        // Check if user exists
        var userExists = await _context.Users.AnyAsync(u => u.UserId == voteDto.UserId);
        if (!userExists)
        {
            return BadRequest("User does not exist");
        }

        // Check if user already voted for this request
        var existingVote = await _context.Votes
            .FirstOrDefaultAsync(v => v.RequestId == voteDto.RequestId && v.UserId == voteDto.UserId);

        if (existingVote != null)
        {
            return Conflict("User has already voted for this request");
        }

        var vote = new Vote
        {
            RequestId = voteDto.RequestId,
            UserId = voteDto.UserId
        };

        _context.Votes.Add(vote);

        try
        {
            await _context.SaveChangesAsync();

            // Calculate new vote count
            int voteCount = request.Votes.Count + 1;

            // Send SignalR notification about the new vote
            try
            {
                await _hubNotificationService.NotifyVoteAdded(request.EventId, request.RequestId, voteCount);
                _logger.LogInformation("SignalR notification sent for vote added to request {RequestId}, new count: {VoteCount}",
                    request.RequestId, voteCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send SignalR notification for vote added to request {RequestId}", request.RequestId);
                // Don't fail the vote creation if notification fails
            }
        }
        catch (DbUpdateException)
        {
            // Handle the unique constraint violation
            return Conflict("User has already voted for this request");
        }

        return CreatedAtAction(nameof(GetVote), new { id = vote.VoteId }, vote);
    }

    // DELETE: api/vote/5
    /// <summary>
    /// Deletes a vote.
    /// </summary>
    /// <param name="id">The ID of the vote to delete.</param>
    /// <returns>A 204 No Content response, or an error.</returns>
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteVote(int id)
    {
        var vote = await _context.Votes.FindAsync(id);
        if (vote == null)
        {
            return NotFound();
        }

        // Only allow DJs or the vote owner to delete the vote
        if (!User.IsInRole("DJ") && vote.UserId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0"))
        {
            return Forbid();
        }

        _context.Votes.Remove(vote);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/vote/user/5/request/10
    /// <summary>
    /// Deletes a vote by User ID and request ID.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="requestId">The ID of the request.</param>
    /// <returns>A 204 No Content response, or an error.</returns>
    [HttpDelete("user/{userId}/request/{requestId}")]
    [Authorize]
    public async Task<IActionResult> DeleteVoteByUserAndRequest(int userId, int requestId)
    {
        // Only allow DJs or the vote owner to delete the vote
        if (!User.IsInRole("DJ") && userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0"))
        {
            return Forbid();
        }

        var vote = await _context.Votes
            .FirstOrDefaultAsync(v => v.UserId == userId && v.RequestId == requestId);

        if (vote == null)
        {
            return NotFound();
        }

        _context.Votes.Remove(vote);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}