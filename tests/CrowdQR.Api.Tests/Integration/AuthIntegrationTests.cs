using System.Net;
using System.Net.Http.Json;
using System.Text;
using CrowdQR.Api.Data;
using CrowdQR.Api.Tests.Helpers;
using CrowdQR.Shared.Models.DTOs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace CrowdQR.Api.Tests.Integration;

/// <summary>
/// Integration tests for authentication-related API endpoints.
/// </summary>
public class AuthIntegrationTests : IClassFixture<WebApplicationFactory<CrowdQR.Api.Program>>
{
    private readonly WebApplicationFactory<CrowdQR.Api.Program> _factory;
    private readonly HttpClient _client;
    private static readonly string DatabaseName = "AuthTestDb_" + Guid.NewGuid();

    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthIntegrationTests"/> class.
    /// </summary>
    /// <param name="factory">The web application factory.</param>
    public AuthIntegrationTests(WebApplicationFactory<CrowdQR.Api.Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                // Remove ALL Entity Framework related services
                var descriptorsToRemove = new List<ServiceDescriptor>();

                foreach (var service in services)
                {
                    if (service.ServiceType.Namespace != null &&
                        (service.ServiceType.Namespace.StartsWith("Microsoft.EntityFrameworkCore") ||
                         service.ServiceType == typeof(CrowdQRContext) ||
                         service.ServiceType == typeof(DbContextOptions<CrowdQRContext>) ||
                         service.ServiceType == typeof(DbContextOptions)))
                    {
                        descriptorsToRemove.Add(service);
                    }
                }

                foreach (var descriptor in descriptorsToRemove)
                {
                    services.Remove(descriptor);
                }

                // Add in-memory database for testing with a static database name
                services.AddDbContext<CrowdQRContext>(options =>
                {
                    options.UseInMemoryDatabase(DatabaseName);
                    options.EnableSensitiveDataLogging();
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
        await ClearDatabase();
        var loginDto = new LoginDto
        {
            UsernameOrEmail = "new_audience_user"
            // No password for audience users
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, $"Login failed. Response: {content}");

        var authResult = JsonSerializer.Deserialize<AuthResultDto>(content, _jsonSerializerOptions);

        authResult.Should().NotBeNull();
        authResult!.Success.Should().BeTrue();
        authResult.Token.Should().NotBeNullOrEmpty();
        authResult.User.Should().NotBeNull();
        authResult.User!.Username.Should().Be("new_audience_user");
        authResult.User.Role.Should().Be(CrowdQR.Shared.Models.Enums.UserRole.Audience);

        // Verify user was created in database using the same database name
        await VerifyUserInDatabase("new_audience_user", "User should be created in database");
    }

    /// <summary>
    /// Tests successful DJ registration.
    /// </summary>
    [Fact]
    public async Task Register_ValidDjData_ReturnsSuccess()
    {
        // Arrange
        await ClearDatabase();
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
        var content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, $"Registration failed. Response: {content}");

        var authResult = JsonSerializer.Deserialize<AuthResultDto>(content, _jsonSerializerOptions);

        authResult.Should().NotBeNull();
        authResult!.Success.Should().BeTrue();
        authResult.User.Should().NotBeNull();
        authResult.User!.Username.Should().Be("test_dj_register");
        authResult.User.Email.Should().Be("testdj@example.com");
        authResult.User.Role.Should().Be(CrowdQR.Shared.Models.Enums.UserRole.DJ);
        authResult.EmailVerificationRequired.Should().BeTrue();

        // Verify user was created in database
        var createdUser = await GetUserFromDatabase("test_dj_register");
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
        // Arrange
        await ClearDatabase();

        // First registration
        var firstRegisterDto = new DjRegisterDto
        {
            Username = "duplicate_dj",
            Email = "first@example.com",
            Password = "TestPassword123!",
            ConfirmPassword = "TestPassword123!"
        };

        var firstResponse = await _client.PostAsJsonAsync("/api/auth/register", firstRegisterDto);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created, "First registration should succeed");

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
        var content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            $"Expected BadRequest for duplicate username but got {response.StatusCode}. Response: {content}");
    }

    /// <summary>
    /// Tests DJ registration with duplicate email.
    /// </summary>
    [Fact]
    public async Task Register_DuplicateEmail_ReturnsBadRequest()
    {
        // Arrange
        await ClearDatabase();

        // First registration
        var firstRegisterDto = new DjRegisterDto
        {
            Username = "first_dj",
            Email = "duplicate@example.com",
            Password = "TestPassword123!",
            ConfirmPassword = "TestPassword123!"
        };

        var firstResponse = await _client.PostAsJsonAsync("/api/auth/register", firstRegisterDto);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created, "First registration should succeed");

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
        var content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            $"Expected BadRequest for duplicate email but got {response.StatusCode}. Response: {content}");
    }

    /// <summary>
    /// Tests successful DJ login with correct credentials.
    /// </summary>
    [Fact]
    public async Task Login_ValidDjCredentials_ReturnsToken()
    {
        // Arrange
        await ClearDatabase();

        // Create DJ user through registration
        var registerDto = new DjRegisterDto
        {
            Username = "login_test_dj",
            Email = "logintest@example.com",
            Password = "TestPassword123!",
            ConfirmPassword = "TestPassword123!"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerDto);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created, "Registration should succeed");

        // Now try to login
        var loginDto = new LoginDto
        {
            UsernameOrEmail = "login_test_dj",
            Password = "TestPassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, $"Login failed. Response: {content}");

        var authResult = JsonSerializer.Deserialize<AuthResultDto>(content, _jsonSerializerOptions);

        authResult.Should().NotBeNull();
        authResult!.Success.Should().BeTrue();
        authResult.Token.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Tests login with invalid credentials.
    /// </summary>
    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        await ClearDatabase();

        // Create a DJ user first
        var registerDto = new DjRegisterDto
        {
            Username = "test_dj_invalid",
            Email = "invalid@example.com",
            Password = "TestPassword123!",
            ConfirmPassword = "TestPassword123!"
        };

        await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        var loginDto = new LoginDto
        {
            UsernameOrEmail = "test_dj_invalid",
            Password = "WrongPassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            $"Expected Unauthorized for invalid credentials but got {response.StatusCode}. Response: {content}");
    }

    /// <summary>
    /// Tests email verification flow.
    /// </summary>
    [Fact]
    public async Task VerifyEmail_ValidToken_ReturnsSuccess()
    {
        // Arrange
        await ClearDatabase();

        // Register DJ user
        var registerDto = new DjRegisterDto
        {
            Username = "verify_test_dj",
            Email = "verify@example.com",
            Password = "TestPassword123!",
            ConfirmPassword = "TestPassword123!"
        };

        await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        // Get the verification token from database
        var user = await GetUserFromDatabase("verify_test_dj");
        user.Should().NotBeNull("User should exist after registration");
        var verificationToken = user!.EmailVerificationToken;
        verificationToken.Should().NotBeNullOrEmpty("Verification token should be generated");

        var verifyDto = new VerifyEmailDto
        {
            Email = "verify@example.com",
            Token = verificationToken!
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/verify-email", verifyDto);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, $"Email verification failed. Response: {content}");

        // Verify user is now verified in database
        var verifiedUser = await GetUserFromDatabase("verify_test_dj");
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
        await ClearDatabase();
        var verifyDto = new VerifyEmailDto
        {
            Email = "test@example.com",
            Token = "invalid-token"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/verify-email", verifyDto);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            $"Expected BadRequest for invalid token but got {response.StatusCode}. Response: {content}");
    }

    /// <summary>
    /// Tests login without username/email.
    /// </summary>
    [Fact]
    public async Task Login_MissingUsername_ReturnsBadRequest()
    {
        // Arrange
        await ClearDatabase();
        var loginDto = new LoginDto
        {
            UsernameOrEmail = "", // Empty username
            Password = "password"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            $"Expected BadRequest for missing username but got {response.StatusCode}. Response: {content}");
    }

    /// <summary>
    /// Tests registration with invalid model data.
    /// </summary>
    [Fact]
    public async Task Register_InvalidModelData_ReturnsBadRequest()
    {
        // Arrange
        await ClearDatabase();
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
        var content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            $"Expected BadRequest for invalid model data but got {response.StatusCode}. Response: {content}");
    }

    /// <summary>
    /// Tests that JWT token validation works correctly when using real JWT.
    /// </summary>
    [Fact]
    public async Task ValidateToken_ValidToken_ReturnsUserInfo()
    {
        // Arrange
        await ClearDatabase();

        // Create and login user
        var loginDto = new LoginDto
        {
            UsernameOrEmail = "token_test_user"
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK, "Login should succeed");

        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        var authResult = JsonSerializer.Deserialize<AuthResultDto>(loginContent, _jsonSerializerOptions);

        // Set authorization header with the real JWT token
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResult!.Token);

        // Act
        var response = await _client.GetAsync("/api/auth/validate");

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, $"Token validation failed. Response: {content}");

        var userDto = JsonSerializer.Deserialize<UserDto>(content, _jsonSerializerOptions);

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
        await ClearDatabase();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "invalid-token");

        // Act
        var response = await _client.GetAsync("/api/auth/validate");

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            $"Expected Unauthorized for invalid token but got {response.StatusCode}. Response: {content}");
    }

    /// <summary>
    /// Clears the test database.
    /// </summary>
    private static async Task ClearDatabase()
    {
        var options = new DbContextOptionsBuilder<CrowdQRContext>()
            .UseInMemoryDatabase(DatabaseName)
            .EnableSensitiveDataLogging()
            .Options;

        await using var context = new CrowdQRContext(options);

        // Ensure the database is created
        await context.Database.EnsureCreatedAsync();

        // Clear existing data
        if (context.Users.Any()) context.Users.RemoveRange(context.Users);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Gets a user from the database by username.
    /// </summary>
    /// <param name="username">The username to search for.</param>
    /// <returns>The user if found, null otherwise.</returns>
    private static async Task<CrowdQR.Api.Models.User?> GetUserFromDatabase(string username)
    {
        var options = new DbContextOptionsBuilder<CrowdQRContext>()
            .UseInMemoryDatabase(DatabaseName)
            .EnableSensitiveDataLogging()
            .Options;

        await using var context = new CrowdQRContext(options);
        return await context.Users.FirstOrDefaultAsync(u => u.Username == username);
    }

    /// <summary>
    /// Verifies that a user exists in the database.
    /// </summary>
    /// <param name="username">The username to check.</param>
    /// <param name="because">The reason for the assertion.</param>
    private static async Task VerifyUserInDatabase(string username, string because)
    {
        var user = await GetUserFromDatabase(username);
        user.Should().NotBeNull(because);
    }
}