using CrowdQR.Api.Services;
using CrowdQR.Shared.Models.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace CrowdQR.Api.Controllers;

/// <summary>
/// API controller for user authentication and authorization.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AuthController"/> class.
/// </remarks>
/// <param name="authService">The authentication service.</param>
/// <param name="logger">The logger.</param>
[ApiController]
[Route("api/[controller]")]
public class AuthController(AuthService authService, ILogger<AuthController> logger) : ControllerBase
{
    private readonly AuthService _authService = authService;
    private readonly ILogger<AuthController> _logger = logger;

    /// <summary>
    /// Authenticates a user and returns a JWT token.
    /// </summary>
    /// <param name="request">The login request.</param>
    /// <returns>Authentication result with JWT token if successful.</returns>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResultDto>> Login([FromBody] LoginDto request)
    {
        if (string.IsNullOrEmpty(request.Username))
        {
            return BadRequest(new AuthResultDto
            {
                Success = false,
                ErrorMessage = "Username is required"
            });
        }

        var result = await _authService.AuthenticateUser(request.Username);

        if (!result.Success)
        {
            return Unauthorized(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Validates a JWT token and returns user information.
    /// </summary>
    /// <returns>User information if token is valid.</returns>
    [HttpGet("validate")]
    public async Task<ActionResult<UserDto>> ValidateToken()
    {
        // Extract token from Authorization header
        var authHeader = Request.Headers.Authorization.FirstOrDefault();

        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return Unauthorized(new { message = "Invalid token" });
        }

#pragma warning disable IDE0057 // Use range operator
        var token = authHeader.Substring("Bearer ".Length).Trim();
#pragma warning restore IDE0057 // Use range operator
        var user = await _authService.ValidateToken(token);

        if (user == null)
        {
            return Unauthorized(new { message = "Invalid token" });
        }

        return Ok(new UserDto
        {
            UserId = user.UserId,
            Username = user.Username,
            Role = user.Role,
            CreatedAt = user.CreatedAt
        });
    }
}