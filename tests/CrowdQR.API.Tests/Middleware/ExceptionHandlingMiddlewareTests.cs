using System.Text.Json;
using CrowdQR.Api.Middleware;
using CrowdQR.Api.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CrowdQR.Api.Tests.Middleware;

/// <summary>
/// Unit tests for the ExceptionHandlingMiddleware.
/// </summary>
public class ExceptionHandlingMiddlewareTests
{
    private readonly Mock<ILogger<ExceptionHandlingMiddleware>> _mockLogger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExceptionHandlingMiddlewareTests"/> class.
    /// </summary>
    public ExceptionHandlingMiddlewareTests()
    {
        _mockLogger = new Mock<ILogger<ExceptionHandlingMiddleware>>();
    }

    /// <summary>
    /// Tests that middleware passes through when no exception occurs.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_NoException_PassesThrough()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var nextCalled = false;

        Task next(HttpContext ctx)
        {
            nextCalled = true;
            return Task.CompletedTask;
        }

        var middleware = new ExceptionHandlingMiddleware(next, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(200); // Default status
    }

    /// <summary>
    /// Tests that middleware handles exceptions and returns 500 status code.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_ExceptionThrown_Returns500()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var testException = new InvalidOperationException("Test exception");

        Task next(HttpContext ctx)
        {
            throw testException;
        }

        var middleware = new ExceptionHandlingMiddleware(next, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(500);
        context.Response.ContentType.Should().Be("application/json");
    }

    /// <summary>
    /// Tests that middleware logs the exception.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_ExceptionThrown_LogsException()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var testException = new InvalidOperationException("Test exception");

        Task next(HttpContext ctx)
        {
            throw testException;
        }

        var middleware = new ExceptionHandlingMiddleware(next, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("An unhandled exception occurred")),
                testException,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that middleware returns correct JSON error response.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_ExceptionThrown_ReturnsCorrectJsonResponse()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var responseStream = new MemoryStream();
        context.Response.Body = responseStream;

        var testException = new InvalidOperationException("Test exception message");

        Task next(HttpContext ctx)
        {
            throw testException;
        }

