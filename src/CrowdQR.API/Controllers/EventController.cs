using CrowdQR.Api.Data;
using CrowdQR.Api.Models;
using CrowdQR.Shared.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrowdQR.Api.Controllers;

/// <summary>
/// API controller for managing DJ events.
/// </summary>
/// <param name="context">The database context.</param>
/// <param name="logger">The logger.</param>
[ApiController]
[Route("api/[controller]")]
public class EventController(CrowdQRContext context, ILogger<EventController> logger) : ControllerBase
{
    private readonly CrowdQRContext _context = context;
    private readonly ILogger<EventController> _logger = logger;

    // GET: api/event
    /// <summary>
    /// Gets all events.
    /// </summary>
    /// <returns>A list of all events.</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetEvents()
    {
        var events = await _context.Events
            .Include(e => e.DJ)
            .ToListAsync();

        // Format the response to avoid circular references
        var formattedEvents = events.Select(e => new
        {
            e.EventId,
            e.Name,
            e.Slug,
            e.CreatedAt,
            e.IsActive,
            DJ = new
            {
                e.DJ.UserId,
                e.DJ.Username
            }
        }).ToList();

        return Ok(formattedEvents);
    }

    // GET: api/event/5
    /// <summary>
    /// Gets a specific event by ID.
    /// </summary>
    /// <param name="id">The ID of the event to retrieve.</param>
    /// <returns>The requested event or a 404 Not Found response.</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetEvent(int id)
    {
        var @event = await _context.Events
            .Include(e => e.DJ)
            .Include(e => e.Requests)
                .ThenInclude(r => r.Votes)
            .FirstOrDefaultAsync(e => e.EventId == id);

        if (@event == null)
        {
            return NotFound();
        }

        // Create a version of the event without circular references
        var formattedEvent = new
        {
            @event.EventId,
            @event.Name,
            @event.Slug,
            @event.CreatedAt,
            @event.IsActive,
            DJ = new
            {
                @event.DJ.UserId,
                @event.DJ.Username
            },
            Requests = @event.Requests.Select(r => new
            {
                r.RequestId,
                r.SongName,
                r.ArtistName,
                r.Status,
                r.CreatedAt,
                VoteCount = r.Votes.Count
            }).ToList()
        };

        return formattedEvent;
    }

    // GET: api/event/slug/{slug}
    /// <summary>
    /// Gets a specific event by its slug.
    /// </summary>
    /// <param name="slug">The slug of the event.</param>
    /// <returns>The requested event or a 404 Not Found response.</returns>
    [HttpGet("slug/{slug}")]
    public async Task<ActionResult<object>> GetEventBySlug(string slug)
    {
        var @event = await _context.Events
            .Include(e => e.DJ)
            .Include(e => e.Requests)
                .ThenInclude(r => r.Votes)
            .FirstOrDefaultAsync(e => e.Slug == slug);

        if (@event == null)
        {
            return NotFound();
        }

        // Create a version of the event without circular references
        var formattedEvent = new
        {
            @event.EventId,
            @event.Name,
            @event.Slug,
            @event.CreatedAt,
            @event.IsActive,
            DJ = new
            {
                @event.DJ.UserId,
                @event.DJ.Username
            },
            Requests = @event.Requests.Select(r => new
            {
                r.RequestId,
                r.SongName,
                r.ArtistName,
                r.Status,
                r.CreatedAt,
                VoteCount = r.Votes.Count
            }).ToList()
        };

        return formattedEvent;
    }

    // GET: api/event/dj/5
    /// <summary>
    /// Gets all events for a specific DJ.
    /// </summary>
    /// <param name="djUserId">The ID of the DJ user.</param>
    /// <returns>A list of events for the specified DJ.</returns>
    [HttpGet("dj/{djUserId}")]
    public async Task<ActionResult<IEnumerable<object>>> GetEventsByDJ(int djUserId)
    {
        var events = await _context.Events
            .Where(e => e.DjUserId == djUserId)
            .Include(e => e.DJ)
            .ToListAsync();

        // Format the response to avoid circular references
        var formattedEvents = events.Select(e => new
        {
            e.EventId,
            e.Name,
            e.Slug,
            e.CreatedAt,
            e.IsActive,
            DJ = new
            {
                e.DJ.UserId,
                e.DJ.Username
            }
        }).ToList();

        return Ok(formattedEvents);
    }

    // POST: api/event
    /// <summary>
    /// Creates a new event.
    /// </summary>
    /// <param name="eventDto">The event data.</param>
    /// <returns>The created event and a 201 Created response, or an error.</returns>
    [HttpPost]
    public async Task<ActionResult<Event>> CreateEvent(EventCreateDto eventDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Check if DJ user exists
        var djExists = await _context.Users.AnyAsync(u => u.UserId == eventDto.DjUserId);
        if (!djExists)
        {
            return BadRequest("DJ does not exist");
        }

        // Check if slug is unique
        var slugExists = await _context.Events.AnyAsync(e => e.Slug == eventDto.Slug);
        if (slugExists)
        {
            return Conflict("An event with this slug already exists");
        }

        var @event = new Event
        {
            DjUserId = eventDto.DjUserId,
            Name = eventDto.Name,
            Slug = eventDto.Slug,
            IsActive = eventDto.IsActive ?? true
        };

        _context.Events.Add(@event);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEvent), new { id = @event.EventId }, @event);
    }

    // PUT: api/event/5
    /// <summary>
    /// Updates an event.
    /// </summary>
    /// <param name="id">The ID of the event to update.</param>
    /// <param name="eventDto">The updated event data.</param>
    /// <returns>A 204 No Content response, or an error.</returns>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEvent(int id, EventUpdateDto eventDto)
    {
        var @event = await _context.Events.FindAsync(id);
        if (@event == null)
        {
            return NotFound();
        }

        // Only update provided fields
        if (!string.IsNullOrEmpty(eventDto.Name))
        {
            @event.Name = eventDto.Name;
        }

        if (!string.IsNullOrEmpty(eventDto.Slug))
        {
            // Check if new slug is unique
            var slugExists = await _context.Events.AnyAsync(e => e.Slug == eventDto.Slug && e.EventId != id);
            if (slugExists)
            {
                return Conflict("An event with this slug already exists");
            }
            @event.Slug = eventDto.Slug;
        }

        if (eventDto.IsActive.HasValue)
        {
            @event.IsActive = eventDto.IsActive.Value;
        }

        _context.Entry(@event).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await EventExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    // DELETE: api/event/5
    /// <summary>
    /// Deletes an event.
    /// </summary>
    /// <param name="id">The ID of the event to delete.</param>
    /// <returns>A 204 No Content response, or an error.</returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var @event = await _context.Events.FindAsync(id);
        if (@event == null)
        {
            return NotFound();
        }

        _context.Events.Remove(@event);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private async Task<bool> EventExists(int id)
    {
        return await _context.Events.AnyAsync(e => e.EventId == id);
    }
}