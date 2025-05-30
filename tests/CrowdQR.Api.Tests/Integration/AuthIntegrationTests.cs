﻿using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CrowdQR.Shared.Models.DTOs;
using Microsoft.Extensions.DependencyInjection;


namespace CrowdQR.Api.Tests.Integration;

/// <summary>
/// Integration tests for authentication-related API endpoints.
/// </summary>
public class AuthIntegrationTests : BaseIntegrationTest
{
    /// <summary>
    /// Tests successful audience user login (auto-creation).
    /// </summary>
    [Fact]
    public async Task Login_NewAudienceUser_CreatesUserAndReturnsToken()
    {
        // Arrange
        await ClearDatabaseAsync();
        var loginDto = new AuthLoginDto
        {
            UsernameOrEmail = "new_audience_user"
            // No password for audience users
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, $"Login failed. Response: {content}");

        var authResult = JsonSerializer.Deserialize<AuthResultDto>(content, JsonOptions);

        authResult.Should().NotBeNull();
        authResult!.Success.Should().BeTrue();
        authResult.Token.Should().NotBeNullOrEmpty();
        authResult.User.Should().NotBeNull();
        authResult.User!.Username.Should().Be("new_audience_user");
        authResult.User.Role.Should().Be(CrowdQR.Shared.Models.Enums.UserRole.Audience);

        // Verify user was created in database using the same database name
        await VerifyUserInDatabaseAsync("new_audience_user", "User should be created in database");
    }

