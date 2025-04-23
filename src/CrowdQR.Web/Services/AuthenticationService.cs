using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using CrowdQR.Shared.Models.DTOs;
using CrowdQR.Shared.Models.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace CrowdQR.Web.Services;

/// <summary>
/// Service for managing authentication with the API.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AuthenticationService"/> class.
/// </remarks>
/// <param name="httpClient">The HTTP client.</param>
/// <param name="logger">The logger.</param>
/// <param name="httpContextAccessor">The HTTP context accessor.</param>
/// <param name="configuration">The configuration.</param>
public class AuthenticationService
    (HttpClient httpClient, 
    ILogger<AuthenticationService> logger, 
    IHttpContextAccessor httpContextAccessor,
    IConfiguration configuration)
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<AuthenticationService> _logger = logger;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly IConfiguration _configuration = configuration;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Attempts to authenticate a DJ user with the given credentials.
    /// </summary>
    /// <param name="usernameOrEmail">The username or email.</param>
    /// <param name="password">The password.</param>
    /// <returns>True if authentication was successful, otherwise false.</returns>
    public async Task<bool> LoginDjAsync(string usernameOrEmail, string password)
    {
        try
        {
            // Get the base URL from configuration
            var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5071";
            // Set the base address
            if (_httpClient.BaseAddress == null)
            {
                _httpClient.BaseAddress = new Uri(apiBaseUrl);
            }

            // Prepare login request
            var loginRequest = new LoginDto
            {
                UsernameOrEmail = usernameOrEmail,
                Password = password
            };

            _logger.LogInformation("Sending DJ login request to {BaseAddress}api/auth/login", _httpClient.BaseAddress);

            // Send login request to API
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", loginRequest, _jsonOptions);

            if (!response.IsSuccessStatusCode)
            {
                var errorResult = await response.Content.ReadFromJsonAsync<AuthResultDto>(_jsonOptions);
                _logger.LogWarning("DJ authentication failed for {UsernameOrEmail}. Status: {StatusCode}, Error: {Error}",
                    usernameOrEmail, response.StatusCode, errorResult?.ErrorMessage);
                return false;
            }

            // Parse response
            var result = await response.Content.ReadFromJsonAsync<AuthResultDto>(_jsonOptions);

            if (result == null || !result.Success || result.User == null || string.IsNullOrEmpty(result.Token))
            {
                _logger.LogWarning("DJ authentication failed for {UsernameOrEmail}. Invalid response format.", usernameOrEmail);
                return false;
            }

            // Only allow DJ users to log in through this method
            if (result.User.Role != UserRole.DJ)
            {
                _logger.LogWarning("Non-DJ user attempted to use DJ login: {UsernameOrEmail}", usernameOrEmail);
                return false;
            }

            // Create claims for authentication
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, result.User.UserId.ToString()),
                new(ClaimTypes.Name, result.User.Username),
                new(ClaimTypes.Role, "DJ"),
                new("ApiToken", result.Token)
            };

            if (!string.IsNullOrEmpty(result.User.Email))
            {
                claims.Add(new Claim(ClaimTypes.Email, result.User.Email));
            }

            // Create identity and principal
            var identity = new ClaimsIdentity(claims, "WebAppCookie");
            var principal = new ClaimsPrincipal(identity);

            // Sign in
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                await httpContext.SignInAsync("WebAppCookie", principal, new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(12)
                });
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during DJ login for {UsernameOrEmail}", usernameOrEmail);
            return false;
        }
    }

    /// <summary>
    /// Creates anonymous audience session
    /// </summary>
    /// <returns>True if successful, false otherwise</returns>
    public async Task<bool> CreateAudienceSessionAsync(string? username = null)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                return false;
            }

            // Generate temporary username if not provided
            if (string.IsNullOrEmpty(username))
            {
                username = $"guest_{Guid.NewGuid().ToString()[..8]}";
            }

            // Make API call to create audience user
            var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5071";
            if (_httpClient.BaseAddress == null)
            {
                _httpClient.BaseAddress = new Uri(apiBaseUrl);
            }

            // Create or get audience user
            var loginRequest = new LoginDto
            {
                UsernameOrEmail = username
                // No password for audience users
            };

            var response = await _httpClient.PostAsJsonAsync("api/auth/login", loginRequest, _jsonOptions);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to create audience session. Status: {StatusCode}", response.StatusCode);
                return false;
            }

            var result = await response.Content.ReadFromJsonAsync<AuthResultDto>(_jsonOptions);
            if (result == null || !result.Success || result.User == null)
            {
                return false;
            }

            // Store audience info in session
            httpContext.Session.SetString("AudienceId", result.User.UserId.ToString());
            httpContext.Session.SetString("AudienceUsername", result.User.Username);
            httpContext.Session.SetString("ApiToken", result.Token ?? "");

            // Sign in with the audience cookie
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, result.User.UserId.ToString()),
                new(ClaimTypes.Name, result.User.Username),
                new(ClaimTypes.Role, "Audience")
            };

            var identity = new ClaimsIdentity(claims, "AudienceCookie");
            var principal = new ClaimsPrincipal(identity);

            await httpContext.SignInAsync("AudienceCookie", principal, new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(1)
            });

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating audience session");
            return false;
        }
    }

    /// <summary>
    /// Logs out the current user.
    /// </summary>
    public async Task LogoutAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return;
        }

        // Clear all authentication cookies
        await httpContext.SignOutAsync("WebAppCookie");
        await httpContext.SignOutAsync("AudienceCookie");

        // Clear session
        httpContext.Session.Clear();
    }

    /// <summary>
    /// Checks if the current user is logged in as a DJ.
    /// </summary>
    /// <returns>True if logged in as a DJ, otherwise false.</returns>
    public bool IsDj()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return false;
        }

        return httpContext.User.Identity?.IsAuthenticated == true &&
               httpContext.User.IsInRole("DJ");
    }

    /// <summary>
    /// Checks if the current user is an audience member.
    /// </summary>
    /// <returns>True if user is an audience member, otherwise false.</returns>
    public bool IsAudience()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return false;
        }

        // Either authenticated as audience or has audience session
        return (httpContext.User.Identity?.IsAuthenticated == true &&
                httpContext.User.IsInRole("Audience")) ||
               !string.IsNullOrEmpty(httpContext.Session.GetString("AudienceId"));
    }

    /// <summary>
    /// Gets the current user's ID.
    /// </summary>
    /// <returns>The user ID, or null if not logged in.</returns>
    public int? GetUserId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return null;
        }

        // Check claims first (DJ users)
        if (httpContext.User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }
        }

        // Then check session (audience)
        var audienceId = httpContext.Session.GetString("AudienceId");
        if (!string.IsNullOrEmpty(audienceId) && int.TryParse(audienceId, out var sessionUserId))
        {
            return sessionUserId;
        }

        return null;
    }

    /// <summary>
    /// Gets the current user's username.
    /// </summary>
    /// <returns>The username, or null if not logged in.</returns>
    public string? GetUsername()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return null;
        }

        // Check claims first (DJ users)
        if (httpContext.User.Identity?.IsAuthenticated == true)
        {
            return httpContext.User.Identity.Name;
        }

        // Then check session (audience)
        return httpContext.Session.GetString("AudienceUsername");
    }
}