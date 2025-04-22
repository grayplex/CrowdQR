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
                new Claim(ClaimTypes.Role, user.Role.ToString())
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
}
