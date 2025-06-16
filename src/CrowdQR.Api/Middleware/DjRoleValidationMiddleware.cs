using System.Security.Claims;
using CrowdQR.Shared.Models.Enums;

namespace CrowdQR.Api.Middleware;

/// <summary>
/// Middleware to validate that users accessing DJ-only endpoints have the DJ role.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="DjRoleValidationMiddleware"/> class.
/// </remarks>
/// <param name="next">The next middleware in the pipeline.</param>
/// <param name="logger">The logger.</param>
public class DjRoleValidationMiddleware(RequestDelegate next, ILogger<DjRoleValidationMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<DjRoleValidationMiddleware> _logger = logger;

    /// <summary>
    /// Processes the request.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        // Skip verification for non-protected paths
        if (!IsProtectedPath(context.Request.Path))
        {
            await _next(context);
            return;
        }

        // Check if user is authenticated
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Unauthorized",
                message = "Authentication is required to access this resource"
            });
            return;
        }

        // Check if user has DJ role
        var roleClaim = context.User.FindFirst(ClaimTypes.Role);
        if (roleClaim == null || roleClaim.Value != UserRole.DJ.ToString())
        {
            var sanitizedPath = context.Request.Path.Value?.Replace("\n", "").Replace("\r", "");
            _logger.LogWarning("User {UserId} attempted to access DJ-only endpoint {Path} without DJ role",
                context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, sanitizedPath);

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Forbidden",
                message = "Only DJ accounts can access this resource"
            });
            return;
        }

        // TEMPORARILY DISABLED: Email Verification Check
        // Check if email is verified for DJ
        /*
        var emailVerifiedClaim = context.User.FindFirst("email_verified");
        if (emailVerifiedClaim == null || emailVerifiedClaim.Value != "true")
        {
            _logger.LogWarning("DJ user {UserId} attempted to access endpoint {Path} with unverified email",
                context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, context.Request.Path);

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Forbidden",
                message = "Email verification is required to access this resource"
            });
            return;
        }
        */

        await _next(context);
    }

    private static bool IsProtectedPath(PathString path)
    {
        // List of paths that should be protected for DJ access only
        var djOnlyPaths = new[]
        {
            "/api/dashboard",
            "/api/event/create",
            "/api/event/update",
            "/api/event/delete",
            "/api/admin"
        };

        return djOnlyPaths.Any(p => path.StartsWithSegments(p));
    }
}