using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

namespace CrowdQR.Web.Extensions;

/// <summary>
/// Extension methods for configuring HTTP client request logging.
/// </summary>
public static class HttpClientLoggingExtensions
{
    /// <summary>
    /// Adds HTTP client request logging to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddHttpClientLogging(this IServiceCollection services)
    {
        // Add HTTP client logging handler
        services.AddTransient<HttpClientLoggingHandler>();

        // Configure named HTTP client with the logging handler
        services.AddHttpClient("CrowdQRApi")
            .AddHttpMessageHandler<HttpClientLoggingHandler>();

        return services;
    }
}

/// <summary>
/// HTTP message handler that logs request and response details for debugging.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="HttpClientLoggingHandler"/> class.
/// </remarks>
/// <param name="logger">The logger.</param>
public class HttpClientLoggingHandler(ILogger<HttpClientLoggingHandler> logger) : DelegatingHandler
{
    private readonly ILogger<HttpClientLoggingHandler> _logger = logger;

    /// <summary>
    /// Sends an HTTP request to the inner handler to send to the server and returns the response.
    /// </summary>
    /// <param name="request">The HTTP request message to send.</param>
    /// <param name="cancellationToken">A cancellation token to cancel operation.</param>
    /// <returns>The HTTP response message.</returns>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Log request details
        var requestId = Guid.NewGuid().ToString();
        var builder = new StringBuilder();

        builder.AppendLine($"[{requestId}] HTTP Request: {request.Method} {request.RequestUri}");

        // Log request headers
        builder.AppendLine("Request Headers:");
        foreach (var header in request.Headers)
        {
            builder.AppendLine($"  {header.Key}: {string.Join(", ", header.Value)}");
        }

        // Log request content if present
        if (request.Content != null)
        {
            builder.AppendLine("Request Content:");
            var content = await request.Content.ReadAsStringAsync(cancellationToken);
            builder.AppendLine($"  {content}");
        }

        _logger.LogDebug(builder.ToString());

        try
        {
            // Send the request to the server
            var response = await base.SendAsync(request, cancellationToken);

            // Log response details
            builder.Clear();
            builder.AppendLine($"[{requestId}] HTTP Response: {(int)response.StatusCode} {response.StatusCode}");

            // Log response headers
            builder.AppendLine("Response Headers:");
            foreach (var header in response.Headers)
            {
                builder.AppendLine($"  {header.Key}: {string.Join(", ", header.Value)}");
            }

            // Log response content if present
            if (response.Content != null)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!string.IsNullOrEmpty(content))
                {
                    // Limit content length for logging
                    var truncatedContent = content.Length > 1000
                        ? string.Concat(content.AsSpan(0, 1000), "...")
                        : content;

                    builder.AppendLine("Response Content:");
                    builder.AppendLine($"  {truncatedContent}");
                }
            }

            _logger.LogDebug(builder.ToString());

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[{requestId}] HTTP Request failed: {request.Method} {request.RequestUri}");
            throw;
        }
    }
}