using System;

namespace CrowdQR.Api.Middleware;

/// <summary>
/// Extension methods for the DJ role validation middleware.
/// </summary>
public static class DjRoleValidationMiddlewareExtensions
{
    /// <summary>
    /// Adds the DJ role validation middleware to the application pipeline.
    /// </summary>
    /// <param name="builder">The application builder.</param>
    /// <returns>The application builder with the middleware added.</returns>
    public static IApplicationBuilder UseDjRoleValidation(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<DjRoleValidationMiddleware>();
    }
}