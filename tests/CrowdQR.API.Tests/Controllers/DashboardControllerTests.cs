using System.Security.Claims;
using CrowdQR.Api.Controllers;
using CrowdQR.Api.Data;
using CrowdQR.Api.Models;
using CrowdQR.Api.Tests.Helpers;
using CrowdQR.Shared.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrowdQR.Api.Tests.Controllers;

/// <summary>
/// Unit tests for the DashboardController.
/// </summary>
public class DashboardControllerTests : IDisposable
{
    private readonly CrowdQRContext _context;
    private readonly DashboardController _controller;

    /// <summary>
    /// Initializes a new instance of the <see cref="DashboardControllerTests"/> class.
    /// </summary>
    public DashboardControllerTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        var logger = TestLoggerFactory.CreateNullLogger<DashboardController>();
        _controller = new DashboardController(_context, logger);
    }

    /// <summary>
    /// Tests getting event summary with valid event ID.
    /// </summary>
    [Fact]
    public async Task GetEventSummary_ValidEventId_ReturnsEventSummary()
    {
        // Arrange
        await SeedDashboardTestDataAsync();
        SetupUserClaims(1, "DJ");

        // Act
        var result = await _controller.GetEventSummary(1);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult?.Value.Should().NotBeNull();

        dynamic? summary = okResult?.Value;
        summary?.Should().NotBeNull();
        summary?.EventId.Should().Be(1);
        summary?.TotalRequests.Should().Be(3);
        summary?.PendingRequests.Should().Be(2);
        summary?.ApprovedRequests.Should().Be(1);
        summary?.RejectedRequests.Should().Be(0);
    }

    /// <summary>
    /// Tests getting event summary for non-existent event.
    /// </summary>
    [Fact]
    public async Task GetEventSummary_NonExistentEvent_ReturnsNotFound()
    {
        // Arrange
        await SeedDashboardTestDataAsync();
        SetupUserClaims(1, "DJ");

        // Act
        var result = await _controller.GetEventSummary(999);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    /// <summary>
    /// Tests getting top requests for an event.
    /// </summary>
    [Fact]
    public async Task GetTopRequests_ValidEventId_ReturnsTopRequests()
    {
        // Arrange
        await SeedDashboardTestDataAsync();
        SetupUserClaims(1, "DJ");

        // Act
        var result = await _controller.GetTopRequests(1, RequestStatus.Pending, 10);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var requests = okResult?.Value as IEnumerable<object>;
        requests.Should().NotBeNull();
        requests!.Count().Should().Be(2); // Two pending requests
    }

    /// <summary>
    /// Tests getting top requests for non-existent event.
    /// </summary>
    [Fact]
    public async Task GetTopRequests_NonExistentEvent_ReturnsNotFound()
    {
        // Arrange
        await SeedDashboardTestDataAsync();
        SetupUserClaims(1, "DJ");

        // Act
        var result = await _controller.GetTopRequests(999, RequestStatus.Pending, 10);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    /// <summary>
    /// Tests getting top requests with custom count limit.
    /// </summary>
    [Fact]
    public async Task GetTopRequests_CustomCount_ReturnsLimitedResults()
    {
        // Arrange
        await SeedDashboardTestDataAsync();
        SetupUserClaims(1, "DJ");

        // Act
        var result = await _controller.GetTopRequests(1, RequestStatus.Pending, 1);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var requests = okResult?.Value as IEnumerable<object>;
        requests.Should().NotBeNull();
        requests!.Count().Should().Be(1); // Limited to 1 result
    }

    /// <summary>
    /// Tests getting approved requests.
    /// </summary>
    [Fact]
    public async Task GetTopRequests_ApprovedStatus_ReturnsApprovedRequests()
    {
        // Arrange
        await SeedDashboardTestDataAsync();
        SetupUserClaims(1, "DJ");

        // Act
        var result = await _controller.GetTopRequests(1, RequestStatus.Approved, 10);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var requests = okResult?.Value as IEnumerable<object>;
        requests.Should().NotBeNull();
        requests!.Count().Should().Be(1); // One approved request
    }

    /// <summary>
    /// Tests getting DJ event statistics.
    /// </summary>
    [Fact]
    public async Task GetDJEventStats_ValidDjId_ReturnsEventStats()
    {
        // Arrange
        await SeedDashboardTestDataAsync();
        SetupUserClaims(1, "DJ");

        // Act
        var result = await _controller.GetDJEventStats(1);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var stats = okResult?.Value as IEnumerable<object>;
        stats.Should().NotBeNull();
        stats!.Count().Should().Be(1); // One event for this DJ
    }

    /// <summary>
    /// Tests getting DJ event statistics for non-existent DJ.
    /// </summary>
    [Fact]
    public async Task GetDJEventStats_NonExistentDj_ReturnsNotFound()
    {
        // Arrange
        await SeedDashboardTestDataAsync();
        SetupUserClaims(1, "DJ");

        // Act
        var result = await _controller.GetDJEventStats(999);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    /// <summary>
    /// Tests getting DJ event statistics for non-DJ user.
    /// </summary>
    [Fact]
    public async Task GetDJEventStats_NonDjUser_ReturnsNotFound()
    {
        // Arrange
        await SeedDashboardTestDataAsync();
        SetupUserClaims(1, "DJ");

        // Act - Try to get stats for audience user
        var result = await _controller.GetDJEventStats(2);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    /// <summary>
    /// Tests that event summary includes active users.
    /// </summary>
    [Fact]
    public async Task GetEventSummary_WithActiveSessions_IncludesActiveUsers()
    {
        // Arrange
        await SeedDashboardTestDataAsync();
        SetupUserClaims(1, "DJ");

        // Add active sessions (within last 15 minutes)
        var activeSessions = new[]
        {
            new Session
            {
                UserId = 2,
                EventId = 1,
                ClientIP = "192.168.1.1",
                LastSeen = DateTime.UtcNow.AddMinutes(-5),
                RequestCount = 1
            },
            new Session
            {
                UserId = 3,
                EventId = 1,
                ClientIP = "192.168.1.2",
                LastSeen = DateTime.UtcNow.AddMinutes(-10),
                RequestCount = 0
            }
        };
        _context.Sessions.AddRange(activeSessions);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetEventSummary(1);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        dynamic? summary = okResult?.Value;
        summary?.ActiveUsers.Should().Be(2);
    }

    /// <summary>
    /// Tests that event summary excludes inactive sessions.
    /// </summary>
    [Fact]
    public async Task GetEventSummary_WithInactiveSessions_ExcludesInactiveUsers()
    {
        // Arrange
        await SeedDashboardTestDataAsync();
        SetupUserClaims(1, "DJ");

        // Add inactive sessions (older than 15 minutes)
        var inactiveSessions = new[]
        {
            new Session
            {
                UserId = 2,
                EventId = 1,
                ClientIP = "192.168.1.1",
                LastSeen = DateTime.UtcNow.AddMinutes(-20), // Inactive
                RequestCount = 1
            },
            new Session
            {
                UserId = 3,
                EventId = 1,
                ClientIP = "192.168.1.2",
                LastSeen = DateTime.UtcNow.AddMinutes(-5), // Active
                RequestCount = 0
            }
        };
        _context.Sessions.AddRange(inactiveSessions);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetEventSummary(1);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        dynamic? summary = okResult?.Value;
        summary?.ActiveUsers.Should().Be(1); // Only one active user
    }

    /// <summary>
    /// Tests that top requests are ordered by vote count descending.
    /// </summary>
    [Fact]
    public async Task GetTopRequests_OrderedByVoteCount_ReturnsCorrectOrder()
    {
        // Arrange
        await SeedDashboardTestDataAsync();
        SetupUserClaims(1, "DJ");

        // Add votes to create different vote counts
        var votes = new[]
        {
            new Vote { UserId = 2, RequestId = 1, CreatedAt = DateTime.UtcNow }, // Request 1: 1 vote
            new Vote { UserId = 3, RequestId = 2, CreatedAt = DateTime.UtcNow }, // Request 2: 2 votes  
            new Vote { UserId = 2, RequestId = 2, CreatedAt = DateTime.UtcNow }  // Request 2: 2nd vote
        };
        _context.Votes.AddRange(votes);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetTopRequests(1, RequestStatus.Pending, 10);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var requests = okResult?.Value as IList<object>;
        requests.Should().NotBeNull();
        requests!.Count.Should().Be(2);

        // First request should have more votes (Request 2 with 2 votes)
        dynamic? firstRequest = requests[0];
        firstRequest?.VoteCount.Should().Be(2);
    }

    /// <summary>
    /// Tests that DJ event stats include correct request counts.
    /// </summary>
    [Fact]
    public async Task GetDJEventStats_WithRequests_ReturnsCorrectCounts()
    {
        // Arrange
        await SeedDashboardTestDataAsync();
        SetupUserClaims(1, "DJ");

        // Act
        var result = await _controller.GetDJEventStats(1);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var stats = okResult?.Value as IList<object>;
        stats.Should().NotBeNull();
        stats!.Count.Should().Be(1);

        dynamic? eventStat = stats[0];
        eventStat?.RequestCounts.Total.Should().Be(3);
        eventStat?.RequestCounts.Pending.Should().Be(2);
        eventStat?.RequestCounts.Approved.Should().Be(1);
        eventStat?.RequestCounts.Rejected.Should().Be(0);
    }

    /// <summary>
    /// Tests getting event summary with no requests.
    /// </summary>
    [Fact]
    public async Task GetEventSummary_EventWithNoRequests_ReturnsZeroCounts()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);

        // Remove all requests to test empty event
        var requests = await _context.Requests.ToListAsync();
        _context.Requests.RemoveRange(requests);
        await _context.SaveChangesAsync();

        SetupUserClaims(1, "DJ");

        // Act
        var result = await _controller.GetEventSummary(1);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        dynamic? summary = okResult?.Value;
        summary?.TotalRequests.Should().Be(0);
        summary?.PendingRequests.Should().Be(0);
        summary?.ApprovedRequests.Should().Be(0);
        summary?.RejectedRequests.Should().Be(0);
        summary?.TotalVotes.Should().Be(0);
    }

    /// <summary>
    /// Tests getting DJ event stats with multiple events.
    /// </summary>
    [Fact]
    public async Task GetDJEventStats_MultipleEvents_ReturnsAllEvents()
    {
        // Arrange
        await SeedDashboardTestDataAsync();

        // Add second event for same DJ
        var secondEvent = new Event
        {
            DjUserId = 1,
            Name = "Second Test Event",
            Slug = "second-test-event",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Events.Add(secondEvent);
        await _context.SaveChangesAsync();

        SetupUserClaims(1, "DJ");

        // Act
        var result = await _controller.GetDJEventStats(1);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var stats = okResult?.Value as IEnumerable<object>;
        stats.Should().NotBeNull();
        stats!.Count().Should().Be(2); // Two events for this DJ
    }

    /// <summary>
    /// Seeds the database with test data for dashboard tests.
    /// </summary>
    private async Task SeedDashboardTestDataAsync()
    {
        await TestDbContextFactory.SeedTestDataAsync(_context);

        // Add additional request with different status
        var approvedRequest = new Request
        {
            UserId = 2,
            EventId = 1,
            SongName = "Approved Song",
            ArtistName = "Approved Artist",
            Status = RequestStatus.Approved,
            CreatedAt = DateTime.UtcNow
        };
        _context.Requests.Add(approvedRequest);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Sets up user claims for the controller context.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="role">The user role.</param>
    private void SetupUserClaims(int userId, string role)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role)
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
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}