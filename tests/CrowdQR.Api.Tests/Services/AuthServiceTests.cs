using CrowdQR.Api.Data;
using CrowdQR.Api.Models;
using CrowdQR.Api.Services;
using CrowdQR.Api.Tests.Helpers;
using CrowdQR.Shared.Models.DTOs;
using CrowdQR.Shared.Models.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;

namespace CrowdQR.Api.Tests.Services;

/// <summary>
/// Unit tests for the AuthService.
/// </summary>
public class AuthServiceTests : IDisposable
{
    private readonly CrowdQRContext _context;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IPasswordService> _mockPasswordService;
    private readonly Mock<ITokenService> _mockTokenService;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly AuthService _authService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthServiceTests"/> class.
    /// </summary>
    public AuthServiceTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockPasswordService = new Mock<IPasswordService>();
        _mockTokenService = new Mock<ITokenService>();
        _mockEmailService = new Mock<IEmailService>();

        var logger = TestLoggerFactory.CreateNullLogger<AuthService>();

        // Setup configuration mocks
        _mockConfiguration.Setup(x => x["Jwt:Secret"]).Returns("test-secret-key-for-jwt-token-generation-12345");
        _mockConfiguration.Setup(x => x["Jwt:Issuer"]).Returns("CrowdQR.Api.Test");
        _mockConfiguration.Setup(x => x["Jwt:Audience"]).Returns("CrowdQR.Web.Test");

