using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CrowdQR.Api.Data;
using CrowdQR.Api.Tests.Helpers;
using CrowdQR.Shared.Models.DTOs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace CrowdQR.Api.Tests.Integration;

/// <summary>
/// Integration tests for authentication-related API endpoints.
/// </summary>
public class AuthIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    private readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthIntegrationTests"/> class.
    /// </summary>
    /// <param name="factory">The web application factory.</param>
    public AuthIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                // Remove the existing DbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<CrowdQRContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add in-memory database for testing
                services.AddDbContext<CrowdQRContext>(options =>
                {
                    options.UseInMemoryDatabase("AuthTestDb_" + Guid.NewGuid());
                });
            });
        });

        _client = _factory.CreateClient();
    }

    /// <summary>
    /// Tests successful audience user login (auto-creation).
    /// </summary>
    [Fact]
    public async Task Login_NewAudienceUser_CreatesUserAndReturnsToken()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            UsernameOrEmail = "new_audience_user"
            // No password for audience users
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var authResult = JsonSerializer.Deserialize<AuthResultDto>(content, jsonSerializerOptions);

        authResult.Should().NotBeNull();
        authResult!.Success.Should().BeTrue();
        authResult.Token.Should().NotBeNullOrEmpty();
        authResult.User.Should().NotBeNull();
        authResult.User!.Username.Should().Be("new_audience_user");
        authResult.User.Role.Should().Be(CrowdQR.Shared.Models.Enums.UserRole.Audience);

        // Verify user was created in database
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrowdQRContext>();
        var createdUser = await context.Users.FirstOrDefaultAsync(u => u.Username == "new_audience_user");
        createdUser.Should().NotBeNull();
    }

    /// <summary>
    /// Tests successful DJ registration.
    /// </summary>
    [Fact]
    public async Task Register_ValidDjData_ReturnsSuccess()
    {
        // Arrange
        var registerDto = new DjRegisterDto
        {
            Username = "test_dj_register",
            Email = "testdj@example.com",
            Password = "TestPassword123!",
            ConfirmPassword = "TestPassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await response.Content.ReadAsStringAsync();
        var authResult = JsonSerializer.Deserialize<AuthResultDto>(content, jsonSerializerOptions);

        authResult.Should().NotBeNull();
        authResult!.Success.Should().BeTrue();
        authResult.User.Should().NotBeNull();
        authResult.User!.Username.Should().Be("test_dj_register");
        authResult.User.Email.Should().Be("testdj@example.com");
        authResult.User.Role.Should().Be(CrowdQR.Shared.Models.Enums.UserRole.DJ);
        authResult.EmailVerificationRequired.Should().BeTrue();

        // Verify user was created in database
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrowdQRContext>();
        var createdUser = await context.Users.FirstOrDefaultAsync(u => u.Username == "test_dj_register");
        createdUser.Should().NotBeNull();
        createdUser!.Role.Should().Be(CrowdQR.Shared.Models.Enums.UserRole.DJ);
        createdUser.IsEmailVerified.Should().BeFalse();
    }

    /// <summary>
    /// Tests DJ registration with duplicate username.
    /// </summary>
    [Fact]
    public async Task Register_DuplicateUsername_ReturnsBadRequest()
    {
        // Arrange - First registration
        var firstRegisterDto = new DjRegisterDto
        {
            Username = "duplicate_dj",
            Email = "first@example.com",
            Password = "TestPassword123!",
            ConfirmPassword = "TestPassword123!"
        };

        await _client.PostAsJsonAsync("/api/auth/register", firstRegisterDto);

        // Second registration with same username
        var secondRegisterDto = new DjRegisterDto
        {
            Username = "duplicate_dj", // Same username
            Email = "second@example.com",
            Password = "TestPassword123!",
            ConfirmPassword = "TestPassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", secondRegisterDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Tests DJ registration with duplicate email.
    /// </summary>
    [Fact]
    public async Task Register_DuplicateEmail_ReturnsBadRequest()
    {
        // Arrange - First registration
        var firstRegisterDto = new DjRegisterDto
        {
            Username = "first_dj",
            Email = "duplicate@example.com",
            Password = "TestPassword123!",
            ConfirmPassword = "TestPassword123!"
        };

        await _client.PostAsJsonAsync("/api/auth/register", firstRegisterDto);

        // Second registration with same email
        var secondRegisterDto = new DjRegisterDto
        {
            Username = "second_dj",
            Email = "duplicate@example.com", // Same email
            Password = "TestPassword123!",
            ConfirmPassword = "TestPassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", secondRegisterDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Tests login with invalid credentials.
    /// </summary>
    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange - Create a DJ user first
        await SeedDjUser();

        var loginDto = new LoginDto
        {
            UsernameOrEmail = "test_dj",
            Password = "WrongPassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Tests successful DJ login with correct credentials.
    /// </summary>
    [Fact]
    public async Task Login_ValidDjCredentials_ReturnsToken()
    {
        // Arrange - Create DJ user through registration
        var registerDto = new DjRegisterDto
        {
            Username = "login_test_dj",
            Email = "logintest@example.com",
            Password = "TestPassword123!",
            ConfirmPassword = "TestPassword123!"
        };

        await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        // Now try to login
        var loginDto = new LoginDto
        {
            UsernameOrEmail = "login_test_dj",
            Password = "TestPassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var authResult = JsonSerializer.Deserialize<AuthResultDto>(content, jsonSerializerOptions);

        authResult.Should().NotBeNull();
        authResult!.Success.Should().BeTrue();
        authResult.Token.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Tests email verification flow.
    /// </summary>
    [Fact]
    public async Task VerifyEmail_ValidToken_ReturnsSuccess()
    {
        // Arrange - Register DJ user
        var registerDto = new DjRegisterDto
        {
            Username = "verify_test_dj",
            Email = "verify@example.com",
            Password = "TestPassword123!",
            ConfirmPassword = "TestPassword123!"
        };

        await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        // Get the verification token from database
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrowdQRContext>();
        var user = await context.Users.FirstOrDefaultAsync(u => u.Username == "verify_test_dj");
        var verificationToken = user!.EmailVerificationToken;

        var verifyDto = new VerifyEmailDto
        {
            Email = "verify@example.com",
            Token = verificationToken!
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/verify-email", verifyDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify user is now verified in database
        var verifiedUser = await context.Users.FirstOrDefaultAsync(u => u.Username == "verify_test_dj");
        verifiedUser!.IsEmailVerified.Should().BeTrue();
        verifiedUser.EmailVerificationToken.Should().BeNull();
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

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/verify-email", verifyDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Tests login without username/email.
    /// </summary>
    [Fact]
    public async Task Login_MissingUsername_ReturnsBadRequest()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            UsernameOrEmail = "", // Empty username
            Password = "password"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Tests registration with invalid model data.
    /// </summary>
    [Fact]
    public async Task Register_InvalidModelData_ReturnsBadRequest()
    {
        // Arrange
        var registerDto = new DjRegisterDto
        {
            Username = "", // Invalid - empty username
            Email = "invalid-email", // Invalid email format
            Password = "123", // Invalid - too short
            ConfirmPassword = "456" // Invalid - doesn't match
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Tests that JWT token validation works correctly.
    /// </summary>
    [Fact]
    public async Task ValidateToken_ValidToken_ReturnsUserInfo()
    {
        // Arrange - Create and login user
        var loginDto = new LoginDto
        {
            UsernameOrEmail = "token_test_user"
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        var authResult = JsonSerializer.Deserialize<AuthResultDto>(loginContent, jsonSerializerOptions);

        // Set authorization header
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResult!.Token);

        // Act
        var response = await _client.GetAsync("/api/auth/validate");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var userDto = JsonSerializer.Deserialize<UserDto>(content, jsonSerializerOptions);

        userDto.Should().NotBeNull();
        userDto!.Username.Should().Be("token_test_user");
    }

    /// <summary>
    /// Tests token validation with invalid token.
    /// </summary>
    [Fact]
    public async Task ValidateToken_InvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "invalid-token");

        // Act
        var response = await _client.GetAsync("/api/auth/validate");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Seeds a DJ user for testing.
    /// </summary>
    private async Task SeedDjUser()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrowdQRContext>();

        var djUser = new CrowdQR.Api.Models.User
        {
            Username = "test_dj",
            Email = "testdj@example.com",
            Role = CrowdQR.Shared.Models.Enums.UserRole.DJ,
            PasswordHash = "test-hash",
            PasswordSalt = "test-salt",
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(djUser);
        await context.SaveChangesAsync();
    }
}