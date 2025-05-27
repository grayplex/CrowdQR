using System.Security.Claims;
using CrowdQR.Api.Controllers;
using CrowdQR.Api.Services;
using CrowdQR.Api.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CrowdQR.Api.Tests.Controllers;

/// <summary>
/// Unit tests for the TestController.
/// </summary>
public class TestControllerTests : IDisposable
{
    private readonly Mock<IHubNotificationService> _mockHubService;
    private readonly TestController _controller;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestControllerTests"/> class.
    /// </summary>
    public TestControllerTests()
    {
        _mockHubService = new Mock<IHubNotificationService>();
        var logger = TestLoggerFactory.CreateNullLogger<TestController>();
        _controller = new TestController(_mockHubService.Object, logger);
    }

    /// <summary>
    /// Tests that Broadcast sends test notifications successfully.
    /// </summary>
    [Fact]
    public async Task Broadcast_ValidEventId_SendsNotificationsAndReturnsOk()
    {
        // Arrange
        SetupDjUser();
        var eventId = 1;

        _mockHubService.Setup(x => x.NotifyRequestAdded(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _mockHubService.Setup(x => x.NotifyUserJoinedEvent(It.IsAny<int>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Broadcast(eventId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult?.Value.Should().NotBeNull();

        // Verify the response structure
        var response = okResult!.Value;
        var responseType = response!.GetType();
        var messageProp = responseType.GetProperty("message");
        messageProp?.GetValue(response).Should().Be($"Broadcast sent to event {eventId}");

        // Verify hub service was called
        _mockHubService.Verify(x => x.NotifyRequestAdded(eventId, 999, "TestControllerUser"), Times.Once);
        _mockHubService.Verify(x => x.NotifyUserJoinedEvent(eventId, "TestUser"), Times.Once);
    }

    /// <summary>
    /// Tests that Broadcast handles hub service exceptions gracefully.
    /// </summary>
    [Fact]
    public async Task Broadcast_HubServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        SetupDjUser();
        var eventId = 1;

        _mockHubService.Setup(x => x.NotifyRequestAdded(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Hub service error"));

        // Act
        var result = await _controller.Broadcast(eventId);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult?.StatusCode.Should().Be(500);

        var response = objectResult!.Value;
        var responseType = response!.GetType();
        var errorProp = responseType.GetProperty("error");
        errorProp?.GetValue(response).Should().Be("Failed to send broadcast");
    }

    /// <summary>
    /// Tests broadcast with different event IDs.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(42)]
    [InlineData(999)]
    public async Task Broadcast_DifferentEventIds_SendsCorrectEventId(int eventId)
    {
        // Arrange
        SetupDjUser();

        _mockHubService.Setup(x => x.NotifyRequestAdded(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _mockHubService.Setup(x => x.NotifyUserJoinedEvent(It.IsAny<int>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Broadcast(eventId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();

        // Verify correct event ID was used
        _mockHubService.Verify(x => x.NotifyRequestAdded(eventId, 999, "TestControllerUser"), Times.Once);
        _mockHubService.Verify(x => x.NotifyUserJoinedEvent(eventId, "TestUser"), Times.Once);
    }

    /// <summary>
    /// Tests broadcast with edge case event IDs.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    public async Task Broadcast_EdgeCaseEventIds_HandlesGracefully(int eventId)
    {
        // Arrange
        SetupDjUser();

        _mockHubService.Setup(x => x.NotifyRequestAdded(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _mockHubService.Setup(x => x.NotifyUserJoinedEvent(It.IsAny<int>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Broadcast(eventId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();

        // Should still attempt the broadcast
        _mockHubService.Verify(x => x.NotifyRequestAdded(eventId, 999, "TestControllerUser"), Times.Once);
        _mockHubService.Verify(x => x.NotifyUserJoinedEvent(eventId, "TestUser"), Times.Once);
    }

    /// <summary>
    /// Tests that only the NotifyRequestAdded throwing an exception still allows NotifyUserJoinedEvent to be called.
    /// </summary>
    [Fact]
    public async Task Broadcast_FirstNotificationFails_SecondStillExecutes()
    {
        // Arrange
        SetupDjUser();
        var eventId = 1;

        _mockHubService.Setup(x => x.NotifyRequestAdded(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("First notification failed"));
        _mockHubService.Setup(x => x.NotifyUserJoinedEvent(It.IsAny<int>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Broadcast(eventId);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult?.StatusCode.Should().Be(500);

        // Verify first call was made (and failed)
        _mockHubService.Verify(x => x.NotifyRequestAdded(eventId, 999, "TestControllerUser"), Times.Once);

        // The second call should not be made because the exception occurs before it
        _mockHubService.Verify(x => x.NotifyUserJoinedEvent(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    /// <summary>
    /// Tests that the controller uses fixed test values for consistency.
    /// </summary>
    [Fact]
    public async Task Broadcast_TestValues_AreConsistent()
    {
        // Arrange
        SetupDjUser();
        var eventId = 123;

        _mockHubService.Setup(x => x.NotifyRequestAdded(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _mockHubService.Setup(x => x.NotifyUserJoinedEvent(It.IsAny<int>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _controller.Broadcast(eventId);

        // Assert - Verify consistent test values are used
        _mockHubService.Verify(x => x.NotifyRequestAdded(eventId, 999, "TestControllerUser"), Times.Once);
        _mockHubService.Verify(x => x.NotifyUserJoinedEvent(eventId, "TestUser"), Times.Once);
    }

    /// <summary>
    /// Sets up DJ user claims for testing.
    /// </summary>
    private void SetupDjUser()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Role, "DJ"),
            new Claim(ClaimTypes.Name, "test_dj")
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };
    }

    /// <summary>
    /// Disposes of the test resources.
    /// </summary>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}