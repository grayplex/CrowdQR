using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace CrowdQR.Web.Middleware;

/// <summary>
/// Middleware that ensures the API URL is correctly set for the current environment.
/// </summary>
/// <remarks>
/// Initializes a new instance of the middleware.
/// </remarks>
/// <param name="next">The next middleware in the pipeline.</param>
/// <param name="configuration">The application configuration.</param>
public class ApiUrlMiddleware(RequestDelegate next, IConfiguration configuration)
{
    private readonly RequestDelegate _next = next;
    private readonly IConfiguration _configuration = configuration;

    /// <summary>
    /// Processes an HTTP request.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task that completes when the middleware has finished processing.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        // Get the base API URL from configuration
        var apiBaseUrl = _configuration["ApiSettings:BaseUrl"];

        // If running in a Docker environment, we might need to adjust the URL
        if (context.Request.Headers.TryGetValue("X-Forwarded-Host", out var host))
        {
            var scheme = context.Request.Headers.TryGetValue("X-Forwarded-Proto", out var proto)
                ? proto.ToString()
                : "http";

            // Update the API URL to use the same host but with the API port
            apiBaseUrl = $"{scheme}://{host}:{_configuration["ApiSettings:Port"] ?? "5000"}";

            // Store the updated URL in context items so it can be accessed in services
            context.Items["ApiBaseUrl"] = apiBaseUrl;
        }

        await _next(context);
    }
}

/// <summary>
/// Extension methods for the API URL middleware.
/// </summary>
public static class ApiUrlMiddlewareExtensions
{
    /// <summary>
    /// Adds the API URL middleware to the application pipeline.
    /// </summary>
    /// <param name="builder">The application builder.</param>
    /// <returns>The application builder.</returns>
    public static IApplicationBuilder UseApiUrlMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ApiUrlMiddleware>();
    }
}