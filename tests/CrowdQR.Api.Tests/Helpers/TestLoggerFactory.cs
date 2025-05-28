using Microsoft.Extensions.Logging;

namespace CrowdQR.Api.Tests.Helpers;

/// <summary>
/// Factory for creating test loggers.
/// </summary>
public static class TestLoggerFactory
{
    /// <summary>
    /// Creates a null logger for testing purposes.
    /// </summary>
    /// <typeparam name="T">The type for which to create the logger.</typeparam>
    /// <returns>A null logger instance.</returns>
    public static ILogger<T> CreateNullLogger<T>()
    {
        return new NullLogger<T>();
    }
}