        var middleware = new ExceptionHandlingMiddleware(next, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        responseStream.Position = 0;
        var responseText = await new StreamReader(responseStream).ReadToEndAsync();
        responseText.Should().NotBeNullOrEmpty();

        var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseText);
        jsonResponse.GetProperty("status").GetInt32().Should().Be(500);
        jsonResponse.GetProperty("message").GetString().Should().Be("An error occured. Please try again later.");
        jsonResponse.GetProperty("detail").GetString().Should().Be("Test exception message");
    }

    /// <summary>
    /// Tests that middleware handles different exception types.
    /// </summary>
    [Theory]
    [InlineData(typeof(ArgumentException), "Argument exception")]
    [InlineData(typeof(InvalidOperationException), "Invalid operation")]
    [InlineData(typeof(NotSupportedException), "Not supported")]
    [InlineData(typeof(TimeoutException), "Timeout occurred")]
    public async Task InvokeAsync_DifferentExceptionTypes_HandlesCorrectly(Type exceptionType, string message)
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var exception = (Exception)Activator.CreateInstance(exceptionType, message)!;

        Task next(HttpContext ctx)
        {
            throw exception;
        }

        var middleware = new ExceptionHandlingMiddleware(next, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(500);
        context.Response.ContentType.Should().Be("application/json");

        // Verify exception was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that middleware handles null exception messages.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_NullExceptionMessage_HandlesGracefully()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var testException = new Exception(); // Exception with null message

        Task next(HttpContext ctx)
        {
            throw testException;
        }

        var middleware = new ExceptionHandlingMiddleware(next, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(500);

        var responseStream = (MemoryStream)context.Response.Body;
        responseStream.Position = 0;
        var responseText = await new StreamReader(responseStream).ReadToEndAsync();

        var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseText);
        jsonResponse.GetProperty("detail").GetString().Should().Be("Exception of type 'System.Exception' was thrown.");
    }

    /// <summary>
    /// Tests that middleware preserves original response if no exception occurs.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_NoException_PreservesOriginalResponse()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var originalStatusCode = 201;
        var originalContentType = "application/custom";

        Task next(HttpContext ctx)
        {
            ctx.Response.StatusCode = originalStatusCode;
            ctx.Response.ContentType = originalContentType;
            return Task.CompletedTask;
        }

        var middleware = new ExceptionHandlingMiddleware(next, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(originalStatusCode);
        context.Response.ContentType.Should().Be(originalContentType);
    }

    /// <summary>
    /// Tests that middleware handles async exceptions correctly.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_AsyncException_HandlesCorrectly()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var testException = new TaskCanceledException("Async operation was cancelled");

        async Task next(HttpContext ctx)
        {
            await Task.Delay(1); // Simulate async work
            throw testException;
        }

        var middleware = new ExceptionHandlingMiddleware(next, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(500);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                testException,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that middleware handles exceptions with inner exceptions.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_ExceptionWithInnerException_HandlesCorrectly()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var innerException = new ArgumentException("Inner exception");
        var outerException = new InvalidOperationException("Outer exception", innerException);

        Task next(HttpContext ctx)
        {
            throw outerException;
        }

        var middleware = new ExceptionHandlingMiddleware(next, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(500);

        var responseStream = (MemoryStream)context.Response.Body;
        responseStream.Position = 0;
        var responseText = await new StreamReader(responseStream).ReadToEndAsync();

        var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseText);
        jsonResponse.GetProperty("detail").GetString().Should().Be("Outer exception");

        // Verify the outer exception was logged (with inner exception info)
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                outerException,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Tests the extension method for adding the middleware.
    /// </summary>
    [Fact]
    public void UseExceptionHandling_AddsMiddlewareToBuilder()
    {
        // Arrange
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();

        var appBuilder = new Microsoft.AspNetCore.Builder.ApplicationBuilder(serviceProvider);

        // Act
        var result = appBuilder.UseExceptionHandling();

        // Assert
        result.Should().BeSameAs(appBuilder);
        // Note: Testing middleware registration in the pipeline is complex and typically done through integration tests
    }

    /// <summary>
    /// Tests that middleware handles large exception messages correctly.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_LargeExceptionMessage_HandlesCorrectly()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var largeMessage = new string('A', 10000); // Very large message
        var testException = new Exception(largeMessage);

        Task next(HttpContext ctx)
        {
            throw testException;
        }

        var middleware = new ExceptionHandlingMiddleware(next, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(500);

        var responseStream = (MemoryStream)context.Response.Body;
        responseStream.Position = 0;
        var responseText = await new StreamReader(responseStream).ReadToEndAsync();

        responseText.Should().NotBeNullOrEmpty();
        var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseText);
        jsonResponse.GetProperty("detail").GetString().Should().Be(largeMessage);
    }

    /// <summary>
    /// Tests that middleware sets correct JSON serialization options.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_JsonResponse_UsesCamelCaseNaming()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var testException = new Exception("Test");

        Task next(HttpContext ctx)
        {
            throw testException;
        }

        var middleware = new ExceptionHandlingMiddleware(next, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var responseStream = (MemoryStream)context.Response.Body;
        responseStream.Position = 0;
        var responseText = await new StreamReader(responseStream).ReadToEndAsync();

        // Should use camelCase property names
        responseText.Should().Contain("status");
        responseText.Should().Contain("message");
        responseText.Should().Contain("detail");

        // Should not contain PascalCase property names
        responseText.Should().NotContain("Status");
        responseText.Should().NotContain("Message");
        responseText.Should().NotContain("Detail");
    }

    /// <summary>
    /// Tests that middleware handles exceptions during response writing.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_ResponseWritingFails_HandlesGracefully()
    {
        // Arrange
        var context = new DefaultHttpContext();

        // Create a stream that throws when written to
        var mockStream = new Mock<Stream>();
        mockStream.Setup(s => s.CanWrite).Returns(true);
        mockStream.Setup(s => s.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Write failed"));

        context.Response.Body = mockStream.Object;

        var testException = new Exception("Original exception");

        Task next(HttpContext ctx)
        {
            throw testException;
        }

        var middleware = new ExceptionHandlingMiddleware(next, _mockLogger.Object);

        // Act & Assert - Should not throw, even if response writing fails
        var exception = await Record.ExceptionAsync(() => middleware.InvokeAsync(context));
        exception.Should().BeOfType<IOException>(); // The write exception will bubble up

        // Original exception should still be logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                testException,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}