    /// <summary>
    /// Tests successful DJ registration.
    /// </summary>
    [Fact]
    public async Task Register_ValidDjData_ReturnsSuccess()
    {
        // Arrange
        await ClearDatabaseAsync();
        var registerDto = new AuthDjRegisterDto
        {
            Username = "test_dj_register",
            Email = "testdj@example.com",
            Password = "TestPassword123!",
            ConfirmPassword = "TestPassword123!"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/register", registerDto);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, $"Registration failed. Response: {content}");

        var authResult = JsonSerializer.Deserialize<AuthResultDto>(content, JsonOptions);

        authResult.Should().NotBeNull();
        authResult!.Success.Should().BeTrue();
        authResult.User.Should().NotBeNull();
        authResult.User!.Username.Should().Be("test_dj_register");
        authResult.User.Email.Should().Be("testdj@example.com");
        authResult.User.Role.Should().Be(CrowdQR.Shared.Models.Enums.UserRole.DJ);
        authResult.EmailVerificationRequired.Should().BeTrue();

        // Verify user was created in database
        var createdUser = await GetUserFromDatabaseAsync("test_dj_register");
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
        await ClearDatabaseAsync();

        // First registration
        var firstRegisterDto = new AuthDjRegisterDto
        {
            Username = "duplicate_dj",
            Email = "first@example.com",
            Password = "TestPassword123!",
            ConfirmPassword = "TestPassword123!"
        };

        var firstResponse = await Client.PostAsJsonAsync("/api/auth/register", firstRegisterDto);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created, "First registration should succeed");

        // Second registration with same username
        var secondRegisterDto = new AuthDjRegisterDto
        {
            Username = "duplicate_dj", // Same username
            Email = "second@example.com",
            Password = "TestPassword123!",
            ConfirmPassword = "TestPassword123!"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/register", secondRegisterDto);

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
        await ClearDatabaseAsync();

        // First registration
        var firstRegisterDto = new AuthDjRegisterDto
        {
            Username = "first_dj",
            Email = "duplicate@example.com",
            Password = "TestPassword123!",
            ConfirmPassword = "TestPassword123!"
        };

        var firstResponse = await Client.PostAsJsonAsync("/api/auth/register", firstRegisterDto);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created, "First registration should succeed");

        // Second registration with same email
        var secondRegisterDto = new AuthDjRegisterDto
        {
            Username = "second_dj",
            Email = "duplicate@example.com", // Same email
            Password = "TestPassword123!",
            ConfirmPassword = "TestPassword123!"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/register", secondRegisterDto);

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
        await ClearDatabaseAsync();

        // Create DJ user through registration
        var registerDto = new AuthDjRegisterDto
        {
            Username = "login_test_dj",
            Email = "logintest@example.com",
            Password = "TestPassword123!",
            ConfirmPassword = "TestPassword123!"
        };

        var registerResponse = await Client.PostAsJsonAsync("/api/auth/register", registerDto);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created, "Registration should succeed");

        // Now try to login
        var loginDto = new AuthLoginDto
        {
            UsernameOrEmail = "login_test_dj",
            Password = "TestPassword123!"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, $"Login failed. Response: {content}");

        var authResult = JsonSerializer.Deserialize<AuthResultDto>(content, JsonOptions);

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
        await ClearDatabaseAsync();

        // Create a DJ user first
        var registerDto = new AuthDjRegisterDto
        {
            Username = "test_dj_invalid",
            Email = "invalid@example.com",
            Password = "TestPassword123!",
            ConfirmPassword = "TestPassword123!"
        };

        await Client.PostAsJsonAsync("/api/auth/register", registerDto);

        var loginDto = new AuthLoginDto
        {
            UsernameOrEmail = "test_dj_invalid",
            Password = "WrongPassword123!"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", loginDto);

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
        await ClearDatabaseAsync();

        // Register DJ user
        var registerDto = new AuthDjRegisterDto
        {
            Username = "verify_test_dj",
            Email = "verify@example.com",
            Password = "TestPassword123!",
            ConfirmPassword = "TestPassword123!"
        };

        await Client.PostAsJsonAsync("/api/auth/register", registerDto);

        // Get the verification token from database
        var user = await GetUserFromDatabaseAsync("verify_test_dj");
        user.Should().NotBeNull("User should exist after registration");
        var verificationToken = user!.EmailVerificationToken;
        verificationToken.Should().NotBeNullOrEmpty("Verification token should be generated");

        var verifyDto = new AuthVerifyEmailDto
        {
            Email = "verify@example.com",
            Token = verificationToken!
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/verify-email", verifyDto);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, $"Email verification failed. Response: {content}");

        // Verify user is now verified in database
        var verifiedUser = await GetUserFromDatabaseAsync("verify_test_dj");
        verifiedUser!.IsEmailVerified.Should().BeTrue();
        verifiedUser.EmailVerificationToken.Should().BeNull();
    }

    /// <summary>
    /// Don't configure test authentication for auth tests - we want to test real JWT.
    /// </summary>
    /// <param name="services">The service collection.</param>
    protected override void ConfigureTestAuthentication(IServiceCollection services)
    {
        // Don't override authentication for auth tests - use real JWT
    }

    /// <summary>
    /// Tests email verification with invalid token.
    /// </summary>
    [Fact]
    public async Task VerifyEmail_InvalidToken_ReturnsBadRequest()
    {
        // Arrange
        await ClearDatabaseAsync();
        var verifyDto = new AuthVerifyEmailDto
        {
            Email = "test@example.com",
            Token = "invalid-token"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/verify-email", verifyDto);

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
        await ClearDatabaseAsync();
        var loginDto = new AuthLoginDto
        {
            UsernameOrEmail = "", // Empty username
            Password = "password"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", loginDto);

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
        await ClearDatabaseAsync();
        var registerDto = new AuthDjRegisterDto
        {
            Username = "", // Invalid - empty username
            Email = "invalid-email", // Invalid email format
            Password = "123", // Invalid - too short
            ConfirmPassword = "456" // Invalid - doesn't match
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/register", registerDto);

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
        await ClearDatabaseAsync();

        // Create and login user
        var loginDto = new AuthLoginDto
        {
            UsernameOrEmail = "token_test_user"
        };

        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", loginDto);
        var loginContent = await loginResponse.Content.ReadAsStringAsync();

        // Debug: Log the login response
        Console.WriteLine($"Login Response Status: {loginResponse.StatusCode}");
        Console.WriteLine($"Login Response Content: {loginContent}");

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK, "Login should succeed");

        var authResult = JsonSerializer.Deserialize<AuthResultDto>(loginContent, JsonOptions);

        // Debug: Check the token
        Console.WriteLine($"Generated Token: {authResult!.Token}");
        Console.WriteLine($"Token Length: {authResult.Token?.Length}");

        authResult.Token.Should().NotBeNullOrEmpty("Token should be generated");

        // Set authorization header with the real JWT token
        Client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResult.Token);

        // Act
        var response = await Client.GetAsync("/api/auth/validate");

        // Assert
        var content = await response.Content.ReadAsStringAsync();

        // Debug: Log the validation response
        Console.WriteLine($"Validation Response Status: {response.StatusCode}");
        Console.WriteLine($"Validation Response Content: {content}");

        response.StatusCode.Should().Be(HttpStatusCode.OK, $"Token validation failed. Response: {content}");

        var userDto = JsonSerializer.Deserialize<UserDto>(content, JsonOptions);

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
        await ClearDatabaseAsync();
        Client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "invalid-token");

        // Act
        var response = await Client.GetAsync("/api/auth/validate");

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            $"Expected Unauthorized for invalid token but got {response.StatusCode}. Response: {content}");
    }
}