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
/// API controller for managing users.
/// </summary>
/// <param name="context">The database context.</param>
/// <param name="logger">The logger.</param>
/// <param name="authService">The authentication service.</param>
[ApiController]
[Route("api/[controller]")]
public class UserController(
    CrowdQRContext context, 
    ILogger<UserController> logger,
    IAuthService authService) : ControllerBase
{
    private readonly CrowdQRContext _context = context;
    private readonly ILogger<UserController> _logger = logger;
    private readonly IAuthService _authService = authService;

    // GET: api/user
    /// <summary>
    /// Gets all users.
    /// </summary>
    /// <returns>A list of all users.</returns>
    [HttpGet]
    [Authorize(Roles = "DJ")] // Only DJs should see all users
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
    [Authorize]
    public async Task<ActionResult<object>> GetUser(int id)
    {
        // Regular users should only be able to get their own user info
        if (!User.IsInRole("DJ") && id != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0"))
        {
            return Forbid();
        }

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

        return Ok(formattedUser);
    }

    // GET: api/user/role/{role}
    /// <summary>
    /// Gets all users with a specific role.
    /// </summary>
    /// <param name="role">The role to filter by.</param>
    /// <returns>A list of users with the specified role.</returns>
    [HttpGet("role/{role}")]
    [Authorize]
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

        return Ok(formattedUser);
    }

    // POST: api/user
    /// <summary>
    /// Creates a new user.
    /// </summary>
    /// <param name="userDto">The user data.</param>
    /// <returns>The created user and a 201 Created response, or an error.</returns>
    [HttpPost]
    [Authorize(Roles = "DJ")]
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
    [Authorize(Roles = "DJ")]
    public async Task<IActionResult> UpdateUser(int id, UserUpdateDto userDto)
    {
        // Regular users should only be able to update their own info
        if (!User.IsInRole("DJ") && id != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0"))
        {
            return Forbid();
        }

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
    [Authorize(Roles = "DJ")]
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

    /// <summary>
    /// Resends the verification email for the current user.
    /// </summary>
    /// <returns>Success or failure status.</returns>
    [HttpPost("resend-verification")]
    [Authorize]
    public async Task<IActionResult> ResendVerification()
    {
        // Get current user email
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrEmpty(email))
        {
            return BadRequest(new { success = false, message = "No email associated with this account" });
        }

        var result = await _authService.ResendVerificationEmail(email);

        if (!result)
        {
            return BadRequest(new { success = false, message = "Failed to resend verification email" });
        }

        return Ok(new { success = true, message = "Verification email has been sent" });
    }

    /// <summary>
    /// Gets the verification status of the current user.
    /// </summary>
    /// <returns>The verification status.</returns>
    [HttpGet("verification-status")]
    [Authorize]
    public async Task<IActionResult> GetVerificationStatus()
    {
        // Get current user ID
        if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
        {
            return Unauthorized();
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        return Ok(new
        {
            isEmailVerified = user.IsEmailVerified,
            email = user.Email,
            requiresVerification = user.Role == UserRole.DJ && !user.IsEmailVerified
        });
    }

    private async Task<bool> UserExists(int id)
    {
        return await _context.Users.AnyAsync(u => u.UserId == id);
    }
}