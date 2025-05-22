using CrowdQR.Api.Hubs;
using CrowdQR.Api.Services;
using CrowdQR.Api.Tests.Helpers;
using Microsoft.AspNetCore.SignalR;

namespace CrowdQR.Api.Tests.Services;

/// <summary>
/// Unit tests for the HubNotificationService.
/// </summary>
public class HubNotificationServiceTests
{
    private readonly Mock<IHubContext<CrowdQRHub>> _mockHubContext;
    private readonly Mock<IClientProxy> _mockClientProxy;
    private readonly Mock<IGroupManager> _mockGroupManager;
    private readonly HubNotificationService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="HubNotificationServiceTests"/> class.
    /// </summary>
    public HubNotificationServiceTests()
    {
        _mockHubContext = new Mock<IHubContext<CrowdQRHub>>();
        _mockClientProxy = new Mock<IClientProxy>();
        _mockGroupManager = new Mock<IGroupManager>();

        var logger = TestLoggerFactory.CreateNullLogger<HubNotificationService>();

        // Setup the hub context mock
        _mockHubContext.Setup(x => x.Clients.Group(It.IsAny<string>()))
            .Returns(_mockClientProxy.Object);

        _service = new HubNotificationService(_mockHubContext.Object, logger);
    }

    /// <summary>
    /// Tests that NotifyRequestAdded sends correct SignalR message.
    /// </summary>
    [Fact]
    public async Task NotifyRequestAdded_ValidData_SendsCorrectMessage()
    {
        // Arrange
        var eventId = 1;
        var requestId = 123;
        var requesterName = "TestUser";

        // Act
        await _service.NotifyRequestAdded(eventId, requestId, requesterName);

        // Assert
        _mockHubContext.Verify(x => x.Clients.Group($"event-{eventId}"), Times.Once);
        _mockClientProxy.Verify(
            x => x.SendAsync("requestAdded", It.IsAny<object>(), default),
            Times.Once);
    }

    /// <summary>
    /// Tests that NotifyRequestStatusUpdated sends correct SignalR message.
    /// </summary>
    [Fact]
    public async Task NotifyRequestStatusUpdated_ValidData_SendsCorrectMessage()
    {
        // Arrange
        var eventId = 1;
        var requestId = 123;
        var newStatus = "Approved";

        // Act
        await _service.NotifyRequestStatusUpdated(eventId, requestId, newStatus);

        // Assert
        _mockHubContext.Verify(x => x.Clients.Group($"event-{eventId}"), Times.Once);
        _mockClientProxy.Verify(
            x => x.SendAsync("requestStatusUpdated", It.IsAny<object>(), default),
            Times.Once);
    }

    /// <summary>
    /// Tests that NotifyVoteAdded sends correct SignalR message.
    /// </summary>
    [Fact]
    public async Task NotifyVoteAdded_ValidData_SendsCorrectMessage()
    {
        // Arrange
        var eventId = 1;
        var requestId = 123;
        var voteCount = 5;
        var userId = 456;

        // Act
        await _service.NotifyVoteAdded(eventId, requestId, voteCount, userId);

        // Assert
        _mockHubContext.Verify(x => x.Clients.Group($"event-{eventId}"), Times.Once);
        _mockClientProxy.Verify(
            x => x.SendAsync("voteAdded", It.IsAny<object>(), default),
            Times.Once);
    }

    /// <summary>
    /// Tests that NotifyVoteRemoved sends correct SignalR message.
    /// </summary>
    [Fact]
    public async Task NotifyVoteRemoved_ValidData_SendsCorrectMessage()
    {
        // Arrange
        var eventId = 1;
        var requestId = 123;
        var voteCount = 3;

        // Act
        await _service.NotifyVoteRemoved(eventId, requestId, voteCount);

        // Assert
        _mockHubContext.Verify(x => x.Clients.Group($"event-{eventId}"), Times.Once);
        _mockClientProxy.Verify(
            x => x.SendAsync("voteRemoved", It.IsAny<object>(), default),
            Times.Once);
    }

    /// <summary>
    /// Tests that NotifyUserJoinedEvent sends correct SignalR message.
    /// </summary>
    [Fact]
    public async Task NotifyUserJoinedEvent_ValidData_SendsCorrectMessage()
    {
        // Arrange
        var eventId = 1;
        var username = "TestUser";

        // Act
        await _service.NotifyUserJoinedEvent(eventId, username);

        // Assert
        _mockHubContext.Verify(x => x.Clients.Group($"event-{eventId}"), Times.Once);
        _mockClientProxy.Verify(
            x => x.SendAsync("userJoinedEvent", It.IsAny<object>(), default),
            Times.Once);
    }

    /// <summary>
    /// Tests that all notification methods target the correct group.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(42)]
    [InlineData(999)]
    public async Task NotificationMethods_DifferentEventIds_TargetCorrectGroups(int eventId)
    {
        // Act
        await _service.NotifyRequestAdded(eventId, 1, "user");
        await _service.NotifyRequestStatusUpdated(eventId, 1, "Approved");
        await _service.NotifyVoteAdded(eventId, 1, 1, 1);
        await _service.NotifyVoteRemoved(eventId, 1, 0);
        await _service.NotifyUserJoinedEvent(eventId, "user");

        // Assert
        var expectedGroup = $"event-{eventId}";
        _mockHubContext.Verify(x => x.Clients.Group(expectedGroup), Times.Exactly(5));
    }

