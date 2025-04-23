using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;

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
            return false;
        }

        // Check if user is authenticated (DJ users)
        if (httpContext.User.Identity?.IsAuthenticated == true)
        {
            // Get token from claims
            var token = httpContext.User.FindFirst("ApiToken")?.Value;
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                return true;
            }
        }

        // Check for audience token in session
        var audienceToken = httpContext.Session.GetString("ApiToken");
        if (!string.IsNullOrEmpty(audienceToken))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", audienceToken);
            return true;
        }

        return false;
    }
}