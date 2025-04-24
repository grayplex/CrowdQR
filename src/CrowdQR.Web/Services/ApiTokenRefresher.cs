using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace CrowdQR.Web.Services;

/// <summary>
/// Service to handle API token refreshing and authorization
/// </summary>
/// <remarks>
/// Initializes a new instance of the ApiTokenRefresher class
/// </remarks>
/// <param name="httpContextAccessor">HTTP context accessor</param>
/// <param name="logger">Logger</param>
public class ApiTokenRefresher(
    IHttpContextAccessor httpContextAccessor,
    ILogger<ApiTokenRefresher> logger)
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly ILogger<ApiTokenRefresher> _logger = logger;

    /// <summary>
    /// Attaches token to an HTTP client for API calls
    /// </summary>
    /// <param name="client">The HTTP client</param>
    /// <returns>True if token was attached, false otherwise</returns>
    public bool AttachTokenToClient(HttpClient client)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            _logger.LogWarning("HttpContext is null when trying to attach token");
            return false;
        }

        // Check if user is authenticated (DJ users)
        if (httpContext.User.Identity?.IsAuthenticated == true)
        {
            _logger.LogInformation("User is authenticated: {Username}, Roles: {Roles}", 
                httpContext.User.Identity.Name,
                string.Join(", ", httpContext.User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value)));

            // Get token from claims
            var token = httpContext.User.FindFirst("ApiToken")?.Value;
            if (!string.IsNullOrEmpty(token))
            {
                _logger.LogInformation("Found API token in claims, attaching to request");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                return true;
            }
            else
            {
                _logger.LogWarning("No API token found in claims for authenticated user");
            }
        }
        else
        {
            _logger.LogWarning("User is not authenticated when trying to attach token");
        }

        // Check for audience token in session
        var audienceToken = httpContext.Session.GetString("ApiToken");
        if (!string.IsNullOrEmpty(audienceToken))
        {
            _logger.LogInformation("Found API token in session, attaching to request");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", audienceToken);
            return true;
        }
        else
        {
            _logger.LogWarning("No API token found in session");
        }

        return false;
    }
}