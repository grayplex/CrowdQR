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
/// Unit tests for the RequestController.
/// </summary>
public class RequestControllerTests : IDisposable
{
    private readonly CrowdQRContext _context;
    private readonly Mock<IHubNotificationService> _mockHubService;
    private readonly RequestController _controller;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestControllerTests"/> class.
    /// </summary>
    public RequestControllerTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _mockHubService = new Mock<IHubNotificationService>();

        var logger = TestLoggerFactory.CreateNullLogger<RequestController>();
        _controller = new RequestController(_context, logger, _mockHubService.Object);
    }

    /// <summary>
    /// Tests that CreateRequest successfully creates a valid request.
    /// </summary>
    [Fact]
    public async Task CreateRequest_ValidRequest_ReturnsCreatedResult()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(2, "Audience");

        var requestDto = new RequestCreateDto
        {
            UserId = 2,
            EventId = 1,
            SongName = "New Test Song",
            ArtistName = "New Test Artist"
        };

        // Act
        var result = await _controller.CreateRequest(requestDto);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result.Result as CreatedAtActionResult;
        createdResult?.Value.Should().BeOfType<Request>();

        // Verify request was saved to database
        var savedRequest = await _context.Requests
            .FirstOrDefaultAsync(r => r.SongName == "New Test Song");
        savedRequest.Should().NotBeNull();
        savedRequest!.UserId.Should().Be(2);
        savedRequest.EventId.Should().Be(1);
        savedRequest.Status.Should().Be(RequestStatus.Pending);

        // Verify SignalR notification was sent
        _mockHubService.Verify(
            x => x.NotifyRequestAdded(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that CreateRequest validates the event exists.
    /// </summary>
    [Fact]
    public async Task CreateRequest_NonExistentEvent_ReturnsBadRequest()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(2, "Audience");

        var requestDto = new RequestCreateDto
        {
            UserId = 2,
            EventId = 999, // Non-existent event
            SongName = "Test Song",
            ArtistName = "Test Artist"
        };

        // Act
        var result = await _controller.CreateRequest(requestDto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result.Result as BadRequestObjectResult;
        badRequestResult?.Value.Should().Be("Event does not exist");
    }

    /// <summary>
    /// Tests that CreateRequest validates the user exists.
    /// </summary>
    [Fact]
    public async Task CreateRequest_NonExistentUser_ReturnsBadRequest()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(999, "Audience");

        var requestDto = new RequestCreateDto
        {
            UserId = 999, // Non-existent user
            EventId = 1,
            SongName = "Test Song",
            ArtistName = "Test Artist"
        };

        // Act
        var result = await _controller.CreateRequest(requestDto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result.Result as BadRequestObjectResult;
        badRequestResult?.Value.Should().Be("User does not exist");
    }

    /// <summary>
    /// Tests that users can only create requests for themselves (unless they're a DJ).
    /// </summary>
    [Fact]
    public async Task CreateRequest_UserCreatingForAnotherUser_ReturnsForbid()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(2, "Audience"); // User ID 2 trying to create request as User ID 3

        var requestDto = new RequestCreateDto
        {
            UserId = 3, // Different user
            EventId = 1,
            SongName = "Test Song",
            ArtistName = "Test Artist"
        };

        // Act
        var result = await _controller.CreateRequest(requestDto);

        // Assert
        result.Result.Should().BeOfType<ForbidResult>();
    }

    /// <summary>
    /// Tests that DJs can create requests for any user.
    /// </summary>
    [Fact]
    public async Task CreateRequest_DjCreatingForAnotherUser_Succeeds()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(1, "DJ"); // DJ user

        var requestDto = new RequestCreateDto
        {
            UserId = 2, // Different user
            EventId = 1,
            SongName = "DJ Test Song",
            ArtistName = "DJ Test Artist"
        };

        // Act
        var result = await _controller.CreateRequest(requestDto);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
    }

    /// <summary>
    /// Tests that CreateRequest validates required fields.
    /// </summary>
    [Fact]
    public async Task CreateRequest_MissingSongName_ReturnsBadRequest()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(2, "Audience");

        _controller.ModelState.AddModelError("SongName", "SongName is required");

        var requestDto = new RequestCreateDto
        {
            UserId = 2,
            EventId = 1,
            SongName = "", // Empty song name
            ArtistName = "Test Artist"
        };

        // Act
        var result = await _controller.CreateRequest(requestDto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Tests that GetRequestsByEvent returns requests for a specific event.
    /// </summary>
    [Fact]
    public async Task GetRequestsByEvent_ValidEvent_ReturnsRequests()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);

        // Act
        var result = await _controller.GetRequestsByEvent(1);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var returnedRequests = okResult?.Value as IEnumerable<object>;
        returnedRequests.Should().NotBeNull();
        returnedRequests!.Count().Should().Be(2); // From seeded data
    }

    /// <summary>
    /// Tests that GetRequest returns a specific request with vote information.
    /// </summary>
    [Fact]
    public async Task GetRequest_ValidId_ReturnsRequestWithVotes()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);

        // Add a vote to the first request
        var vote = new Vote
        {
            UserId = 2,
            RequestId = 1,
            CreatedAt = DateTime.UtcNow
        };
        _context.Votes.Add(vote);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetRequest(1);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().NotBeNull();

        // Verify the response contains expected properties
        var response = okResult.Value;
        response.Should().NotBeNull();

        // Verify the response structure
        var responseType = response!.GetType();
        responseType.GetProperty("RequestId").Should().NotBeNull();
        responseType.GetProperty("VoteCount").Should().NotBeNull();
        responseType.GetProperty("Votes").Should().NotBeNull();
    }

    /// <summary>
    /// Tests that GetRequest returns NotFound for non-existent request.
    /// </summary>
    [Fact]
    public async Task GetRequest_NonExistentId_ReturnsNotFound()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);

        // Act
        var result = await _controller.GetRequest(999);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    /// <summary>
    /// Tests that UpdateRequestStatus successfully updates request status for DJs.
    /// </summary>
    [Fact]
    public async Task UpdateRequestStatus_ValidDjRequest_ReturnsNoContent()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(1, "DJ"); // DJ user

        var statusDto = new RequestStatusUpdateDto
        {
            Status = RequestStatus.Approved
        };

        // Act
        var result = await _controller.UpdateRequestStatus(1, statusDto);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        // Verify status was updated
        var updatedRequest = await _context.Requests.FindAsync(1);
        updatedRequest.Should().NotBeNull();
        updatedRequest!.Status.Should().Be(RequestStatus.Approved);

        // Verify SignalR notification was sent
        _mockHubService.Verify(
            x => x.NotifyRequestStatusUpdated(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that UpdateRequestStatus returns NotFound for non-existent request.
    /// </summary>
    [Fact]
    public async Task UpdateRequestStatus_NonExistentRequest_ReturnsNotFound()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(1, "DJ");

        var statusDto = new RequestStatusUpdateDto
        {
            Status = RequestStatus.Approved
        };

        // Act
        var result = await _controller.UpdateRequestStatus(999, statusDto);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    /// <summary>
    /// Tests that DeleteRequest successfully removes a request for DJs.
    /// </summary>
    [Fact]
    public async Task DeleteRequest_ValidDjRequest_ReturnsNoContent()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(1, "DJ");

        // Act
        var result = await _controller.DeleteRequest(1);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        // Verify request was deleted
        var deletedRequest = await _context.Requests.FindAsync(1);
        deletedRequest.Should().BeNull();
    }

    /// <summary>
    /// Tests that DeleteRequest returns NotFound for non-existent request.
    /// </summary>
    [Fact]
    public async Task DeleteRequest_NonExistentRequest_ReturnsNotFound()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(1, "DJ");

        // Act
        var result = await _controller.DeleteRequest(999);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    /// <summary>
    /// Tests input validation for song name length.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task CreateRequest_InvalidSongName_ReturnsBadRequest(string? songName)
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(2, "Audience");

        // Add model state error for invalid input
        _controller.ModelState.AddModelError("SongName", "SongName is required");

        var requestDto = new RequestCreateDto
        {
            UserId = 2,
            EventId = 1,
            SongName = songName ?? "",
            ArtistName = "Test Artist"
        };

        // Act
        var result = await _controller.CreateRequest(requestDto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Tests that single character song names are valid (remove from invalid test).
    /// </summary>
    [Fact]
    public async Task CreateRequest_SingleCharacterSongName_Succeeds()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(2, "Audience");

        var requestDto = new RequestCreateDto
        {
            UserId = 2,
            EventId = 1,
            SongName = "A", // Single character is valid
            ArtistName = "Test Artist"
        };

        // Act
        var result = await _controller.CreateRequest(requestDto);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
    }

    /// <summary>
    /// Tests that very long song names are handled appropriately.
    /// </summary>
    [Fact]
    public async Task CreateRequest_TooLongSongName_ReturnsBadRequest()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(2, "Audience");

        var longSongName = new string('A', 256); // Max length is 255
        _controller.ModelState.AddModelError("SongName", "SongName is too long");

        var requestDto = new RequestCreateDto
        {
            UserId = 2,
            EventId = 1,
            SongName = longSongName,
            ArtistName = "Test Artist"
        };

        // Act
        var result = await _controller.CreateRequest(requestDto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Tests that requests are created with proper default values.
    /// </summary>
    [Fact]
    public async Task CreateRequest_ValidInput_SetsCorrectDefaults()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(2, "Audience");

        var requestDto = new RequestCreateDto
        {
            UserId = 2,
            EventId = 1,
            SongName = "Test Song",
            ArtistName = "Test Artist"
        };

        var beforeCreation = DateTime.UtcNow;

        // Act
        var result = await _controller.CreateRequest(requestDto);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();

        var savedRequest = await _context.Requests
            .FirstOrDefaultAsync(r => r.SongName == "Test Song");
        savedRequest.Should().NotBeNull();
        savedRequest!.Status.Should().Be(RequestStatus.Pending);
        savedRequest.CreatedAt.Should().BeAfter(beforeCreation);
        savedRequest.CreatedAt.Should().BeOnOrBefore(DateTime.UtcNow);
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