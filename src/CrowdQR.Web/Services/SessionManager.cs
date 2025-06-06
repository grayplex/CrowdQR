﻿using System.Text.Json;
using CrowdQR.Shared.Models.DTOs;
using CrowdQR.Shared.Models.Enums;

namespace CrowdQR.Web.Services;

/// <summary>
/// Manages user sessions and provides authentication-related functionality.
/// </summary>
/// <remarks>
/// Initializes a new instance of the SessionManager class.
/// </remarks>
/// <param name="httpContextAccessor">The HTTP context accessor.</param>
/// <param name="userService">The user service.</param>
/// <param name="sessionService">The session service.</param>
/// <param name="logger">The logger.</param>
public class SessionManager(
    IHttpContextAccessor httpContextAccessor,
    UserService userService,
    SessionService sessionService,
    ILogger<SessionManager> logger)
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly UserService _userService = userService;
    private readonly SessionService _sessionService = sessionService;
    private readonly ILogger<SessionManager> _logger = logger;

    // Session key constants
    private const string UserIdKey = "UserId";
    private const string UsernameKey = "Username";
    private const string UserRoleKey = "UserRole";
    private const string EventIdKey = "EventId";
    private const string EventSlugKey = "EventSlug";
    private const string SessionIdKey = "ApiSessionId";

    /// <summary>
    /// Gets the current user ID from the session.
    /// </summary>
    /// <returns>The user ID, or null if not logged in.</returns>
    public int? GetCurrentUserId()
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        if (session == null)
        {
            return null;
        }

        var userIdString = session.GetString(UserIdKey);
        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
        {
            return null;
        }

        return userId;
    }

    /// <summary>
    /// Gets the current username from the session.
    /// </summary>
    /// <returns>The username, or null if not logged in.</returns>
    public string? GetCurrentUsername()
    {
        return _httpContextAccessor.HttpContext?.Session.GetString(UsernameKey);
    }

    /// <summary>
    /// Gets the current user role from the session.
    /// </summary>
    /// <returns>The user role, or null if not logged in.</returns>
    public string? GetCurrentUserRole()
    {
        return _httpContextAccessor.HttpContext?.Session.GetString(UserRoleKey);
    }

    /// <summary>
    /// Gets the current event ID from the session.
    /// </summary>
    /// <returns>The event ID, or null if not in an event.</returns>
    public int? GetCurrentEventId()
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        if (session == null)
        {
            return null;
        }

        var eventIdString = session.GetString(EventIdKey);
        if (string.IsNullOrEmpty(eventIdString) || !int.TryParse(eventIdString, out int eventId))
        {
            return null;
        }

        return eventId;
    }

    /// <summary>
    /// Gets the current event slug from the session.
    /// </summary>
    /// <returns>The event slug, or null if not in an event.</returns>
    public string? GetCurrentEventSlug()
    {
        return _httpContextAccessor.HttpContext?.Session.GetString(EventSlugKey);
    }

    /// <summary>
    /// Gets the current API session ID from the session.
    /// </summary>
    /// <returns>The API session ID, or null if not in an event.</returns>
    public int? GetApiSessionId()
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        if (session == null)
        {
            return null;
        }

        var sessionIdString = session.GetString(SessionIdKey);
        if (string.IsNullOrEmpty(sessionIdString) || !int.TryParse(sessionIdString, out int sessionId))
        {
            return null;
        }

        return sessionId;
    }

    /// <summary>
    /// Checks if the current user is logged in.
    /// </summary>
    /// <returns>True if logged in, false otherwise.</returns>
    public bool IsLoggedIn()
    {
        return GetCurrentUserId() != null;
    }

    /// <summary>
    /// Checks if the current user is a DJ.
    /// </summary>
    /// <returns>True if the user is a DJ, false otherwise.</returns>
    public bool IsDj()
    {
        var roleStr = GetCurrentUserRole();
        if (string.IsNullOrEmpty(roleStr))
        {
            return false;
        }

        // Log the role for debugging
        _logger.LogInformation("Current user role: {Role}", roleStr);

        // Try both direct comparison and enum parsing
        if (roleStr == "DJ" || roleStr == "1")
        {
            return true;
        }

        if (Enum.TryParse<UserRole>(roleStr, out var role))
        {
            return role == UserRole.DJ;
        }

        return false;
    }

    /// <summary>
    /// Checks if the current user is in an event.
    /// </summary>
    /// <returns>True if in an event, false otherwise.</returns>
    public bool IsInEvent()
    {
        return GetCurrentEventId() != null;
    }

    /// <summary>
    /// Logs in a user with the given credentials.
    /// </summary>
    /// <param name="username">The username.</param>
    /// <returns>True if login was successful, false otherwise.</returns>
    public async Task<bool> LoginAsync(string username)
    {
        try
        {
            // For simplicity, we're just looking up the user by username
            // In a real app, you would validate a password here
            var user = await _userService.GetUserByUsernameAsync(username);
            if (user == null)
            {
                return false;
            }

            // Store user info in session
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session == null)
            {
                return false;
            }

            session.SetString(UserIdKey, user.UserId.ToString());
            session.SetString(UsernameKey, user.Username);
            session.SetString(UserRoleKey, user.Role.ToString());

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging in user {Username}", username);
            return false;
        }
    }

    /// <summary>
    /// Creates a new user and logs them in.
    /// </summary>
    /// <param name="username">The username.</param>
    /// <param name="role">The user role (0 = Audience, 1 = DJ).</param>
    /// <returns>True if registration was successful, false otherwise.</returns>
    public async Task<bool> RegisterAndLoginAsync(string username, UserRole role = UserRole.Audience)
    {
        try
        {
            // Check if username is taken
            var existingUser = await _userService.GetUserByUsernameAsync(username);
            if (existingUser != null)
            {
                // User exists, just log them in
                var session = _httpContextAccessor.HttpContext?.Session;
                if (session == null)
                {
                    return false;
                }

                session.SetString(UserIdKey, existingUser.UserId.ToString());
                session.SetString(UsernameKey, existingUser.Username);
                session.SetString(UserRoleKey, existingUser.Role.ToString());
                
                return true;
            }

            // For guest users in demo mode, we'll create a local session without API calls
            if (username.StartsWith("guest_"))
            {
                var session = _httpContextAccessor.HttpContext?.Session;
                if (session == null)
                {
                    return false;
                }

                // Generate a random user ID for the guest
                var random = new Random();
                var guestUserId = random.Next(10000, 99999);
                
                session.SetString(UserIdKey, guestUserId.ToString());
                session.SetString(UsernameKey, username);
                session.SetString(UserRoleKey, UserRole.Audience.ToString());
                
                return true;
            }

            // Create new user via API
            var userDto = new UserCreateDto
            {
                Username = username,
                Role = role
            };

            var (success, user) = await _userService.CreateUserAsync(userDto);
            if (!success || user == null)
            {
                return false;
            }

            // Store user info in session
            var apiSession = _httpContextAccessor.HttpContext?.Session;
            if (apiSession == null)
            {
                return false;
            }

            apiSession.SetString(UserIdKey, user.UserId.ToString());
            apiSession.SetString(UsernameKey, user.Username);
            apiSession.SetString(UserRoleKey, user.Role.ToString());

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering user {Username}", username);
            return false;
        }
    }

    /// <summary>
    /// Joins an event and creates an API session.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <param name="slug">The event slug.</param>
    /// <returns>True if joining was successful, false otherwise.</returns>
    public async Task<bool> JoinEventAsync(int eventId, string slug)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return false;
            }

            var username = GetCurrentUsername();
            if (string.IsNullOrEmpty(username))
            {
                return false;
            }

            // For guest users, we'll skip the API session creation
            if (username.StartsWith("guest_"))
            {
                // Store event info in session
                var session = _httpContextAccessor.HttpContext?.Session;
                if (session == null)
                {
                    return false;
                }

                session.SetString(EventIdKey, eventId.ToString());
                session.SetString(EventSlugKey, slug);
                session.SetString(SessionIdKey, "0"); // Use 0 for guest sessions

                return true;
            }

            // Create/update API session
            var sessionDto = new SessionCreateDto
            {
                UserId = userId.Value,
                EventId = eventId,
                ClientIP = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
            };

            try
            {
                var (success, apiSession) = await _sessionService.CreateOrUpdateSessionAsync(sessionDto);
                if (!success || apiSession == null)
                {
                    _logger.LogWarning("Failed to create API session for user {UserId} in event {EventId}", userId, eventId);
                    // Even if API session creation fails, we'll still create a local session
                    // to allow basic functionality
                    var httpContext = _httpContextAccessor.HttpContext;
                    if (httpContext != null)
                    {
                        httpContext.Session.SetString(EventIdKey, eventId.ToString());
                        httpContext.Session.SetString(EventSlugKey, slug);
                        httpContext.Session.SetString(SessionIdKey, "0"); // Fallback session ID
                    }
                    return true; // Return true so user can still use the application
                }

                // Store event info in session
                var apiUserSession = _httpContextAccessor.HttpContext?.Session;
                if (apiUserSession == null)
                {
                    return false;
                }

                apiUserSession.SetString(EventIdKey, eventId.ToString());
                apiUserSession.SetString(EventSlugKey, slug);
                apiUserSession.SetString(SessionIdKey, apiSession.SessionId.ToString());

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating API session for user {UserId} in event {EventId}", userId, eventId);
                // Fall back to local session on error
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext != null)
                {
                    httpContext.Session.SetString(EventIdKey, eventId.ToString());
                    httpContext.Session.SetString(EventSlugKey, slug);
                    httpContext.Session.SetString(SessionIdKey, "0"); // Fallback session ID
                }
                return true; // Return true so user can still use the application
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining event {EventId}", eventId);
            return false;
        }
    }

    /// <summary>
    /// Logs out the current user.
    /// </summary>
    public void Logout()
    {
        _httpContextAccessor.HttpContext?.Session.Clear();
    }

    /// <summary>
    /// Leaves the current event.
    /// </summary>
    public void LeaveEvent()
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        if (session == null)
        {
            return;
        }

        session.Remove(EventIdKey);
        session.Remove(EventSlugKey);
        session.Remove(SessionIdKey);
    }

    /// <summary>
    /// Converts a temporary guest user to a permanent user.
    /// </summary>
    /// <param name="username">The new username.</param>
    /// <returns>True if successful, false otherwise.</returns>
    public async Task<bool> ConvertGuestToUserAsync(string username)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return false;
            }

            // Get current user
            var currentUser = await _userService.GetUserByIdAsync(currentUserId.Value);
            if (currentUser == null)
            {
                return false;
            }

            // Check if username is already taken
            var existingUser = await _userService.GetUserByUsernameAsync(username);
            if (existingUser != null && existingUser.UserId != currentUserId)
            {
                return false;
            }

            // Update the username
            var updateDto = new UserUpdateDto
            {
                Username = username
            };

            bool updated = await _userService.UpdateUserAsync(currentUserId.Value, updateDto);
            if (!updated)
            {
                return false;
            }

            // Update session
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session == null)
            {
                return false;
            }

            session.SetString(UsernameKey, username);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting guest user {UserId} to permanent user", GetCurrentUserId());
            return false;
        }
    }

    /// <summary>
    /// Checks if the current user is a guest user.
    /// </summary>
    /// <returns>True if the user is a guest, false otherwise.</returns>
    public bool IsGuestUser()
    {
        var username = GetCurrentUsername();
        return username != null && username.StartsWith("guest_");
    }

    /// <summary>
    /// Refreshes the current API session to keep it active.
    /// </summary>
    /// <returns>True if successful, false otherwise.</returns>
    public async Task<bool> RefreshCurrentSessionAsync()
    {
        var sessionId = GetApiSessionId();
        if (!sessionId.HasValue)
        {
            return false;
        }

        try
        {
            return await _sessionService.RefreshSessionAsync(sessionId.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh session {SessionId}", sessionId.Value);
            return false;
        }
    }

    /// <summary>
    /// Sets up an automatic session refresh for the current session.
    /// </summary>
    public void SetupSessionRefresh()
    {
        var sessionRefreshKey = "SessionRefreshActive";
        var session = _httpContextAccessor.HttpContext?.Session;

        if (session == null || session.GetString(sessionRefreshKey) == "true")
        {
            return;
        }

        session.SetString(sessionRefreshKey, "true");

        // This would ideally be implemented with JavaScript to periodically call
        // an API endpoint that refreshes the session
    }
}