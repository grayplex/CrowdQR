using System;

namespace CrowdQR.Api.Middleware;

/// <summary>
/// Extension methods for the authorization logging middleware.
/// </summary>
public static class AuthorizationLoggingMiddlewareExtensions
{
    /// <summary>
    /// Adds the authorization logging middleware to the application pipeline.
    /// </summary>
    /// <param name="builder">The application builder.</param>
    /// <returns>The application builder with the middleware configured.</returns>
    public static IApplicationBuilder UseAuthorizationLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AuthorizationLoggingMiddleware>();
    }
}