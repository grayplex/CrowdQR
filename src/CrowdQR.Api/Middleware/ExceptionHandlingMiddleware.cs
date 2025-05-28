using System.Text.Json;

namespace CrowdQR.Api.Middleware;

/// <summary>
///  Middleware for handling exceptions globally and returning standardized error responses.
/// </summary>
/// <param name="next">The next middleware in the pipeline.</param>
/// <param name="logger">The logger for recording exception details.</param>
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger = logger;
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    /// <summary>
    /// Invokes the middleware to process the HTTP request.
    /// </summary>
    /// <param name="context">The HTTP context for the current request</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var response = new
        {
            status = context.Response.StatusCode,
            message = "An error occured. Please try again later.",
            detail = exception.Message
        };

        var json = JsonSerializer.Serialize(response, _jsonOptions);

        try
        {
            await context.Response.WriteAsync(json);
        }
        catch
        {
            // If we can't write the response (e.g., client disconnected),
            // we can't do much about it - just ignore the write failure
            // The original exception has already been logged
        }
    }
}
