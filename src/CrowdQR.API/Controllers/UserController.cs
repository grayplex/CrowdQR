using CrowdQR.Api.Data;
using CrowdQR.Api.Models;
using CrowdQR.Shared.Models.DTOs;
using CrowdQR.Shared.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrowdQR.Api.Controllers;

/// <summary>
/// API controller for managing users.
/// </summary>
/// <param name="context">The database context.</param>
/// <param name="logger">The logger.</param>
[ApiController]
[Route("api/[controller]")]
public class UserController(CrowdQRContext context, ILogger<UserController> logger) : ControllerBase
{
    private readonly CrowdQRContext _context = context;
    private readonly ILogger<UserController> _logger = logger;

    // GET: api/user
    /// <summary>
    /// Gets all users.
    /// </summary>
    /// <returns>A list of all users.</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetUsers()
    {
        var users = await _context.Users.ToListAsync();

        // Format the response to avoid circular references
        var formattedUsers = users.Select(u => new
        {
            u.UserId,
            u.Username,
            u.Role,
            u.CreatedAt
        }).ToList();

        return Ok(formattedUsers);
    }

    // GET: api/user/5
    /// <summary>
    /// Gets a specific user by ID.
    /// </summary>
    /// <param name="id">The ID of the user to retrieve.</param>
    /// <returns>The requested user or a 404 Not Found response.</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetUser(int id)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            return NotFound();
        }

        // Format the response to avoid circular references
        var formattedUser = new
        {
            user.UserId,
            user.Username,
            user.Role,
            user.CreatedAt
        };

        return formattedUser;
    }

    // GET: api/user/role/{role}
    /// <summary>
    /// Gets all users with a specific role.
    /// </summary>
    /// <param name="role">The role to filter by.</param>
    /// <returns>A list of users with the specified role.</returns>
    [HttpGet("role/{role}")]
    public async Task<ActionResult<IEnumerable<object>>> GetUsersByRole(UserRole role)
    {
        var users = await _context.Users
            .Where(u => u.Role == role)
            .ToListAsync();

        // Format the response to avoid circular references
        var formattedUsers = users.Select(u => new
        {
            u.UserId,
            u.Username,
            u.Role,
            u.CreatedAt
        }).ToList();

        return Ok(formattedUsers);
    }

    // GET: api/user/username/{username}
    /// <summary>
    /// Gets a user by username.
    /// </summary>
    /// <param name="username">The username to search for.</param>
    /// <returns>The matching user or a 404 Not Found response.</returns>
    [HttpGet("username/{username}")]
    public async Task<ActionResult<object>> GetUserByUsername(string username)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user == null)
        {
            return NotFound();
        }

        // Format the response to avoid circular references
        var formattedUser = new
        {
            user.UserId,
            user.Username,
            user.Role,
            user.CreatedAt
        };

        return formattedUser;
    }

    // POST: api/user
    /// <summary>
    /// Creates a new user.
    /// </summary>
    /// <param name="userDto">The user data.</param>
    /// <returns>The created user and a 201 Created response, or an error.</returns>
    [HttpPost]
    public async Task<ActionResult<User>> CreateUser(UserCreateDto userDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Check if username is unique
        var usernameExists = await _context.Users.AnyAsync(u => u.Username == userDto.Username);
        if (usernameExists)
        {
            return Conflict("Username already exists");
        }

        var user = new User
        {
            Username = userDto.Username,
            Role = userDto.Role
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetUser), new { id = user.UserId }, user);
    }

    // PUT: api/user/5
    /// <summary>
    /// Updates a user.
    /// </summary>
    /// <param name="id">The ID of the user to update.</param>
    /// <param name="userDto">The updated user data.</param>
    /// <returns>A 204 No Content response, or an error.</returns>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, UserUpdateDto userDto)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        // Only update provided fields
        if (!string.IsNullOrEmpty(userDto.Username))
        {
            // Check if new username is unique
            var usernameExists = await _context.Users.AnyAsync(u => u.Username == userDto.Username && u.UserId != id);
            if (usernameExists)
            {
                return Conflict("Username already exists");
            }
            user.Username = userDto.Username;
        }

        if (userDto.Role.HasValue)
        {
            user.Role = userDto.Role.Value;
        }

        _context.Entry(user).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await UserExists(id))
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

    // DELETE: api/user/5
    /// <summary>
    /// Deletes a user.
    /// </summary>
    /// <param name="id">The ID of the user to delete.</param>
    /// <returns>A 204 No Content response, or an error.</returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private async Task<bool> UserExists(int id)
    {
        return await _context.Users.AnyAsync(u => u.UserId == id);
    }
}