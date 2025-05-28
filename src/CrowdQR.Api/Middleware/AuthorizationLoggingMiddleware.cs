using System.Security.Claims;

namespace CrowdQR.Api.Middleware;

/// <summary>
/// Middleware for logging unauthorized access attempts.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AuthorizationLoggingMiddleware"/> class.
/// </remarks>
/// <param name="next">The next middleware in the pipeline.</param>
/// <param name="logger">The logger for recording authorization attempts.</param>
public class AuthorizationLoggingMiddleware(RequestDelegate next, ILogger<AuthorizationLoggingMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<AuthorizationLoggingMiddleware> _logger = logger;

    /// <summary>
    /// Processes the request through the middleware.
    /// </summary>
    /// <param name="context">The HTTP context for the request.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        // Save the response status code before the next middleware
        var originalStatusCode = context.Response.StatusCode;

        // Call the next middleware
        await _next(context);

        // Check if the response is 401 (Unauthorized) or 403 (Forbidden)
        if (context.Response.StatusCode == StatusCodes.Status401Unauthorized ||
            context.Response.StatusCode == StatusCodes.Status403Forbidden)
        {
            var user = context.User?.Identity?.Name ?? "Anonymous";
            var userId = context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Unknown";
            var path = context.Request.Path;
            var method = context.Request.Method;
            var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            var message = $"Access denied ({context.Response.StatusCode}) to {method} {path} " +
                          $"for user {user} (ID: {userId}) from IP {clientIp}";

            _logger.LogWarning(message);
        }
    }
}
