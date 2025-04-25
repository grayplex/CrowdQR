using CrowdQR.Shared.Models.DTOs;

namespace CrowdQR.Web.Services;

/// <summary>
/// Service for managing user sessions through the API.
/// </summary>
/// <remarks>
/// Initializes a new instance of the SessionService class.
/// </remarks>
/// <param name="apiService">The API service.</param>
/// <param name="logger">The logger.</param>
public class SessionService(ApiService apiService, ILogger<SessionService> logger)
{
    private readonly ApiService _apiService = apiService;
    private readonly ILogger<SessionService> _logger = logger;
    private const string BaseEndpoint = "api/session";

    /// <summary>
    /// Gets a session for a specific user in a specific event.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <returns>The session, or null if not found.</returns>
    public async Task<SessionDto?> GetSessionByEventAndUserAsync(int eventId, int userId)
    {
        return await _apiService.GetAsync<SessionDto>($"{BaseEndpoint}/event/{eventId}/user/{userId}");
    }

    /// <summary>
    /// Creates a new session or updates an existing one.
    /// </summary>
    /// <param name="sessionDto">The session data.</param>
    /// <returns>The created/updated session and success flag.</returns>
    public async Task<(bool Success, SessionDto? Session)> CreateOrUpdateSessionAsync(SessionCreateDto sessionDto)
    {
        var (success, response) = await _apiService.PostAsync<SessionCreateDto, SessionDto>(BaseEndpoint, sessionDto);
        if (!success)
        {
            _logger.LogError("Failed to create or update session");
            return (false, null);
        }
        if (response == null)
        {
            _logger.LogError("Failed to create or update session: No response received");
            return (false, null);
        }
        _logger.LogInformation("Session created/updated successfully: {SessionId}", response.SessionId);
        return (true, response);
    }

    /// <summary>
    /// Increments the request count for a session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <returns>True if successful, false otherwise.</returns>
    public async Task<bool> IncrementRequestCountAsync(int sessionId)
    {
        string endpoint = $"{BaseEndpoint}/{sessionId}/increment-request-count";
        return await _apiService.PutAsync(endpoint);
    }

    /// <summary>
    /// Refreshes the user's session to keep it active.
    /// </summary>
    /// <param name="sessionId">The session ID to refresh.</param>
    /// <returns>True if successful, false otherwise.</returns>
    public async Task<bool> RefreshSessionAsync(int sessionId)
    {
        // This just calls the increment API to update the last seen time
        // without actually incrementing the request count
        return await _apiService.PutAsync($"{BaseEndpoint}/{sessionId}/refresh");
    }
}