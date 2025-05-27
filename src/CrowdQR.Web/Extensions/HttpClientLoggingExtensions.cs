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
