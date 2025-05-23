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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CrowdQR.Api.Tests.Controllers;

/// <summary>
/// Unit tests for the UserController.
/// </summary>
public class UserControllerTests : IDisposable
{
    private readonly CrowdQRContext _context;
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly UserController _controller;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserControllerTests"/> class.
    /// </summary>
    public UserControllerTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _mockAuthService = new Mock<IAuthService>();
        var logger = TestLoggerFactory.CreateNullLogger<UserController>();
        _controller = new UserController(_context, logger, _mockAuthService.Object);
    }

    /// <summary>
    /// Tests getting all users as a DJ.
    /// </summary>
    [Fact]
    public async Task GetUsers_AsDj_ReturnsAllUsers()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(1, "DJ");

        // Act
        var result = await _controller.GetUsers();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var users = okResult?.Value as IEnumerable<object>;
        users.Should().NotBeNull();
        users!.Count().Should().Be(3); // From seeded data: 1 DJ + 2 Audience
    }

    /// <summary>
    /// Tests getting a specific user by ID.
    /// </summary>
    [Fact]
    public async Task GetUser_ValidId_ReturnsUser()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(1, "DJ");

        // Act
        var result = await _controller.GetUser(1);

        // Assert
        if (result.Result != null)
        {
            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            okResult?.Value.Should().NotBeNull();
        }
        else
        {
            result.Value.Should().NotBeNull();
        }
    }

    /// <summary>
    /// Tests getting non-existent user.
    /// </summary>
    [Fact]
    public async Task GetUser_NonExistentId_ReturnsNotFound()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(1, "DJ");

        // Act
        var result = await _controller.GetUser(999);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    /// <summary>
    /// Tests that regular users can only access their own user info.
    /// </summary>
    [Fact]
    public async Task GetUser_NonDjAccessingOtherUser_ReturnsForbid()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(2, "Audience"); // User ID 2 trying to access User ID 3

        // Act
        var result = await _controller.GetUser(3);

        // Assert
        result.Result.Should().BeOfType<ForbidResult>();
    }

    /// <summary>
    /// Tests that users can access their own user info.
    /// </summary>
    [Fact]
    public async Task GetUser_UserAccessingOwnInfo_ReturnsUser()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(2, "Audience");

        // Act
        var result = await _controller.GetUser(2);

        // Assert
        if (result.Result != null)
        {
            result.Result.Should().BeOfType<OkObjectResult>();
        }
        else
        {
            result.Value.Should().NotBeNull();
        }
    }

    /// <summary>
    /// Tests getting users by role.
    /// </summary>
    [Fact]
    public async Task GetUsersByRole_ValidRole_ReturnsFilteredUsers()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(1, "DJ");

        // Act
        var result = await _controller.GetUsersByRole(UserRole.Audience);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var users = okResult?.Value as IEnumerable<object>;
        users.Should().NotBeNull();
        users!.Count().Should().Be(2); // Two audience users from seeded data
    }

    /// <summary>
    /// Tests getting user by username.
    /// </summary>
    [Fact]
    public async Task GetUserByUsername_ValidUsername_ReturnsUser()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);

        // Act
        var result = await _controller.GetUserByUsername("test_dj");

        // Assert
        if (result.Result != null)
        {
            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            okResult?.Value.Should().NotBeNull();
        }
        else
        {
            result.Value.Should().NotBeNull();
        }
    }

    /// <summary>
    /// Tests getting user by non-existent username.
    /// </summary>
    [Fact]
    public async Task GetUserByUsername_NonExistentUsername_ReturnsNotFound()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);

        // Act
        var result = await _controller.GetUserByUsername("non_existent_user");

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    /// <summary>
    /// Tests successful user creation by DJ.
    /// </summary>
    [Fact]
    public async Task CreateUser_ValidData_ReturnsCreated()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(1, "DJ");

        var userDto = new UserCreateDto
        {
            Username = "new_user",
            Role = UserRole.Audience
        };

        // Act
        var result = await _controller.CreateUser(userDto);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result.Result as CreatedAtActionResult;
        createdResult?.Value.Should().BeOfType<User>();

        // Verify user was saved
        var savedUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == "new_user");
        savedUser.Should().NotBeNull();
        savedUser!.Role.Should().Be(UserRole.Audience);
    }

    /// <summary>
    /// Tests user creation with duplicate username.
    /// </summary>
    [Fact]
    public async Task CreateUser_DuplicateUsername_ReturnsConflict()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(1, "DJ");

        var userDto = new UserCreateDto
        {
            Username = "test_dj", // Already exists
            Role = UserRole.Audience
        };

        // Act
        var result = await _controller.CreateUser(userDto);

        // Assert
        result.Result.Should().BeOfType<ConflictObjectResult>();
    }

    /// <summary>
    /// Tests successful user update by DJ.
    /// </summary>
    [Fact]
    public async Task UpdateUser_ValidData_ReturnsNoContent()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(1, "DJ");

        var updateDto = new UserUpdateDto
        {
            Username = "updated_username",
            Role = UserRole.DJ
        };

        // Act
        var result = await _controller.UpdateUser(2, updateDto);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        // Verify user was updated
        var updatedUser = await _context.Users.FindAsync(2);
        updatedUser!.Username.Should().Be("updated_username");
        updatedUser.Role.Should().Be(UserRole.DJ);
    }

    /// <summary>
    /// Tests updating non-existent user.
    /// </summary>
    [Fact]
    public async Task UpdateUser_NonExistentUser_ReturnsNotFound()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(1, "DJ");

        var updateDto = new UserUpdateDto
        {
            Username = "updated_username"
        };

        // Act
        var result = await _controller.UpdateUser(999, updateDto);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    /// <summary>
    /// Tests that regular users can update their own info.
    /// </summary>
    [Fact]
    public async Task UpdateUser_UserUpdatingOwnInfo_ReturnsNoContent()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(2, "Audience"); // User updating their own info

        var updateDto = new UserUpdateDto
        {
            Username = "self_updated_username"
        };

        // Act
        var result = await _controller.UpdateUser(2, updateDto);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        // Verify user was updated
        var updatedUser = await _context.Users.FindAsync(2);
        updatedUser!.Username.Should().Be("self_updated_username");
    }

    /// <summary>
    /// Tests that regular users cannot update other users.
    /// </summary>
    [Fact]
    public async Task UpdateUser_UserUpdatingOtherUser_ReturnsForbid()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(2, "Audience"); // User ID 2 trying to update User ID 3

        var updateDto = new UserUpdateDto
        {
            Username = "unauthorized_update"
        };

        // Act
        var result = await _controller.UpdateUser(3, updateDto);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    /// <summary>
    /// Tests successful user deletion by DJ.
    /// </summary>
    [Fact]
    public async Task DeleteUser_ValidRequest_ReturnsNoContent()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(1, "DJ");

        // Act
        var result = await _controller.DeleteUser(2);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        // Verify user was deleted
        var deletedUser = await _context.Users.FindAsync(2);
        deletedUser.Should().BeNull();
    }

    /// <summary>
    /// Tests deleting non-existent user.
    /// </summary>
    [Fact]
    public async Task DeleteUser_NonExistentUser_ReturnsNotFound()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(1, "DJ");

        // Act
        var result = await _controller.DeleteUser(999);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    /// <summary>
    /// Tests resending verification email.
    /// </summary>
    [Fact]
    public async Task ResendVerification_ValidRequest_ReturnsOk()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaimsWithEmail(1, "DJ", "dj@test.com");

        _mockAuthService.Setup(x => x.ResendVerificationEmail("dj@test.com"))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ResendVerification();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    /// <summary>
    /// Tests resending verification email without email claim.
    /// </summary>
    [Fact]
    public async Task ResendVerification_NoEmailClaim_ReturnsBadRequest()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(1, "DJ"); // No email claim

        // Act
        var result = await _controller.ResendVerification();

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Tests getting verification status.
    /// </summary>
    [Fact]
    public async Task GetVerificationStatus_ValidUser_ReturnsStatus()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(1, "DJ");

        // Act
        var result = await _controller.GetVerificationStatus();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    /// <summary>
    /// Tests model validation for user creation.
    /// </summary>
    [Fact]
    public async Task CreateUser_InvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(1, "DJ");

        _controller.ModelState.AddModelError("Username", "Username is required");

        var userDto = new UserCreateDto
        {
            Username = "", // Invalid
            Role = UserRole.Audience
        };

        // Act
        var result = await _controller.CreateUser(userDto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Tests updating user with duplicate username.
    /// </summary>
    [Fact]
    public async Task UpdateUser_DuplicateUsername_ReturnsConflict()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        SetupUserClaims(1, "DJ");

        var updateDto = new UserUpdateDto
        {
            Username = "audience1" // Already exists (User ID 2)
        };

        // Act
        var result = await _controller.UpdateUser(3, updateDto); // Trying to update User ID 3

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
    /// Sets up user claims with email for the controller context.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="role">The user role.</param>
    /// <param name="email">The email address.</param>
    private void SetupUserClaimsWithEmail(int userId, string role, string email)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role),
            new Claim(ClaimTypes.Email, email)
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