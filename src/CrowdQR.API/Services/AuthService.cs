using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CrowdQR.Api.Data;
using CrowdQR.Api.Models;
using CrowdQR.Shared.Models.DTOs;
using CrowdQR.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CrowdQR.Api.Services;

/// <summary>
/// Service for authentication and managing user sessions.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AuthService"/> class.
/// </remarks>
/// <param name="context">The database context.</param>
/// <param name="configuration">The configuration.</param>
/// <param name="logger">The logger.</param>
public class AuthService(CrowdQRContext context, IConfiguration configuration, ILogger<AuthService> logger)
{
    private readonly CrowdQRContext _context = context;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<AuthService> _logger = logger;

    /// <summary>
    /// Authenticates a user by username.
    /// </summary>
    /// <param name="username">The username to authenticate.</param>
    /// <returns>Authentication result with token if successful, or error.</returns>
    public async Task<AuthResultDto> AuthenticateUser(string username)
    {
        try
        {
            // Find user by username
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username);

            // If user doesn't exist, create a temporary one with audience role
            if (user == null)
            {
                user = new User
                {
                    Username = username,
                    Role = UserRole.Audience,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created new user {Username} with Audience role", username);
            }

            // Generate JWT token
            var token = GenerateJwtToken(user);

            return new AuthResultDto
            {
                Success = true,
                Token = token,
                User = new UserDto
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    Role = user.Role,
                    CreatedAt = user.CreatedAt
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error authenticating user {Username}", username);
            return new AuthResultDto
            {
                Success = false,
                ErrorMessage = "Authentication failed"
            };
        }
    }

    /// <summary>
    /// Validates JWT token and returns the associated user.
    /// </summary>
    /// <param name="token">The JWT token to validate.</param>
    /// <returns>The user if token is valid, null otherwise.</returns>
    public async Task<User?> ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Secret"] ?? "temporaryCrowdQRSecretKey12345!@#$%");

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _configuration["Jwt:Issuer"] ?? "CrowdQR.Api",
                ValidateAudience = true,
                ValidAudience = _configuration["Jwt:Audience"] ?? "CrowdQR.Web",
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
            {
                return await _context.Users.FindAsync(userId);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
            return null;
        }
    }

    private string GenerateJwtToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Secret"] ?? "temporaryCrowdQRSecretKey12345!@#$%");

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("account_created", user.CreatedAt.ToString("O"))
            ]),
            Expires = DateTime.UtcNow.AddDays(7), // Token valid for 7 days
            Issuer = _configuration["Jwt:Issuer"] ?? "CrowdQR.Api",
            Audience = _configuration["Jwt:Audience"] ?? "CrowdQR.Web",
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// Checks if a user can access a specific event.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <returns>True if the user can access the event, false otherwise.</returns>
    public async Task<bool> CanAccessEventAsync(int eventId, int userId)
    {
        var @event = await _context.Events.FindAsync(eventId);
        if (@event == null)
        {
            return false;
        }

        // DJ of the event can access it
        if (@event.DjUserId == userId)
        {
            return true;
        }

        // Check if user has a session for this event
        var hasSession = await _context.Sessions
            .AnyAsync(s => s.EventId == eventId && s.UserId == userId);

        return hasSession;
    }

    /// <summary>
    /// Checks if a user can modify a specific request.
    /// </summary>
    /// <param name="requestId">The request ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <returns>True if the user can modify the request, false otherwise.</returns>
    public async Task<bool> CanModifyRequestAsync(int requestId, int userId)
    {
        var request = await _context.Requests
            .Include(r => r.Event)
            .FirstOrDefaultAsync(r => r.RequestId == requestId);

        if (request == null)
        {
            return false;
        }

        // Request owner can modify it
        if (request.UserId == userId)
        {
            return true;
        }

        // Event DJ can modify it
        if (request.Event.DjUserId == userId)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if a user is a DJ.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>True if the user is a DJ, false otherwise.</returns>
    public async Task<bool> IsDjAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        return user?.Role == UserRole.DJ;
    }
}
