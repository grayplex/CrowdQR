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
/// API controller for managing user sessions.
/// </summary>
/// <param name="context">The database context.</param>
/// <param name="logger">The logger.</param>
/// /// <param name="hubNotificationService">The hub notification service.</param>
[ApiController]
[Route("api/[controller]")]
public class SessionController(
    CrowdQRContext context, 
    ILogger<SessionController> logger,
    IHubNotificationService hubNotificationService) : ControllerBase
{
    private readonly CrowdQRContext _context = context;
    private readonly ILogger<SessionController> _logger = logger;
    private readonly IHubNotificationService _hubNotificationService = hubNotificationService;

    // GET: api/session
    /// <summary>
    /// Gets all sessions.
    /// </summary>
    /// <returns>A list of all sessions.</returns>
    [HttpGet]
    [Authorize(Roles = "DJ")]
    public async Task<ActionResult<IEnumerable<object>>> GetSessions()
    {
        var sessions = await _context.Sessions
            .Include(s => s.User)
            .Include(s => s.Event)
            .ToListAsync();

        // Format the response to avoid circular references
        var formattedSessions = sessions.Select(s => new
        {
            s.SessionId,
            User = new { s.User.UserId, s.User.Username },
            Event = new { s.Event.EventId, s.Event.Name },
            s.ClientIP,
            s.LastSeen,
            s.RequestCount
        }).ToList();

        return Ok(formattedSessions);
    }

    // GET: api/session/5
    /// <summary>
    /// Gets a specific session by ID.
    /// </summary>
    /// <param name="id">The ID of the session to retrieve.</param>
    /// <returns>The requested session or a 404 Not Found response.</returns>
    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<object>> GetSession(int id)
    {
        var session = await _context.Sessions
            .Include(s => s.User)
            .Include(s => s.Event)
            .FirstOrDefaultAsync(s => s.SessionId == id);

        if (session == null)
        {
            return NotFound();
        }

        // Users should only access their own sessions
        if (!User.IsInRole("DJ") && session.UserId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0"))
        {
            return Forbid();
        }

        // Format the response to avoid circular references
        var formattedSession = new
        {
            session.SessionId,
            User = new { session.User.UserId, session.User.Username },
            Event = new { session.Event.EventId, session.Event.Name },
            session.ClientIP,
            session.LastSeen,
            session.RequestCount
        };

        return Ok(formattedSession);
    }

    // GET: api/session/event/5
    /// <summary>
    /// Gets all sessions for a specific event.
    /// </summary>
    /// <param name="eventId">The ID of the event.</param>
    /// <returns>A list of sessions for the specified event.</returns>
    [HttpGet("event/{eventId}")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<object>>> GetSessionsByEvent(int eventId)
    {
        // Get the event to check if the requesting user is the event DJ
        var @event = await _context.Events.FindAsync(eventId);
        if (@event == null)
        {
            return NotFound("Event not found");
        }

        // Only allow the event's DJ or other DJs to access all sessions
        if (!User.IsInRole("DJ") && @event.DjUserId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0"))
        {
            return Forbid();
        }

        // Get all sessions for the event
        var sessions = await _context.Sessions
            .Where(s => s.EventId == eventId)
            .Include(s => s.User)
            .ToListAsync();

        // Format the response to avoid circular references
        var formattedSessions = sessions.Select(s => new
        {
            s.SessionId,
            User = new { s.User.UserId, s.User.Username },
            s.EventId,
            s.ClientIP,
            s.LastSeen,
            s.RequestCount
        }).ToList();

        return Ok(formattedSessions);
    }

    // GET: api/session/user/5
    /// <summary>
    /// Gets all sessions for a specific user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <returns>A list of sessions for the specified user.</returns>
    [HttpGet("user/{userId}")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<object>>> GetSessionsByUser(int userId)
    {
        // Check to ensure users can only access their own sessions
        if (!User.IsInRole("DJ") && userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0"))
        {
            return Forbid();
        }

        var sessions = await _context.Sessions
            .Where(s => s.UserId == userId)
            .Include(s => s.Event)
            .ToListAsync();

        // Format the response to avoid circular references
        var formattedSessions = sessions.Select(s => new
        {
            s.SessionId,
            s.UserId,
            Event = new { s.Event.EventId, s.Event.Name },
            s.ClientIP,
            s.LastSeen,
            s.RequestCount
        }).ToList();

        return Ok(formattedSessions);
    }

    // GET: api/session/event/5/user/10
    /// <summary>
    /// Gets a session for a specific user in a specific event.
    /// </summary>
    /// <param name="eventId">The ID of the event.</param>
    /// <param name="userId">The ID of the user.</param>
    /// <returns>The matching session or a 404 Not Found response.</returns>
    [HttpGet("event/{eventId}/user/{userId}")]
    [Authorize]
    public async Task<ActionResult<object>> GetSessionByEventAndUser(int eventId, int userId)
    {
        // Check if this is the user's own session or the DJ for this event
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var @event = await _context.Events.FindAsync(eventId);

        if (!User.IsInRole("DJ") && userId != currentUserId &&
            (@event == null || @event.DjUserId != currentUserId))
        {
            return Forbid();
        }

        var session = await _context.Sessions
            .Include(s => s.User)
            .Include(s => s.Event)
            .FirstOrDefaultAsync(s => s.EventId == eventId && s.UserId == userId);

        if (session == null)
        {
            return NotFound();
        }

        // Format the response to avoid circular references
        var formattedSession = new
        {
            session.SessionId,
            User = new { session.User.UserId, session.User.Username },
            Event = new { session.Event.EventId, session.Event.Name },
            session.ClientIP,
            session.LastSeen,
            session.RequestCount
        };

        return Ok(formattedSession);
    }

    // POST: api/session
    /// <summary>
    /// Creates a new session or updates an existing one.
    /// </summary>
    /// <param name="sessionDto">The session data.</param>
    /// <returns>The created session and a 201 Created response, or an error.</returns>
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<Session>> CreateOrUpdateSession(SessionCreateDto sessionDto)
    {
        // Check to ensure users can only create sessions for themselves
        if (!User.IsInRole("DJ") && sessionDto.UserId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0"))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Check if user exists
        var user = await _context.Users.FindAsync(sessionDto.UserId);
        if (user == null)
        {
            return BadRequest("User does not exist");
        }

        // Check if event exists
        var eventExists = await _context.Events.AnyAsync(e => e.EventId == sessionDto.EventId);
        if (!eventExists)
        {
            return BadRequest("Event does not exist");
        }

        // Check if session already exists for this user and event
        var existingSession = await _context.Sessions
            .FirstOrDefaultAsync(s => s.UserId == sessionDto.UserId && s.EventId == sessionDto.EventId);

        bool isNewSession = existingSession == null;

        if (existingSession != null)
        {
            // Update existing session
            existingSession.LastSeen = DateTime.UtcNow;
            existingSession.ClientIP = sessionDto.ClientIP ?? existingSession.ClientIP;

            _context.Entry(existingSession).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return Ok(existingSession);
        }

        // Create new session
        var session = new Session
        {
            UserId = sessionDto.UserId,
            EventId = sessionDto.EventId,
            ClientIP = sessionDto.ClientIP,
            LastSeen = DateTime.UtcNow,
            RequestCount = 0
        };

        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        // Send SignalR notification that a user joined the event (only for new sessions)
        if (isNewSession)
        {
            try
            {
                await _hubNotificationService.NotifyUserJoinedEvent(sessionDto.EventId, user.Username);
                _logger.LogInformation("SignalR notification sent for user {Username} joining event {EventId}",
                    user.Username, sessionDto.EventId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send SignalR notification for user {UserId} joining event {EventId}",
                    sessionDto.UserId, sessionDto.EventId);
                // Don't fail the session creation if notification fails
            }
        }

        return CreatedAtAction(nameof(GetSession), new { id = session.SessionId }, session);
    }

    // PUT: api/session/5/increment-request-count
    /// <summary>
    /// Increments the request count for a session.
    /// </summary>
    /// <param name="id">The ID of the session.</param>
    /// <returns>A 204 No Content response, or an error.</returns>
    [HttpPut("{id}/increment-request-count")]
    [Authorize]
    public async Task<IActionResult> IncrementRequestCount(int id)
    {
        var session = await _context.Sessions.FindAsync(id);
        if (session == null)
        {
            return NotFound();
        }

        // Check if this is the user's own session or the DJ
        if (!User.IsInRole("DJ") && session.UserId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0"))
        {
            return Forbid();
        }

        session.RequestCount++;
        session.LastSeen = DateTime.UtcNow;

        _context.Entry(session).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // PUT: api/session/5/refresh
    /// <summary>
    /// Refreshes a session without incrementing request count
    /// </summary>
    /// <param name="id">The ID of the session to refresh.</param>
    /// <returns>A 204 No Content response, or an error</returns>
    [HttpPut("{id}/refresh")]
    [Authorize]
    public async Task<IActionResult> RefreshSession(int id)
    {
        var session = await _context.Sessions.FindAsync(id);
        if (session == null)
        {
            return NotFound();
        }

        // Check if this is the user's own session or the DJ
        if (!User.IsInRole("DJ") && session.UserId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0"))
        {
            return Forbid();
        }

        session.LastSeen = DateTime.UtcNow;

        _context.Entry(session).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/session/5
    /// <summary>
    /// Deletes a session.
    /// </summary>
    /// <param name="id">The ID of the session to delete.</param>
    /// <returns>A 204 No Content response, or an error.</returns>
    [HttpDelete("{id}")]
    [Authorize(Roles = "DJ")]
    public async Task<IActionResult> DeleteSession(int id)
    {
        var session = await _context.Sessions.FindAsync(id);
        if (session == null)
        {
            return NotFound();
        }

        _context.Sessions.Remove(session);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}