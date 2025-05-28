using System.Security.Claims;
using CrowdQR.Api.Controllers;
using CrowdQR.Api.Data;
using CrowdQR.Api.Models;
using CrowdQR.Api.Tests.Helpers;
using CrowdQR.Shared.Models.DTOs;
using CrowdQR.Shared.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrowdQR.Api.Tests.Controllers;

/// <summary>
/// Unit tests for the EventController.
/// </summary>
public class EventControllerTests : IDisposable
{
    private readonly CrowdQRContext _context;
    private readonly EventController _controller;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventControllerTests"/> class.
    /// </summary>
    public EventControllerTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        var logger = TestLoggerFactory.CreateNullLogger<EventController>();
        _controller = new EventController(_context, logger);
    }

    /// <summary>
    /// Tests getting all events.
    /// </summary>
    [Fact]
    public async Task GetEvents_ReturnsAllEvents()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);

        // Act
        var result = await _controller.GetEvents();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var events = okResult?.Value as IEnumerable<object>;
        events.Should().NotBeNull();
        events!.Count().Should().Be(1); // From seeded data
    }

    /// <summary>
    /// Tests getting a specific event by ID.
    /// </summary>
    [Fact]
    public async Task GetEvent_ValidId_ReturnsEvent()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);

        // Act
        var result = await _controller.GetEvent(1);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult?.Value.Should().NotBeNull();
    }

    /// <summary>
    /// Tests getting a non-existent event.
    /// </summary>
    [Fact]
    public async Task GetEvent_NonExistentId_ReturnsNotFound()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);

        // Act
        var result = await _controller.GetEvent(999);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    /// <summary>
    /// Tests getting an event by slug.
    /// </summary>
    [Fact]
    public async Task GetEventBySlug_ValidSlug_ReturnsEvent()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);

        // Act
        var result = await _controller.GetEventBySlug("test-event");

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult?.Value.Should().NotBeNull();
    }

    /// <summary>
    /// Tests getting an event by non-existent slug.
    /// </summary>
    [Fact]
    public async Task GetEventBySlug_NonExistentSlug_ReturnsNotFound()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);

        // Act
        var result = await _controller.GetEventBySlug("non-existent-slug");

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    /// <summary>
    /// Tests getting events by DJ user ID.
    /// </summary>
    [Fact]
    public async Task GetEventsByDJ_ValidDjId_ReturnsEvents()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(1, "DJ"); // Same DJ user

        // Act
        var result = await _controller.GetEventsByDJ(1);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var events = okResult?.Value as IEnumerable<object>;
        events.Should().NotBeNull();
        events!.Count().Should().Be(1);
    }

    /// <summary>
    /// Tests that non-DJ users can't access other DJ's events.
    /// </summary>
    [Fact]
    public async Task GetEventsByDJ_UnauthorizedUser_ReturnsForbid()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(2, "Audience"); // Different user, not DJ

        // Act
        var result = await _controller.GetEventsByDJ(1);

        // Assert
        result.Result.Should().BeOfType<ForbidResult>();
    }

    /// <summary>
    /// Tests successful event creation.
    /// </summary>
    [Fact]
    public async Task CreateEvent_ValidData_ReturnsCreated()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(1, "DJ");

        var eventDto = new EventCreateDto
        {
            DjUserId = 1,
            Name = "New Test Event",
            Slug = "new-test-event"
        };

        // Act
        var result = await _controller.CreateEvent(eventDto);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result.Result as CreatedAtActionResult;
        createdResult?.Value.Should().BeOfType<Event>();

        // Verify event was saved
        var savedEvent = await _context.Events
            .FirstOrDefaultAsync(e => e.Slug == "new-test-event");
        savedEvent.Should().NotBeNull();
        savedEvent!.Name.Should().Be("New Test Event");
    }

    /// <summary>
    /// Tests event creation with duplicate slug.
    /// </summary>
    [Fact]
    public async Task CreateEvent_DuplicateSlug_ReturnsConflict()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(1, "DJ");

        var eventDto = new EventCreateDto
        {
            DjUserId = 1,
            Name = "Another Event",
            Slug = "test-event" // Already exists in seeded data
        };

        // Act
        var result = await _controller.CreateEvent(eventDto);

        // Assert
        result.Result.Should().BeOfType<ConflictObjectResult>();
    }

    /// <summary>
    /// Tests that DJs can only create events for themselves.
    /// </summary>
    [Fact]
    public async Task CreateEvent_DjCreatingForAnother_ReturnsBadRequest()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(1, "DJ"); // User ID 1 trying to create for User ID 2

        var eventDto = new EventCreateDto
        {
            DjUserId = 2, // Different DJ
            Name = "Unauthorized Event",
            Slug = "unauthorized-event"
        };

        // Act
        var result = await _controller.CreateEvent(eventDto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Tests successful event update.
    /// </summary>
    [Fact]
    public async Task UpdateEvent_ValidData_ReturnsNoContent()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(1, "DJ");

        var updateDto = new EventUpdateDto
        {
            Name = "Updated Event Name",
            IsActive = false
        };

        // Act
        var result = await _controller.UpdateEvent(1, updateDto);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        // Verify event was updated
        var updatedEvent = await _context.Events.FindAsync(1);
        updatedEvent!.Name.Should().Be("Updated Event Name");
        updatedEvent.IsActive.Should().BeFalse();
    }

    /// <summary>
    /// Tests updating non-existent event.
    /// </summary>
    [Fact]
    public async Task UpdateEvent_NonExistentEvent_ReturnsNotFound()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(1, "DJ");

        var updateDto = new EventUpdateDto
        {
            Name = "Updated Name"
        };

        // Act
        var result = await _controller.UpdateEvent(999, updateDto);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    /// <summary>
    /// Tests that DJs can only update their own events.
    /// </summary>
    [Fact]
    public async Task UpdateEvent_UnauthorizedDj_ReturnsForbid()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);

        // Add another DJ user
        var anotherDj = new User
        {
            Username = "another_dj",
            Role = UserRole.DJ,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(anotherDj);
        await _context.SaveChangesAsync();

        SetupUserClaims(anotherDj.UserId, "DJ"); // Different DJ

        var updateDto = new EventUpdateDto
        {
            Name = "Unauthorized Update"
        };

        // Act
        var result = await _controller.UpdateEvent(1, updateDto);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    /// <summary>
    /// Tests successful event deletion.
    /// </summary>
    [Fact]
    public async Task DeleteEvent_ValidRequest_ReturnsNoContent()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(1, "DJ");

        // Act
        var result = await _controller.DeleteEvent(1);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        // Verify event was deleted
        var deletedEvent = await _context.Events.FindAsync(1);
        deletedEvent.Should().BeNull();
    }

    /// <summary>
    /// Tests deleting non-existent event.
    /// </summary>
    [Fact]
    public async Task DeleteEvent_NonExistentEvent_ReturnsNotFound()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(1, "DJ");

        // Act
        var result = await _controller.DeleteEvent(999);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    /// <summary>
    /// Tests model validation for event creation.
    /// </summary>
    [Fact]
    public async Task CreateEvent_InvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(1, "DJ");

        _controller.ModelState.AddModelError("Name", "Name is required");

        var eventDto = new EventCreateDto
        {
            DjUserId = 1,
            Name = "", // Invalid
            Slug = "valid-slug"
        };

        // Act
        var result = await _controller.CreateEvent(eventDto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Tests slug validation during update.
    /// </summary>
    [Fact]
    public async Task UpdateEvent_DuplicateSlug_ReturnsConflict()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(1, "DJ");

        // Create another event
        var anotherEvent = new Event
        {
            DjUserId = 1,
            Name = "Another Event",
            Slug = "another-event",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Events.Add(anotherEvent);
        await _context.SaveChangesAsync();

        var updateDto = new EventUpdateDto
        {
            Slug = "another-event" // Trying to use existing slug
        };

        // Act
        var result = await _controller.UpdateEvent(1, updateDto);

        // Assert
        result.Should().BeOfType<ConflictObjectResult>();
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