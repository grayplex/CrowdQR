using System.Security.Claims;
using CrowdQR.Api.Services;
using CrowdQR.Shared.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
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
        if (string.IsNullOrEmpty(request.UsernameOrEmail))
        {
            return BadRequest(new AuthResultDto
            {
                Success = false,
                ErrorMessage = "Username or email is required"
            });
        }

        var result = await _authService.AuthenticateUser(request.UsernameOrEmail, request.Password);

        if (!result.Success)
        {
            if (result.EmailVerificationRequired)
            {
                return BadRequest(result); // 400 Bad Request for unverified email
            }
            return Unauthorized(result); // 401 Unauthorized for invalid credentials
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
            Email = user.Email,
            IsEmailVerified = user.IsEmailVerified,
            Role = user.Role,
            CreatedAt = user.CreatedAt
        });
    }

    /// <summary>
    /// Registers a new DJ user.
    /// </summary>
    /// <param name="request">The registration request.</param>
    /// <returns>Registration result.</returns>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResultDto>> Register([FromBody] DjRegisterDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _authService.RegisterDj(request);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        // Return 201 Created for successful registration
        return CreatedAtAction(nameof(Login), new { username = request.Username }, result);
    }

    /// <summary>
    /// Verifies a user's email address.
    /// </summary>
    /// <param name="request">The email verification request.</param>
    /// <returns>Success or failure response.</returns>
    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var success = await _authService.VerifyEmail(request);

        if (!success)
        {
            return BadRequest(new { success = false, message = "Invalid or expired verification token" });
        }

        return Ok(new { success = true, message = "Email verified successfully" });
    }

    /// <summary>
    /// Resends an email verification token.
    /// </summary>
    /// <param name="email">The email address.</param>
    /// <returns>Success or failure response.</returns>
    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification([FromBody] string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            return BadRequest(new { success = false, message = "Email is required" });
        }

        var success = await _authService.ResendVerificationEmail(email);

        if (!success)
        {
            return BadRequest(new { success = false, message = "Could not resend verification email" });
        }

        return Ok(new { success = true, message = "Verification email sent" });
    }

    /// <summary>
    /// Changes a user's password.
    /// </summary>
    /// <param name="request">The password change request.</param>
    /// <returns>Success or failure response.</returns>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Get user ID from claims
        if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
        {
            return Unauthorized();
        }

        var success = await _authService.ChangePassword(userId, request.CurrentPassword, request.NewPassword);

        if (!success)
        {
            return BadRequest(new { success = false, message = "Current password is incorrect" });
        }

        return Ok(new { success = true, message = "Password changed successfully" });
    }
}