using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CrowdQR.Shared.Models.DTOs;
using CrowdQR.Shared.Models.Enums;
using CrowdQR.Web.Utilities;

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
public class AuthenticationService(HttpClient httpClient, ILogger<AuthenticationService> logger, IHttpContextAccessor httpContextAccessor)
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<AuthenticationService> _logger = logger;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    // Session keys
    private const string TokenKey = "Auth:Token";
    private const string UserIdKey = "Auth:UserId";
    private const string UsernameKey = "Auth:Username";
    private const string UserRoleKey = "Auth:Role";
    private const string AuthExpiryKey = "Auth:Expiry";

    /// <summary>
    /// Attempts to authenticate with the given username.
    /// </summary>
    /// <param name="username">The username to authenticate with.</param>
    /// <returns>True if authentication was successful, otherwise false.</returns>
    public async Task<bool> LoginAsync(string username)
    {
        try
        {
            // Prepare login request
            var loginRequest = new LoginDto
            {
                UsernameOrEmail = username
            };

            // Send login request to API
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", loginRequest, _jsonOptions);

            if (!response.IsSuccessStatusCode)
            {
                var errorResult = await response.Content.ReadFromJsonAsync<AuthResultDto>(_jsonOptions);
                _logger.LogWarning("Authentication failed for username {Username}. Status: {StatusCode}, Error: {Error}",
                    username, response.StatusCode, errorResult?.ErrorMessage);
                return false;
            }

            // Parse response
            var result = await response.Content.ReadFromJsonAsync<AuthResultDto>(_jsonOptions);

            if (result == null || !result.Success || result.User == null || string.IsNullOrEmpty(result.Token))
            {
                _logger.LogWarning("Authentication failed for username {Username}. Invalid response format.", username);
                return false;
            }

            // Save auth information to session
            SaveAuthToSession(result.Token, result.User);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for username {Username}", username);
            return false;
        }
    }

    /// <summary>
    /// Logs the user out by clearing authentication session data.
    /// </summary>
    public void Logout()
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        if (session != null)
        {
            session.Remove(TokenKey);
            session.Remove(UserIdKey);
            session.Remove(UsernameKey);
            session.Remove(UserRoleKey);
            session.Remove(AuthExpiryKey);
        }
    }

    /// <summary>
    /// Checks if the user is logged in.
    /// </summary>
    /// <returns>True if logged in, otherwise false.</returns>
    public bool IsLoggedIn()
    {
        return !string.IsNullOrEmpty(GetToken()) && !IsTokenExpired();
    }

    /// <summary>
    /// Gets the current user's ID.
    /// </summary>
    /// <returns>The user ID, or null if not logged in.</returns>
    public int? GetUserId()
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        if (session == null)
        {
            return null;
        }

        var userIdString = session.GetString(UserIdKey);
        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
        {
            return null;
        }

        return userId;
    }

    /// <summary>
    /// Gets the current user's username.
    /// </summary>
    /// <returns>The username, or null if not logged in.</returns>
    public string? GetUsername()
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        if (session == null)
        {
            return null;
        }

        return session.GetString(UsernameKey);
    }

    /// <summary>
    /// Gets the current user's role.
    /// </summary>
    /// <returns>The user role, or null if not logged in.</returns>
    public UserRole? GetUserRole()
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        if (session == null)
        {
            return null;
        }

        var roleString = session.GetString(UserRoleKey);
        if (string.IsNullOrEmpty(roleString))
        {
            return null;
        }

        if (Enum.TryParse<UserRole>(roleString, out var role))
        {
            return role;
        }

        return null;
    }

    /// <summary>
    /// Checks if the current user is a DJ.
    /// </summary>
    /// <returns>True if the user is a DJ, otherwise false.</returns>
    public bool IsDj()
    {
        var role = GetUserRole();
        return role == UserRole.DJ;
    }

    /// <summary>
    /// Gets the authentication token for API requests.
    /// </summary>
    /// <returns>The token, or null if not logged in.</returns>
    public string? GetToken()
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        if (session == null)
        {
            return null;
        }

        return session.GetString(TokenKey);
    }

    /// <summary>
    /// Adds the authentication token to the HTTP client for API requests.
    /// </summary>
    /// <param name="client">The HTTP client to authorize.</param>
    public void AuthorizeClient(HttpClient client)
    {
        var token = GetToken();
        if (!string.IsNullOrEmpty(token))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    /// <summary>
    /// Validates and refreshes authentication token with the API.
    /// </summary>
    /// <returns>True if the token is valid, otherwise false.</returns>
    public async Task<bool> ValidateTokenAsync()
    {
        try
        {
            var token = GetToken();
            if (string.IsNullOrEmpty(token))
            {
                return false;
            }

            // Set auth header
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Validate token with API
            var response = await _httpClient.GetAsync("api/auth/validate");

            if (!response.IsSuccessStatusCode)
            {
                Logout();
                return false;
            }

            // Token is valid, refresh the expiry
            var session = _httpContextAccessor.HttpContext?.Session;
            session?.SetString(AuthExpiryKey, DateTime.UtcNow.AddHours(1).ToString("o"));

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating auth token");
            return false;
        }
    }

    private void SaveAuthToSession(string token, UserDto user)
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        if (session == null)
        {
            return;
        }

        session.SetString(TokenKey, token);
        session.SetString(UserIdKey, user.UserId.ToString());
        session.SetString(UsernameKey, user.Username);
        session.SetString(UserRoleKey, user.Role.ToString());
        session.SetString(AuthExpiryKey, DateTime.UtcNow.AddHours(1).ToString("o"));
    }

    private bool IsTokenExpired()
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        if (session == null)
        {
            return true;
        }

        var expiryString = session.GetString(AuthExpiryKey);
        if (string.IsNullOrEmpty(expiryString))
        {
            return true;
        }

        if (DateTime.TryParse(expiryString, out var expiry))
        {
            return expiry < DateTime.UtcNow;
        }

        return true;
    }
}