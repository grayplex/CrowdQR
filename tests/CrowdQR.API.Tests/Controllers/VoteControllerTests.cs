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
/// Unit tests for the VoteController.
/// </summary>
public class VoteControllerTests : IDisposable
{
    private readonly CrowdQRContext _context;
    private readonly Mock<IHubNotificationService> _mockHubService;
    private readonly VoteController _controller;

    /// <summary>
    /// Initializes a new instance of the <see cref="VoteControllerTests"/> class.
    /// </summary>
    public VoteControllerTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _mockHubService = new Mock<IHubNotificationService>();

        var logger = TestLoggerFactory.CreateNullLogger<VoteController>();
        _controller = new VoteController(_context, logger, _mockHubService.Object);
    }

    /// <summary>
    /// Tests that CreateVote successfully creates a vote for a valid request.
    /// </summary>
    [Fact]
    public async Task CreateVote_ValidRequest_ReturnsCreatedResult()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(2, "Audience"); // User ID 2, Audience role

        var voteDto = new VoteCreateDto
        {
            UserId = 2,
            RequestId = 1
        };

        // Act
        var result = await _controller.CreateVote(voteDto);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result.Result as CreatedAtActionResult;
        createdResult?.Value.Should().BeOfType<Vote>();

        // Verify vote was saved to database
        var savedVote = await _context.Votes.FirstOrDefaultAsync(v => v.UserId == 2 && v.RequestId == 1);
        savedVote.Should().NotBeNull();
        savedVote!.UserId.Should().Be(2);
        savedVote.RequestId.Should().Be(1);

        // Verify SignalR notification was sent
        _mockHubService.Verify(
            x => x.NotifyVoteAdded(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that CreateVote prevents duplicate votes from the same user.
    /// </summary>
    [Fact]
    public async Task CreateVote_DuplicateVote_ReturnsConflict()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(2, "Audience");

        // Create initial vote
        var existingVote = new Vote
        {
            UserId = 2,
            RequestId = 1,
            CreatedAt = DateTime.UtcNow
        };
        _context.Votes.Add(existingVote);
        await _context.SaveChangesAsync();

        var voteDto = new VoteCreateDto
        {
            UserId = 2,
            RequestId = 1
        };

        // Act
        var result = await _controller.CreateVote(voteDto);

        // Assert
        result.Result.Should().BeOfType<ConflictObjectResult>();
        var conflictResult = result.Result as ConflictObjectResult;
        conflictResult?.Value.Should().Be("User has already voted for this request");

        // Verify only one vote exists
        var voteCount = await _context.Votes.CountAsync(v => v.UserId == 2 && v.RequestId == 1);
        voteCount.Should().Be(1);
    }

    /// <summary>
    /// Tests that CreateVote validates that the request exists.
    /// </summary>
    [Fact]
    public async Task CreateVote_NonExistentRequest_ReturnsBadRequest()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(2, "Audience");

        var voteDto = new VoteCreateDto
        {
            UserId = 2,
            RequestId = 999 // Non-existent request
        };

        // Act
        var result = await _controller.CreateVote(voteDto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result.Result as BadRequestObjectResult;
        badRequestResult?.Value.Should().Be("Request does not exist");
    }

    /// <summary>
    /// Tests that CreateVote validates that the user exists.
    /// </summary>
    [Fact]
    public async Task CreateVote_NonExistentUser_ReturnsBadRequest()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(999, "Audience"); // Non-existent user

        var voteDto = new VoteCreateDto
        {
            UserId = 999,
            RequestId = 1
        };

        // Act
        var result = await _controller.CreateVote(voteDto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result.Result as BadRequestObjectResult;
        badRequestResult?.Value.Should().Be("User does not exist");
    }

    /// <summary>
    /// Tests that users can only create votes for themselves (unless they're a DJ).
    /// </summary>
    [Fact]
    public async Task CreateVote_UserVotingForAnotherUser_ReturnsForbid()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(2, "Audience"); // User ID 2 trying to vote as User ID 3

        var voteDto = new VoteCreateDto
        {
            UserId = 3, // Different user
            RequestId = 1
        };

        // Act
        var result = await _controller.CreateVote(voteDto);

        // Assert
        result.Result.Should().BeOfType<ForbidResult>();
    }

    /// <summary>
    /// Tests that DJs can create votes for any user.
    /// </summary>
    [Fact]
    public async Task CreateVote_DjVotingForAnotherUser_Succeeds()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(1, "DJ"); // DJ user

        var voteDto = new VoteCreateDto
        {
            UserId = 2, // Different user
            RequestId = 1
        };

        // Act
        var result = await _controller.CreateVote(voteDto);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
    }

    /// <summary>
    /// Tests that GetVotesByRequest returns votes for a specific request.
    /// </summary>
    [Fact]
    public async Task GetVotesByRequest_ValidRequest_ReturnsVotes()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(2, "Audience");

        // Add votes
        var votes = new[]
        {
            new Vote { UserId = 2, RequestId = 1, CreatedAt = DateTime.UtcNow },
            new Vote { UserId = 3, RequestId = 1, CreatedAt = DateTime.UtcNow }
        };
        _context.Votes.AddRange(votes);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetVotesByRequest(1);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var returnedVotes = okResult?.Value as IEnumerable<object>;
        returnedVotes.Should().NotBeNull();
        returnedVotes!.Count().Should().Be(2);
    }

    /// <summary>
    /// Tests that DeleteVoteByUserAndRequest successfully removes a vote.
    /// </summary>
    [Fact]
    public async Task DeleteVoteByUserAndRequest_ValidVote_ReturnsNoContent()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(2, "Audience");

        var vote = new Vote
        {
            UserId = 2,
            RequestId = 1,
            CreatedAt = DateTime.UtcNow
        };
        _context.Votes.Add(vote);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.DeleteVoteByUserAndRequest(2, 1);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        // Verify vote was deleted
        var deletedVote = await _context.Votes.FirstOrDefaultAsync(v => v.UserId == 2 && v.RequestId == 1);
        deletedVote.Should().BeNull();
    }

    /// <summary>
    /// Tests that DeleteVoteByUserAndRequest returns NotFound for non-existent vote.
    /// </summary>
    [Fact]
    public async Task DeleteVoteByUserAndRequest_NonExistentVote_ReturnsNotFound()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(2, "Audience");

        // Act
        var result = await _controller.DeleteVoteByUserAndRequest(2, 1);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    /// <summary>
    /// Tests that users can only delete their own votes.
    /// </summary>
    [Fact]
    public async Task DeleteVoteByUserAndRequest_UserDeletingAnotherUsersVote_ReturnsForbid()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(2, "Audience"); // User ID 2 trying to delete User ID 3's vote

        var vote = new Vote
        {
            UserId = 3, // Different user's vote
            RequestId = 1,
            CreatedAt = DateTime.UtcNow
        };
        _context.Votes.Add(vote);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.DeleteVoteByUserAndRequest(3, 1);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    /// <summary>
    /// Tests validation of invalid model state.
    /// </summary>
    [Fact]
    public async Task CreateVote_InvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(2, "Audience");

        _controller.ModelState.AddModelError("UserId", "UserId is required");

        var voteDto = new VoteCreateDto
        {
            UserId = 2,
            RequestId = 1
        };

        // Act
        var result = await _controller.CreateVote(voteDto);

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