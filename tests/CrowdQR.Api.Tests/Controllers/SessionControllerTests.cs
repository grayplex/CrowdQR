using System.Security.Claims;
using CrowdQR.Api.Controllers;
using CrowdQR.Api.Data;
using CrowdQR.Api.Models;
using CrowdQR.Api.Services;
using CrowdQR.Api.Tests.Helpers;
using CrowdQR.Shared.Models.DTOs;
using CrowdQR.Shared.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrowdQR.Api.Tests.Controllers;

/// <summary>
/// Unit tests for the SessionController.
/// </summary>
public class SessionControllerTests : IDisposable
{
    private readonly CrowdQRContext _context;
    private readonly Mock<IHubNotificationService> _mockHubService;
    private readonly SessionController _controller;

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionControllerTests"/> class.
    /// </summary>
    public SessionControllerTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _mockHubService = new Mock<IHubNotificationService>();
        var logger = TestLoggerFactory.CreateNullLogger<SessionController>();
        _controller = new SessionController(_context, logger, _mockHubService.Object);
    }

    /// <summary>
    /// Tests getting all sessions as a DJ.
    /// </summary>
    [Fact]
    public async Task GetSessions_AsDj_ReturnsAllSessions()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(1, "DJ");

        // Add test sessions
        var sessions = new[]
        {
            new Session { UserId = 2, EventId = 1, ClientIP = "192.168.1.1", LastSeen = DateTime.UtcNow, RequestCount = 0 },
            new Session { UserId = 3, EventId = 1, ClientIP = "192.168.1.2", LastSeen = DateTime.UtcNow, RequestCount = 1 }
        };
        _context.Sessions.AddRange(sessions);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetSessions();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var returnedSessions = okResult?.Value as IEnumerable<object>;
        returnedSessions.Should().NotBeNull();
        returnedSessions!.Count().Should().Be(2);
    }

    /// <summary>
    /// Tests getting a specific session by ID.
    /// </summary>
    [Fact]
    public async Task GetSession_ValidId_ReturnsSession()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(1, "DJ");

        var session = new Session
        {
            UserId = 2,
            EventId = 1,
            ClientIP = "192.168.1.1",
            LastSeen = DateTime.UtcNow,
            RequestCount = 0
        };
        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetSession(session.SessionId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult?.Value.Should().NotBeNull();
    }

    /// <summary>
    /// Tests getting non-existent session.
    /// </summary>
    [Fact]
    public async Task GetSession_NonExistentId_ReturnsNotFound()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(1, "DJ");

        // Act
        var result = await _controller.GetSession(999);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    /// <summary>
    /// Tests that users can only access their own sessions.
    /// </summary>
    [Fact]
    public async Task GetSession_UserAccessingOtherSession_ReturnsForbid()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(2, "Audience"); // User ID 2

        var session = new Session
        {
            UserId = 3, // Different user's session
            EventId = 1,
            ClientIP = "192.168.1.1",
            LastSeen = DateTime.UtcNow,
            RequestCount = 0
        };
        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetSession(session.SessionId);

        // Assert
        result.Result.Should().BeOfType<ForbidResult>();
    }

    /// <summary>
    /// Tests getting sessions by event ID.
    /// </summary>
    [Fact]
    public async Task GetSessionsByEvent_ValidEventId_ReturnsSessions()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(1, "DJ"); // DJ for event 1

        var sessions = new[]
        {
            new Session { UserId = 2, EventId = 1, ClientIP = "192.168.1.1", LastSeen = DateTime.UtcNow, RequestCount = 0 },
            new Session { UserId = 3, EventId = 1, ClientIP = "192.168.1.2", LastSeen = DateTime.UtcNow, RequestCount = 1 }
        };
        _context.Sessions.AddRange(sessions);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetSessionsByEvent(1);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var returnedSessions = okResult?.Value as IEnumerable<object>;
        returnedSessions.Should().NotBeNull();
        returnedSessions!.Count().Should().Be(2);
    }

    /// <summary>
    /// Tests getting sessions for non-existent event.
    /// </summary>
    [Fact]
    public async Task GetSessionsByEvent_NonExistentEvent_ReturnsNotFound()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(1, "DJ");

        // Act
        var result = await _controller.GetSessionsByEvent(999);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    /// <summary>
    /// Tests that non-DJs cannot access event sessions unless they are the event DJ.
    /// </summary>
    [Fact]
    public async Task GetSessionsByEvent_UnauthorizedUser_ReturnsForbid()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(2, "Audience"); // Not the DJ for event 1

        // Act
        var result = await _controller.GetSessionsByEvent(1);

        // Assert
        result.Result.Should().BeOfType<ForbidResult>();
    }

    /// <summary>
    /// Tests getting sessions by user ID.
    /// </summary>
    [Fact]
    public async Task GetSessionsByUser_ValidUserId_ReturnsSessions()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(2, "Audience"); // User accessing their own sessions

        var session = new Session
        {
            UserId = 2,
            EventId = 1,
            ClientIP = "192.168.1.1",
            LastSeen = DateTime.UtcNow,
            RequestCount = 0
        };
        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetSessionsByUser(2);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var returnedSessions = okResult?.Value as IEnumerable<object>;
        returnedSessions.Should().NotBeNull();
        returnedSessions!.Count().Should().Be(1);
    }

    /// <summary>
    /// Tests that users cannot access other users' sessions.
    /// </summary>
    [Fact]
    public async Task GetSessionsByUser_UnauthorizedUser_ReturnsForbid()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(2, "Audience"); // User ID 2 trying to access User ID 3's sessions

        // Act
        var result = await _controller.GetSessionsByUser(3);

        // Assert
        result.Result.Should().BeOfType<ForbidResult>();
    }

    /// <summary>
    /// Tests creating a new session.
    /// </summary>
    [Fact]
    public async Task CreateOrUpdateSession_NewSession_ReturnsCreated()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(2, "Audience");

        var sessionDto = new SessionCreateDto
        {
            UserId = 2,
            EventId = 1,
            ClientIP = "192.168.1.1"
        };

        // Act
        var result = await _controller.CreateOrUpdateSession(sessionDto);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();

        // Verify session was created
        var createdSession = await _context.Sessions
            .FirstOrDefaultAsync(s => s.UserId == 2 && s.EventId == 1);
        createdSession.Should().NotBeNull();
        createdSession!.ClientIP.Should().Be("192.168.1.1");

        // Verify SignalR notification was sent
        _mockHubService.Verify(
            x => x.NotifyUserJoinedEvent(1, It.IsAny<string>()),
            Times.Once);
    }

    /// <summary>
    /// Tests updating an existing session.
    /// </summary>
    [Fact]
    public async Task CreateOrUpdateSession_ExistingSession_ReturnsOk()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(2, "Audience");

        // Create existing session
        var existingSession = new Session
        {
            UserId = 2,
            EventId = 1,
            ClientIP = "192.168.1.1",
            LastSeen = DateTime.UtcNow.AddMinutes(-5),
            RequestCount = 1
        };
        _context.Sessions.Add(existingSession);
        await _context.SaveChangesAsync();

        var oldLastSeen = existingSession.LastSeen;
        await Task.Delay(10);

        var sessionDto = new SessionCreateDto
        {
            UserId = 2,
            EventId = 1,
            ClientIP = "192.168.1.2" // Different IP
        };

        // Act
        var result = await _controller.CreateOrUpdateSession(sessionDto);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();

        // Verify session was updated
        var updatedSession = await _context.Sessions
            .FirstOrDefaultAsync(s => s.UserId == 2 && s.EventId == 1);
        updatedSession.Should().NotBeNull();
        updatedSession!.ClientIP.Should().Be("192.168.1.2");
        updatedSession.LastSeen.Should().BeOnOrAfter(oldLastSeen);

        // Verify no SignalR notification for existing session
        _mockHubService.Verify(
            x => x.NotifyUserJoinedEvent(It.IsAny<int>(), It.IsAny<string>()),
            Times.Never);
    }

    /// <summary>
    /// Tests that users can only create sessions for themselves.
    /// </summary>
    [Fact]
    public async Task CreateOrUpdateSession_UserCreatingForAnother_ReturnsForbid()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(2, "Audience"); // User ID 2 trying to create session for User ID 3

        var sessionDto = new SessionCreateDto
        {
            UserId = 3, // Different user
            EventId = 1,
            ClientIP = "192.168.1.1"
        };

        // Act
        var result = await _controller.CreateOrUpdateSession(sessionDto);

        // Assert
        result.Result.Should().BeOfType<ForbidResult>();
    }

    /// <summary>
    /// Tests creating session for non-existent user.
    /// </summary>
    [Fact]
    public async Task CreateOrUpdateSession_NonExistentUser_ReturnsBadRequest()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(999, "Audience");

        var sessionDto = new SessionCreateDto
        {
            UserId = 999, // Non-existent user
            EventId = 1,
            ClientIP = "192.168.1.1"
        };

        // Act
        var result = await _controller.CreateOrUpdateSession(sessionDto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Tests creating session for non-existent event.
    /// </summary>
    [Fact]
    public async Task CreateOrUpdateSession_NonExistentEvent_ReturnsBadRequest()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(2, "Audience");

        var sessionDto = new SessionCreateDto
        {
            UserId = 2,
            EventId = 999, // Non-existent event
            ClientIP = "192.168.1.1"
        };

        // Act
        var result = await _controller.CreateOrUpdateSession(sessionDto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Tests incrementing request count.
    /// </summary>
    [Fact]
    public async Task IncrementRequestCount_ValidSession_ReturnsNoContent()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(2, "Audience");

        var session = new Session
        {
            UserId = 2,
            EventId = 1,
            ClientIP = "192.168.1.1",
            LastSeen = DateTime.UtcNow,
            RequestCount = 0
        };
        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.IncrementRequestCount(session.SessionId);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        // Verify request count was incremented
        var updatedSession = await _context.Sessions.FindAsync(session.SessionId);
        updatedSession!.RequestCount.Should().Be(1);
    }

    /// <summary>
    /// Tests refreshing a session.
    /// </summary>
    [Fact]
    public async Task RefreshSession_ValidSession_ReturnsNoContent()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(2, "Audience");

        var session = new Session
        {
            UserId = 2,
            EventId = 1,
            ClientIP = "192.168.1.1",
            LastSeen = DateTime.UtcNow.AddMinutes(-5),
            RequestCount = 1
        };
        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        var oldLastSeen = session.LastSeen;

        // Act
        var result = await _controller.RefreshSession(session.SessionId);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        // Verify LastSeen was updated but RequestCount wasn't
        var updatedSession = await _context.Sessions.FindAsync(session.SessionId);
        updatedSession!.LastSeen.Should().BeAfter(oldLastSeen);
        updatedSession.RequestCount.Should().Be(1); // Should remain unchanged
    }

    /// <summary>
    /// Tests deleting a session as DJ.
    /// </summary>
    [Fact]
    public async Task DeleteSession_AsDj_ReturnsNoContent()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(1, "DJ");

        var session = new Session
        {
            UserId = 2,
            EventId = 1,
            ClientIP = "192.168.1.1",
            LastSeen = DateTime.UtcNow,
            RequestCount = 0
        };
        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.DeleteSession(session.SessionId);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        // Verify session was deleted
        var deletedSession = await _context.Sessions.FindAsync(session.SessionId);
        deletedSession.Should().BeNull();
    }

    /// <summary>
    /// Tests model validation for session creation.
    /// </summary>
    [Fact]
    public async Task CreateOrUpdateSession_InvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(2, "Audience");

        _controller.ModelState.AddModelError("UserId", "UserId is required");

        var sessionDto = new SessionCreateDto
        {
            UserId = 2,
            EventId = 1,
            ClientIP = "192.168.1.1"
        };

        // Act
        var result = await _controller.CreateOrUpdateSession(sessionDto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
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