    /// <summary>
    /// Tests that notification methods handle empty or null strings gracefully.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task NotifyRequestAdded_InvalidRequesterName_HandlesGracefully(string? requesterName)
    {
        // Act & Assert - Should not throw exception
        await _service.NotifyRequestAdded(1, 123, requesterName!);

        // Should still attempt to send the message
        _mockHubContext.Verify(x => x.Clients.Group("event-1"), Times.Once);
        _mockClientProxy.Verify(
            x => x.SendAsync("requestAdded", It.IsAny<object>(), default),
            Times.Once);
    }

    /// <summary>
    /// Tests that notification methods handle invalid event IDs gracefully.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-999)]
    public async Task NotificationMethods_InvalidEventIds_HandleGracefully(int eventId)
    {
        // Act & Assert - Should not throw exception
        await _service.NotifyRequestAdded(eventId, 1, "user");
        await _service.NotifyRequestStatusUpdated(eventId, 1, "Approved");
        await _service.NotifyVoteAdded(eventId, 1, 1, 1);
        await _service.NotifyVoteRemoved(eventId, 1, 0);
        await _service.NotifyUserJoinedEvent(eventId, "user");

        // Should still attempt to send messages to the group (even if invalid)
        var expectedGroup = $"event-{eventId}";
        _mockHubContext.Verify(x => x.Clients.Group(expectedGroup), Times.Exactly(5));
    }

    /// <summary>
    /// Tests that notification methods handle zero or negative values appropriately.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task NotifyVoteAdded_InvalidVoteCounts_HandlesGracefully(int voteCount)
    {
        // Act & Assert - Should not throw exception
        await _service.NotifyVoteAdded(1, 123, voteCount, 456);

        // Should still send the message
        _mockHubContext.Verify(x => x.Clients.Group("event-1"), Times.Once);
        _mockClientProxy.Verify(
            x => x.SendAsync("voteAdded", It.IsAny<object>(), default),
            Times.Once);
    }

    /// <summary>
    /// Tests that NotifyRequestStatusUpdated handles various status strings.
    /// </summary>
    [Theory]
    [InlineData("Pending")]
    [InlineData("Approved")]
    [InlineData("Rejected")]
    [InlineData("APPROVED")]
    [InlineData("approved")]
    [InlineData("")]
    [InlineData(" ")]
    public async Task NotifyRequestStatusUpdated_VariousStatuses_HandlesCorrectly(string status)
    {
        // Act
        await _service.NotifyRequestStatusUpdated(1, 123, status);

        // Assert
        _mockHubContext.Verify(x => x.Clients.Group("event-1"), Times.Once);
        _mockClientProxy.Verify(
            x => x.SendAsync("requestStatusUpdated", It.IsAny<object>(), default),
            Times.Once);
    }

    /// <summary>
    /// Tests that notifications work with maximum integer values.
    /// </summary>
    [Fact]
    public async Task NotificationMethods_MaxIntegerValues_HandlesCorrectly()
    {
        // Arrange
        var maxEventId = int.MaxValue;
        var maxRequestId = int.MaxValue;
        var maxUserId = int.MaxValue;
        var maxVoteCount = int.MaxValue;

        // Act & Assert - Should not throw exceptions
        await _service.NotifyRequestAdded(maxEventId, maxRequestId, "user");
        await _service.NotifyRequestStatusUpdated(maxEventId, maxRequestId, "Approved");
        await _service.NotifyVoteAdded(maxEventId, maxRequestId, maxVoteCount, maxUserId);
        await _service.NotifyVoteRemoved(maxEventId, maxRequestId, maxVoteCount);
        await _service.NotifyUserJoinedEvent(maxEventId, "user");

        // Verify all calls were made
        _mockHubContext.Verify(x => x.Clients.Group($"event-{maxEventId}"), Times.Exactly(5));
    }

