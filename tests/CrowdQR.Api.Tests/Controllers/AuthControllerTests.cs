using System.Security.Claims;
using CrowdQR.Api.Controllers;
using CrowdQR.Api.Services;
using CrowdQR.Api.Tests.Helpers;
using CrowdQR.Shared.Models.DTOs;
using CrowdQR.Shared.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace CrowdQR.Api.Tests.Controllers;

/// <summary>
/// Unit tests for the AuthController.
/// </summary>
public class AuthControllerTests : IDisposable
{
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly AuthController _controller;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthControllerTests"/> class.
    /// </summary>
    public AuthControllerTests()
    {
        _mockAuthService = new Mock<IAuthService>();
        var logger = TestLoggerFactory.CreateNullLogger<AuthController>();
        _controller = new AuthController(_mockAuthService.Object, logger);
    }

    /// <summary>
    /// Tests successful login with valid credentials.
    /// </summary>
    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkWithToken()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            UsernameOrEmail = "test_dj",
            Password = "password123"
        };

        var expectedResult = new AuthResultDto
        {
            Success = true,
            Token = "jwt-token",
            User = new UserDto
            {
                UserId = 1,
                Username = "test_dj",
                Role = UserRole.DJ
            }
        };

        _mockAuthService.Setup(x => x.AuthenticateUser(loginDto.UsernameOrEmail, loginDto.Password))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.Login(loginDto);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult?.Value.Should().BeEquivalentTo(expectedResult);
    }

    /// <summary>
    /// Tests login with invalid credentials.
    /// </summary>
    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            UsernameOrEmail = "test_user",
            Password = "wrong_password"
        };

        var expectedResult = new AuthResultDto
        {
            Success = false,
            ErrorMessage = "Invalid credentials"
        };

        _mockAuthService.Setup(x => x.AuthenticateUser(loginDto.UsernameOrEmail, loginDto.Password))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.Login(loginDto);

        // Assert
        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    /// <summary>
    /// Tests login with unverified email.
    /// </summary>
    [Fact]
    public async Task Login_UnverifiedEmail_ReturnsBadRequest()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            UsernameOrEmail = "unverified@test.com",
            Password = "password123"
        };

        var expectedResult = new AuthResultDto
        {
            Success = false,
            ErrorMessage = "Email verification required",
            EmailVerificationRequired = true
        };

        _mockAuthService.Setup(x => x.AuthenticateUser(loginDto.UsernameOrEmail, loginDto.Password))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.Login(loginDto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Tests login with missing username or email.
    /// </summary>
    [Fact]
    public async Task Login_MissingUsernameOrEmail_ReturnsBadRequest()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            UsernameOrEmail = "",
            Password = "password123"
        };

        // Act
        var result = await _controller.Login(loginDto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result.Result as BadRequestObjectResult;
        var authResult = badRequestResult?.Value as AuthResultDto;
        authResult?.Success.Should().BeFalse();
        authResult?.ErrorMessage.Should().Be("Username or email is required");
    }

    /// <summary>
    /// Tests successful DJ registration.
    /// </summary>
    [Fact]
    public async Task Register_ValidDjData_ReturnsCreated()
    {
        // Arrange
        var registerDto = new DjRegisterDto
        {
            Username = "new_dj",
            Email = "newdj@test.com",
            Password = "password123",
            ConfirmPassword = "password123"
        };

        var expectedResult = new AuthResultDto
        {
            Success = true,
            User = new UserDto
            {
                UserId = 2,
                Username = "new_dj",
                Email = "newdj@test.com",
                Role = UserRole.DJ
            },
            EmailVerificationRequired = true
        };

        _mockAuthService.Setup(x => x.RegisterDj(registerDto))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.Register(registerDto);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
    }

    /// <summary>
    /// Tests registration with duplicate username.
    /// </summary>
    [Fact]
    public async Task Register_DuplicateUsername_ReturnsBadRequest()
    {
        // Arrange
        var registerDto = new DjRegisterDto
        {
            Username = "existing_dj",
            Email = "newdj@test.com",
            Password = "password123",
            ConfirmPassword = "password123"
        };

        var expectedResult = new AuthResultDto
        {
            Success = false,
            ErrorMessage = "Username is already taken"
        };

        _mockAuthService.Setup(x => x.RegisterDj(registerDto))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.Register(registerDto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Tests successful email verification.
    /// </summary>
    [Fact]
    public async Task VerifyEmail_ValidToken_ReturnsOk()
    {
        // Arrange
        var verifyDto = new VerifyEmailDto
        {
            Email = "test@example.com",
            Token = "valid-token"
        };

        _mockAuthService.Setup(x => x.VerifyEmail(verifyDto))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.VerifyEmail(verifyDto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    /// <summary>
    /// Tests email verification with invalid token.
    /// </summary>
    [Fact]
    public async Task VerifyEmail_InvalidToken_ReturnsBadRequest()
    {
        // Arrange
        var verifyDto = new VerifyEmailDto
        {
            Email = "test@example.com",
            Token = "invalid-token"
        };

        _mockAuthService.Setup(x => x.VerifyEmail(verifyDto))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.VerifyEmail(verifyDto);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Tests successful password change.
    /// </summary>
    [Fact]
    public async Task ChangePassword_ValidRequest_ReturnsOk()
    {
        // Arrange
        SetupUserClaims(1, "DJ");

        var changePasswordDto = new ChangePasswordDto
        {
            CurrentPassword = "oldpassword",
            NewPassword = "newpassword123",
            ConfirmPassword = "newpassword123"
        };

        _mockAuthService.Setup(x => x.ChangePassword(1, "oldpassword", "newpassword123"))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ChangePassword(changePasswordDto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    /// <summary>
    /// Tests password change with incorrect current password.
    /// </summary>
    [Fact]
    public async Task ChangePassword_IncorrectCurrentPassword_ReturnsBadRequest()
    {
        // Arrange
        SetupUserClaims(1, "DJ");

        var changePasswordDto = new ChangePasswordDto
        {
            CurrentPassword = "wrongpassword",
            NewPassword = "newpassword123",
            ConfirmPassword = "newpassword123"
        };

        _mockAuthService.Setup(x => x.ChangePassword(1, "wrongpassword", "newpassword123"))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.ChangePassword(changePasswordDto);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Tests model validation for registration.
    /// </summary>
    [Fact]
    public async Task Register_InvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        _controller.ModelState.AddModelError("Email", "Email is required");

        var registerDto = new DjRegisterDto
        {
            Username = "new_dj",
            Email = "", // Invalid email
            Password = "password123",
            ConfirmPassword = "password123"
        };

        // Act
        var result = await _controller.Register(registerDto);

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
        GC.SuppressFinalize(this);
    }
}