using CrowdQR.Api.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace CrowdQR.Api.Tests.Controllers;

/// <summary>
/// Unit tests for the PingController.
/// </summary>
public class PingControllerTests
{
    private readonly PingController _controller;

    /// <summary>
    /// Initializes a new instance of the <see cref="PingControllerTests"/> class.
    /// </summary>
    public PingControllerTests()
    {
        _controller = new PingController();
    }

    /// <summary>
    /// Tests that the Ping endpoint returns OK with expected structure.
    /// </summary>
    [Fact]
    public void Ping_ReturnsOkWithExpectedStructure()
    {
        // Act
        var result = _controller.Ping();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult?.Value.Should().NotBeNull();

        // Verify the response structure
        dynamic? response = okResult?.Value;
        response?.Should().NotBeNull();

        // Check that response has expected properties
        var responseType = response?.GetType();
        responseType?.GetProperty("status").Should().NotBeNull();
        responseType?.GetProperty("timestamp").Should().NotBeNull();
        responseType?.GetProperty("environment").Should().NotBeNull();
    }

    /// <summary>
    /// Tests that the Ping endpoint returns status "ok".
    /// </summary>
    [Fact]
    public void Ping_ReturnsStatusOk()
    {
        // Act
        var result = _controller.Ping();

        // Assert
        var okResult = result as OkObjectResult;
        dynamic? response = okResult?.Value;

        // Use reflection to get the status value
        var statusProperty = response?.GetType().GetProperty("status");
        var statusValue = statusProperty?.GetValue(response);
        statusValue?.ToString().Should().Be("ok");
    }

    /// <summary>
    /// Tests that the Ping endpoint returns a recent timestamp.
    /// </summary>
    [Fact]
    public void Ping_ReturnsRecentTimestamp()
    {
        // Arrange
        var beforeCall = DateTime.UtcNow;

        // Act
        var result = _controller.Ping();

        // Assert
        var afterCall = DateTime.UtcNow;
        var okResult = result as OkObjectResult;
        dynamic? response = okResult?.Value;

        // Use reflection to get the timestamp value
        var timestampProperty = response?.GetType().GetProperty("timestamp");
        var timestampValue = timestampProperty?.GetValue(response);

        if (timestampValue is DateTime timestamp)
        {
            timestamp.Should().BeAfter(beforeCall.AddSeconds(-1)); // Allow small timing difference
            timestamp.Should().BeBefore(afterCall.AddSeconds(1));
        }
        else
        {
            Assert.Fail("Timestamp should be a DateTime");
        }
    }

    /// <summary>
    /// Tests that the Ping endpoint returns environment information.
    /// </summary>
    [Fact]
    public void Ping_ReturnsEnvironmentInfo()
    {
        // Act
        var result = _controller.Ping();

        // Assert
        var okResult = result as OkObjectResult;
        dynamic? response = okResult?.Value;

        // Use reflection to get the environment value
        var environmentProperty = response?.GetType().GetProperty("environment");
        var environmentValue = environmentProperty?.GetValue(response);

        environmentValue?.Should().NotBeNull();
        environmentValue?.ToString().Should().NotBeNullOrEmpty();

        // In test environment, it should be "Production" (default) since no env var is set
        environmentValue?.ToString().Should().Be("Production");
    }

    /// <summary>
    /// Tests that multiple calls to Ping return consistent structure.
    /// </summary>
    [Fact]
    public void Ping_MultipleCalls_ReturnConsistentStructure()
    {
        // Act
        var result1 = _controller.Ping();
        var result2 = _controller.Ping();

        // Assert
        result1.Should().BeOfType<OkObjectResult>();
        result2.Should().BeOfType<OkObjectResult>();

        var okResult1 = result1 as OkObjectResult;
        var okResult2 = result2 as OkObjectResult;

        dynamic? response1 = okResult1?.Value;
        dynamic? response2 = okResult2?.Value;

        // Both responses should have the same structure
        response1?.GetType().Should().Be(response2?.GetType());

        // Status should be the same
        var status1 = response1?.GetType().GetProperty("status")?.GetValue(response1);
        var status2 = response2?.GetType().GetProperty("status")?.GetValue(response2);
        status1?.Should().Be(status2);

        // Environment should be the same
        var env1 = response1?.GetType().GetProperty("environment")?.GetValue(response1);
        var env2 = response2?.GetType().GetProperty("environment")?.GetValue(response2);
        env1?.Should().Be(env2);
    }
}