        _authService = new AuthService(
            _context,
            _mockConfiguration.Object,
            logger,
            _mockPasswordService.Object,
            _mockTokenService.Object,
            _mockEmailService.Object);
    }

    /// <summary>
    /// Tests successful authentication of existing audience user without password.
    /// </summary>
    [Fact]
    public async Task AuthenticateUser_ExistingAudienceUser_ReturnsSuccess()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);

        // Act
        var result = await _authService.AuthenticateUser("audience1");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.User.Should().NotBeNull();
        result.User!.Username.Should().Be("audience1");
        result.User.Role.Should().Be(UserRole.Audience);
        result.Token.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Tests creation of new audience user when username doesn't exist.
    /// </summary>
    [Fact]
    public async Task AuthenticateUser_NewAudienceUser_CreatesUserAndReturnsSuccess()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        var newUsername = "new_audience_user";

        // Act
        var result = await _authService.AuthenticateUser(newUsername);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.User.Should().NotBeNull();
        result.User!.Username.Should().Be(newUsername);
        result.User.Role.Should().Be(UserRole.Audience);

        // Verify user was created in database
        var createdUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == newUsername);
        createdUser.Should().NotBeNull();
        createdUser!.Role.Should().Be(UserRole.Audience);
    }

    /// <summary>
    /// Tests successful DJ authentication with correct password.
    /// </summary>
    [Fact]
    public async Task AuthenticateUser_ValidDjCredentials_ReturnsSuccess()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);

        // Setup password verification to return true
        _mockPasswordService.Setup(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        // Update the DJ user to have password hash and salt
        var djUser = await _context.Users.FindAsync(1);
        djUser!.PasswordHash = "test-hash";
        djUser.PasswordSalt = "test-salt";
        djUser.IsEmailVerified = true;
        await _context.SaveChangesAsync();

        // Act
        var result = await _authService.AuthenticateUser("test_dj", "correct-password");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.User.Should().NotBeNull();
        result.User!.Username.Should().Be("test_dj");
        result.User.Role.Should().Be(UserRole.DJ);
        result.Token.Should().NotBeNullOrEmpty();

        // Verify password verification was called
        _mockPasswordService.Verify(
            x => x.VerifyPassword("correct-password", "test-hash", "test-salt"),
            Times.Once);
    }

    /// <summary>
    /// Tests DJ authentication failure with incorrect password.
    /// </summary>
    [Fact]
    public async Task AuthenticateUser_InvalidDjPassword_ReturnsFailure()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);

        // Setup password verification to return false
        _mockPasswordService.Setup(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);

        // Update the DJ user to have password hash and salt
        var djUser = await _context.Users.FindAsync(1);
        djUser!.PasswordHash = "test-hash";
        djUser.PasswordSalt = "test-salt";
        await _context.SaveChangesAsync();

        // Act
        var result = await _authService.AuthenticateUser("test_dj", "wrong-password");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Invalid username/email or password");
        result.Token.Should().BeNull();
    }

    /// <summary>
    /// Tests DJ authentication requires password.
    /// </summary>
    [Fact]
    public async Task AuthenticateUser_DjWithoutPassword_ReturnsFailure()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);

        // Act
        var result = await _authService.AuthenticateUser("test_dj"); // No password provided

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Password is required for DJ accounts");
    }

    /// <summary>
    /// Tests successful DJ registration.
    /// </summary>
    [Fact]
    public async Task RegisterDj_ValidData_ReturnsSuccess()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);

        _mockPasswordService.Setup(x => x.HashPassword(It.IsAny<string>()))
            .Returns(("hashed-password", "password-salt"));
        _mockTokenService.Setup(x => x.GenerateVerificationToken())
            .Returns("verification-token");
        _mockTokenService.Setup(x => x.GenerateTokenExpiry(24))
            .Returns(DateTime.UtcNow.AddHours(24));
        _mockEmailService.Setup(x => x.SendVerificationEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var registerDto = new AuthDjRegisterDto
        {
            Username = "new_dj",
            Email = "newdj@test.com",
            Password = "password123",
            ConfirmPassword = "password123"
        };

        // Act
        var result = await _authService.RegisterDj(registerDto);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.User.Should().NotBeNull();
        result.User!.Username.Should().Be("new_dj");
        result.User.Email.Should().Be("newdj@test.com");
        result.User.Role.Should().Be(UserRole.DJ);
        result.EmailVerificationRequired.Should().BeTrue();

        // Verify user was created in database
        var createdUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == "new_dj");
        createdUser.Should().NotBeNull();
        createdUser!.Role.Should().Be(UserRole.DJ);
        createdUser.IsEmailVerified.Should().BeFalse();

        // Verify services were called
        _mockPasswordService.Verify(x => x.HashPassword("password123"), Times.Once);
        _mockTokenService.Verify(x => x.GenerateVerificationToken(), Times.Once);
        _mockEmailService.Verify(
            x => x.SendVerificationEmailAsync("newdj@test.com", "new_dj", "verification-token"),
            Times.Once);
    }

    /// <summary>
    /// Tests DJ registration with duplicate username.
    /// </summary>
    [Fact]
    public async Task RegisterDj_DuplicateUsername_ReturnsFailure()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);

        var registerDto = new AuthDjRegisterDto
        {
            Username = "test_dj", // Already exists
            Email = "duplicate@test.com",
            Password = "password123",
            ConfirmPassword = "password123"
        };

        // Act
        var result = await _authService.RegisterDj(registerDto);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Username is already taken");
    }

    /// <summary>
    /// Tests DJ registration with duplicate email.
    /// </summary>
    [Fact]
    public async Task RegisterDj_DuplicateEmail_ReturnsFailure()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);

        var registerDto = new AuthDjRegisterDto
        {
            Username = "new_unique_dj",
            Email = "dj@test.com", // Already exists
            Password = "password123",
            ConfirmPassword = "password123"
        };

        // Act
        var result = await _authService.RegisterDj(registerDto);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Email is already registered");
    }

    /// <summary>
    /// Tests successful email verification.
    /// </summary>
    [Fact]
    public async Task VerifyEmail_ValidToken_ReturnsTrue()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);

        var djUser = await _context.Users.FindAsync(1);
        djUser!.EmailVerificationToken = "valid-token";
        djUser.EmailTokenExpiry = DateTime.UtcNow.AddHours(1);
        djUser.IsEmailVerified = false;
        await _context.SaveChangesAsync();

        var verifyDto = new AuthVerifyEmailDto
        {
            Email = "dj@test.com",
            Token = "valid-token"
        };

        // Act
        var result = await _authService.VerifyEmail(verifyDto);

        // Assert
        result.Should().BeTrue();

        // Verify user was updated
        var updatedUser = await _context.Users.FindAsync(1);
        updatedUser!.IsEmailVerified.Should().BeTrue();
        updatedUser.EmailVerificationToken.Should().BeNull();
        updatedUser.EmailTokenExpiry.Should().BeNull();
    }

    /// <summary>
    /// Tests email verification with expired token.
    /// </summary>
    [Fact]
    public async Task VerifyEmail_ExpiredToken_ReturnsFalse()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);

        var djUser = await _context.Users.FindAsync(1);
        djUser!.EmailVerificationToken = "expired-token";
        djUser.EmailTokenExpiry = DateTime.UtcNow.AddHours(-1); // Expired
        djUser.IsEmailVerified = false;
        await _context.SaveChangesAsync();

        var verifyDto = new AuthVerifyEmailDto
        {
            Email = "dj@test.com",
            Token = "expired-token"
        };

        // Act
        var result = await _authService.VerifyEmail(verifyDto);

        // Assert
        result.Should().BeFalse();

        // Verify user was not updated
        var updatedUser = await _context.Users.FindAsync(1);
        updatedUser!.IsEmailVerified.Should().BeFalse();
    }

    /// <summary>
    /// Tests email verification with invalid token.
    /// </summary>
    [Fact]
    public async Task VerifyEmail_InvalidToken_ReturnsFalse()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);

        var verifyDto = new AuthVerifyEmailDto
        {
            Email = "dj@test.com",
            Token = "invalid-token"
        };

        // Act
        var result = await _authService.VerifyEmail(verifyDto);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Tests successful password change.
    /// </summary>
    [Fact]
    public async Task ChangePassword_ValidCurrentPassword_ReturnsTrue()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);

        var djUser = await _context.Users.FindAsync(1);
        djUser!.PasswordHash = "old-hash";
        djUser.PasswordSalt = "old-salt";
        await _context.SaveChangesAsync();

        _mockPasswordService.Setup(x => x.VerifyPassword("current-password", "old-hash", "old-salt"))
            .Returns(true);
        _mockPasswordService.Setup(x => x.HashPassword("new-password"))
            .Returns(("new-hash", "new-salt"));

        // Act
        var result = await _authService.ChangePassword(1, "current-password", "new-password");

        // Assert
        result.Should().BeTrue();

        // Verify password was updated
        var updatedUser = await _context.Users.FindAsync(1);
        updatedUser!.PasswordHash.Should().Be("new-hash");
        updatedUser.PasswordSalt.Should().Be("new-salt");

        // Verify services were called
        _mockPasswordService.Verify(x => x.VerifyPassword("current-password", "old-hash", "old-salt"), Times.Once);
        _mockPasswordService.Verify(x => x.HashPassword("new-password"), Times.Once);
    }

    /// <summary>
    /// Tests password change with incorrect current password.
    /// </summary>
    [Fact]
    public async Task ChangePassword_InvalidCurrentPassword_ReturnsFalse()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);

        var djUser = await _context.Users.FindAsync(1);
        djUser!.PasswordHash = "old-hash";
        djUser.PasswordSalt = "old-salt";
        await _context.SaveChangesAsync();

        _mockPasswordService.Setup(x => x.VerifyPassword("wrong-password", "old-hash", "old-salt"))
            .Returns(false);

        // Act
        var result = await _authService.ChangePassword(1, "wrong-password", "new-password");

        // Assert
        result.Should().BeFalse();

        // Verify password was not updated
        var updatedUser = await _context.Users.FindAsync(1);
        updatedUser!.PasswordHash.Should().Be("old-hash");
        updatedUser.PasswordSalt.Should().Be("old-salt");
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