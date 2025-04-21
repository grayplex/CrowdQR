using CrowdQR.Api.Data;
using CrowdQR.Api.Models;
using CrowdQR.Api.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrowdQR.Api.Controllers;

/// <summary>
/// API controller for managing votes on song requests.
/// </summary>
/// <param name="context">The database context.</param>
/// <param name="logger">The logger.</param>
[ApiController]
[Route("api/[controller]")]
public class VoteController(CrowdQRContext context, ILogger<VoteController> logger) : ControllerBase
{
    private readonly CrowdQRContext _context = context;
    private readonly ILogger<VoteController> _logger = logger;

    // GET: api/vote
    /// <summary>
    /// Gets all votes.
    /// </summary>
    /// <returns>A list of all votes.</returns>
    [HttpGet]
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
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Check if request exists
        var requestExists = await _context.Requests.AnyAsync(r => r.RequestId == voteDto.RequestId);
        if (!requestExists)
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
    public async Task<IActionResult> DeleteVote(int id)
    {
        var vote = await _context.Votes.FindAsync(id);
        if (vote == null)
        {
            return NotFound();
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
    public async Task<IActionResult> DeleteVoteByUserAndRequest(int userId, int requestId)
    {
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