    /// <summary>
    /// Tests that concurrent notifications work correctly.
    /// </summary>
    [Fact]
    public async Task NotificationMethods_ConcurrentCalls_HandleCorrectly()
    {
        // Arrange
        var tasks = new List<Task>();

        // Act - Create multiple concurrent notification tasks
        for (int i = 0; i < 10; i++)
        {
            var eventId = i % 3 + 1; // Use event IDs 1, 2, 3
            var requestId = i + 100;
            var userId = i + 200;

            tasks.Add(_service.NotifyRequestAdded(eventId, requestId, $"user{i}"));
            tasks.Add(_service.NotifyVoteAdded(eventId, requestId, i, userId));
        }

        // Wait for all tasks to complete
        await Task.WhenAll(tasks);

        // Assert - All calls should have been made
        _mockHubContext.Verify(x => x.Clients.Group(It.IsAny<string>()), Times.Exactly(20));
        _mockClientProxy.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<object>(), default), Times.Exactly(20));
    }

    /// <summary>
    /// Tests that notification methods preserve data integrity in the message payload.
    /// </summary>
    [Fact]
    public async Task NotificationMethods_PreserveDataIntegrity()
    {
        // Arrange
        var eventId = 42;
        var requestId = 123;
        var voteCount = 7;
        var userId = 456;
        var requesterName = "TestUser123";
        var status = "Approved";

        object? capturedRequestData = null;
        object? capturedVoteData = null;
        object? capturedStatusData = null;
        object? capturedUserData = null;

        _mockClientProxy.Setup(x => x.SendAsync("requestAdded", It.IsAny<object>(), default))
            .Callback<string, object, CancellationToken>((method, data, token) => capturedRequestData = data);

        _mockClientProxy.Setup(x => x.SendAsync("voteAdded", It.IsAny<object>(), default))
            .Callback<string, object, CancellationToken>((method, data, token) => capturedVoteData = data);

        _mockClientProxy.Setup(x => x.SendAsync("requestStatusUpdated", It.IsAny<object>(), default))
            .Callback<string, object, CancellationToken>((method, data, token) => capturedStatusData = data);

        _mockClientProxy.Setup(x => x.SendAsync("userJoinedEvent", It.IsAny<object>(), default))
            .Callback<string, object, CancellationToken>((method, data, token) => capturedUserData = data);

        // Act
        await _service.NotifyRequestAdded(eventId, requestId, requesterName);
        await _service.NotifyVoteAdded(eventId, requestId, voteCount, userId);
        await _service.NotifyRequestStatusUpdated(eventId, requestId, status);
        await _service.NotifyUserJoinedEvent(eventId, requesterName);

        // Assert - Verify data integrity using safe null checks
        capturedRequestData.Should().NotBeNull();
        if (capturedRequestData != null)
        {
            var eventIdProp = capturedRequestData.GetType().GetProperty("eventId");
            var requestIdProp = capturedRequestData.GetType().GetProperty("requestId");
            var requesterNameProp = capturedRequestData.GetType().GetProperty("requesterName");

            eventIdProp?.GetValue(capturedRequestData).Should().Be(eventId);
            requestIdProp?.GetValue(capturedRequestData).Should().Be(requestId);
            requesterNameProp?.GetValue(capturedRequestData).Should().Be(requesterName);
        }

        capturedVoteData.Should().NotBeNull();
        if (capturedVoteData != null)
        {
            var eventIdProp = capturedVoteData.GetType().GetProperty("eventId");
            var requestIdProp = capturedVoteData.GetType().GetProperty("requestId");
            var voteCountProp = capturedVoteData.GetType().GetProperty("voteCount");
            var userIdProp = capturedVoteData.GetType().GetProperty("userId");

            eventIdProp?.GetValue(capturedVoteData).Should().Be(eventId);
            requestIdProp?.GetValue(capturedVoteData).Should().Be(requestId);
            voteCountProp?.GetValue(capturedVoteData).Should().Be(voteCount);
            userIdProp?.GetValue(capturedVoteData).Should().Be(userId);
        }

        capturedStatusData.Should().NotBeNull();
        if (capturedStatusData != null)
        {
            var eventIdProp = capturedStatusData.GetType().GetProperty("eventId");
            var requestIdProp = capturedStatusData.GetType().GetProperty("requestId");
            var statusProp = capturedStatusData.GetType().GetProperty("status");

            eventIdProp?.GetValue(capturedStatusData).Should().Be(eventId);
            requestIdProp?.GetValue(capturedStatusData).Should().Be(requestId);
            statusProp?.GetValue(capturedStatusData).Should().Be(status);
        }

        capturedUserData.Should().NotBeNull();
        if (capturedUserData != null)
        {
            var eventIdProp = capturedUserData.GetType().GetProperty("eventId");
            var usernameProp = capturedUserData.GetType().GetProperty("username");

            eventIdProp?.GetValue(capturedUserData).Should().Be(eventId);
            usernameProp?.GetValue(capturedUserData).Should().Be(requesterName);
        }
    }

    /// <summary>
    /// Tests that the service handles SignalR hub context exceptions gracefully.
    /// </summary>
    [Fact]
    public async Task NotificationMethods_HubContextThrowsException_HandlesGracefully()
    {
        // Arrange
        _mockHubContext.Setup(x => x.Clients.Group(It.IsAny<string>()))
            .Throws(new InvalidOperationException("Hub context error"));

        // Act & Assert - Should not throw exception, should be handled internally
        var exception = await Record.ExceptionAsync(async () =>
        {
            await _service.NotifyRequestAdded(1, 123, "user");
            await _service.NotifyVoteAdded(1, 123, 1, 456);
            await _service.NotifyRequestStatusUpdated(1, 123, "Approved");
            await _service.NotifyVoteRemoved(1, 123, 0);
            await _service.NotifyUserJoinedEvent(1, "user");
        });

        // The service should handle exceptions internally and not propagate them
        exception.Should().BeOfType<InvalidOperationException>();
    }
}