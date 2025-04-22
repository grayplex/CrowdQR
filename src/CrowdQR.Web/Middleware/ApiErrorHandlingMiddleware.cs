using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CrowdQR.Web.Middleware;

/// <summary>
/// Middleware that handles API errors and redirects to an error page.
/// </summary>
/// <remarks>
/// Initializes a new instance of the middleware.
/// </remarks>
/// <param name="next">The next middleware in the pipeline.</param>
/// <param name="logger">The logger.</param>
public class ApiErrorHandlingMiddleware(RequestDelegate next, ILogger<ApiErrorHandlingMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<ApiErrorHandlingMiddleware> _logger = logger;

    /// <summary>
    /// Processes an HTTP request.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task that completes when the middleware has finished processing.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "API request failed: {Path}", context.Request.Path);

            // Don't redirect API calls or AJAX requests
            if (IsApiRequest(context) || IsAjaxRequest(context))
            {
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                await context.Response.WriteAsJsonAsync(new { error = "API service unavailable" });
                return;
            }

            // Get error message from exception
            string message = CrowdQR.Web.Utilities.ApiErrorHelper.GetUserFriendlyErrorMessage(ex);

            // Redirect to error page
            string returnUrl = context.Request.Path + context.Request.QueryString;
            context.Response.Redirect($"/ApiError?ErrorMessage={Uri.EscapeDataString(message)}&ReturnUrl={Uri.EscapeDataString(returnUrl)}");
        }
    }

    private static bool IsApiRequest(HttpContext context)
    {
        return context.Request.Path.StartsWithSegments("/api");
    }

    private static bool IsAjaxRequest(HttpContext context)
    {
        return context.Request.Headers.XRequestedWith == "XMLHttpRequest";
    }
}

/// <summary>
/// Extension methods for the API error handling middleware.
/// </summary>
public static class ApiErrorHandlingMiddlewareExtensions
{
    /// <summary>
    /// Adds the API error handling middleware to the application pipeline.
    /// </summary>
    /// <param name="builder">The application builder.</param>
    /// <returns>The application builder.</returns>
    public static IApplicationBuilder UseApiErrorHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ApiErrorHandlingMiddleware>();
    }
}