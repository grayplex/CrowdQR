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

/// <summary>
/// Null logger implementation for testing.
/// </summary>
/// <typeparam name="T">The type for which this logger is created.</typeparam>
public class NullLogger<T> : ILogger<T>
{
    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => false;

    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        // Do nothing - this is a null logger
    }
}