using System.Text.Json;
using CrowdQR.Shared.Models.DTOs;

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
        var role = GetCurrentUserRole();
        return role == "DJ";
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
            session.SetString(UserRoleKey, user.Role);

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
    public async Task<bool> RegisterAndLoginAsync(string username, int role = 0)
    {
        try
        {
            // Check if username is taken
            var existingUser = await _userService.GetUserByUsernameAsync(username);
            if (existingUser != null)
            {
                return false;
            }

            // Create new user
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
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session == null)
            {
                return false;
            }

            session.SetString(UserIdKey, user.UserId.ToString());
            session.SetString(UsernameKey, user.Username);
            session.SetString(UserRoleKey, user.Role);

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

            // Create/update API session
            var sessionDto = new SessionCreateDto
            {
                UserId = userId.Value,
                EventId = eventId,
                ClientIP = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
            };

            var (success, apiSession) = await _sessionService.CreateOrUpdateSessionAsync(sessionDto);
            if (!success || apiSession == null)
            {
                return false;
            }

            // Store event info in session
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session == null)
            {
                return false;
            }

            session.SetString(EventIdKey, eventId.ToString());
            session.SetString(EventSlugKey, slug);
            session.SetString(SessionIdKey, apiSession.SessionId.ToString());

            return true;
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
}