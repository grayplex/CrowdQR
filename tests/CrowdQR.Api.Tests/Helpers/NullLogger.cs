using Microsoft.Extensions.Logging;

namespace CrowdQR.Api.Tests.Helpers;

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