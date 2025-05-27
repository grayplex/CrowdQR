using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using CrowdQR.Api.Data;
using CrowdQR.Api.Tests.Helpers;
using CrowdQR.Shared.Models.DTOs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;

namespace CrowdQR.Api.Tests.Integration;

/// <summary>
/// Integration tests for vote-related API endpoints.
/// </summary>
public class VoteIntegrationTests : IClassFixture<WebApplicationFactory<CrowdQR.Api.Program>>
{
    private readonly WebApplicationFactory<CrowdQR.Api.Program> _factory;
    private readonly HttpClient _client;
    private static readonly string DatabaseName = "VoteTestDb_" + Guid.NewGuid();

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="VoteIntegrationTests"/> class.
    /// </summary>
    /// <param name="factory">The web application factory.</param>
    public VoteIntegrationTests(WebApplicationFactory<CrowdQR.Api.Program> factory)
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

                // Remove existing authentication and add test authentication
                var authDescriptors = services.Where(d =>
                    d.ServiceType.Namespace != null &&
                    d.ServiceType.Namespace.StartsWith("Microsoft.AspNetCore.Authentication")).ToList();

                foreach (var descriptor in authDescriptors)
                {
                    services.Remove(descriptor);
                }

                // Add test authentication
                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                        "Test", options => { });

                // Override the default authentication scheme
                services.Configure<AuthenticationOptions>(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                    options.DefaultScheme = "Test";
                });
            });
        });

        _client = _factory.CreateClient();
    }

    /// <summary>
    /// Tests the complete vote creation flow.
    /// </summary>
    [Fact]
    public async Task CreateVote_EndToEndFlow_Success()
    {
        // Arrange
        await SeedDatabase();
        await VerifyDatabaseState("CreateVote_EndToEndFlow_Success");
        SetAuthenticationHeader("user-2-audience");

        var voteDto = new VoteCreateDto
        {
            UserId = 2,
            RequestId = 1
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/vote", voteDto);

        // Assert
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Created,
            $"Expected Created but got {response.StatusCode}. Response: {content}");

        // Verify vote was created in database
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrowdQRContext>();
        var createdVote = await context.Votes.FirstOrDefaultAsync(v => v.UserId == 2 && v.RequestId == 1);
        createdVote.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that duplicate votes are prevented.
    /// </summary>
    [Fact]
    public async Task CreateVote_DuplicateVote_ReturnsConflict()
    {
        // Arrange
        await SeedDatabase();
        await VerifyDatabaseState("CreateVote_DuplicateVote_ReturnsConflict");
        SetAuthenticationHeader("user-2-audience");

        var voteDto = new VoteCreateDto
        {
            UserId = 2,
            RequestId = 1
        };

        // Create first vote
        var firstResponse = await _client.PostAsJsonAsync("/api/vote", voteDto);
        var firstContent = await firstResponse.Content.ReadAsStringAsync();

        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created,
            $"Expected Created for first vote but got {firstResponse.StatusCode}. Response: {firstContent}");

        // Act - Try to create duplicate vote
        var secondResponse = await _client.PostAsJsonAsync("/api/vote", voteDto);

        // Assert
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    /// <summary>
    /// Tests authorization for vote creation without authentication header.
    /// </summary>
    [Fact]
    public async Task CreateVote_Unauthorized_ReturnsUnauthorizedOrForbidden()
    {
        // Arrange
        await SeedDatabase();
        // Clear any existing authorization header to ensure no authentication
        _client.DefaultRequestHeaders.Authorization = null;

        var voteDto = new VoteCreateDto
        {
            UserId = 2,
            RequestId = 1
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/vote", voteDto);

        // Assert
        var content = await response.Content.ReadAsStringAsync();

        // The VoteController's CreateVote method doesn't have [Authorize] attribute,
        // but middleware or controller logic may still return 403 Forbidden for unauthenticated requests.
        // Both 401 and 403 indicate authentication/authorization failure, which is what we want to test.
        response.StatusCode.Should().BeOneOf([HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden],
            $"Expected authentication/authorization failure (401 or 403) but got {response.StatusCode}. Response: {content}");
    }

    /// <summary>
    /// Tests that users cannot vote for others (non-DJ).
    /// </summary>
    [Fact]
    public async Task CreateVote_UserVotingForAnother_ReturnsForbid()
    {
        // Arrange
        await SeedDatabase();
        SetAuthenticationHeader("user-2-audience");

        var voteDto = new VoteCreateDto
        {
            UserId = 3, // Different user
            RequestId = 1
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/vote", voteDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// Tests DJ can vote for other users.
    /// </summary>
    [Fact]
    public async Task CreateVote_DjVotingForAnother_Success()
    {
        // Arrange
        await SeedDatabase();
        await VerifyDatabaseState("CreateVote_DjVotingForAnother_Success");
        SetAuthenticationHeader("user-1-dj");

        var voteDto = new VoteCreateDto
        {
            UserId = 2, // Different user
            RequestId = 1
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/vote", voteDto);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            $"Expected Created but got {response.StatusCode}. Response: {content}");
    }

    /// <summary>
    /// Tests getting votes for a request.
    /// </summary>
    [Fact]
    public async Task GetVotesByRequest_WithExistingVotes_ReturnsVotes()
    {
        // Arrange
        await SeedDatabase();

        // Create some votes first
        SetAuthenticationHeader("user-2-audience");
        var vote1Response = await _client.PostAsJsonAsync("/api/vote", new VoteCreateDto { UserId = 2, RequestId = 1 });
        vote1Response.StatusCode.Should().Be(HttpStatusCode.Created, "First vote should be created");

        SetAuthenticationHeader("user-3-audience");
        var vote2Response = await _client.PostAsJsonAsync("/api/vote", new VoteCreateDto { UserId = 3, RequestId = 1 });
        vote2Response.StatusCode.Should().Be(HttpStatusCode.Created, "Second vote should be created");

        // Act
        SetAuthenticationHeader("user-2-audience"); // Reset auth for GET request
        var response = await _client.GetAsync("/api/vote/request/1");

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, $"GET request failed. Response: {content}");

        var votes = JsonSerializer.Deserialize<JsonElement[]>(content, _jsonOptions);
        votes.Should().HaveCount(2, $"Expected 2 votes but got response: {content}");
    }

    /// <summary>
    /// Tests deleting a vote.
    /// </summary>
    [Fact]
    public async Task DeleteVote_ValidRequest_ReturnsNoContent()
    {
        // Arrange
        await SeedDatabase();
        SetAuthenticationHeader("user-2-audience");

        // Create vote first
        var voteDto = new VoteCreateDto { UserId = 2, RequestId = 1 };
        var createResponse = await _client.PostAsJsonAsync("/api/vote", voteDto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created, "Vote should be created before deletion");

        // Act
        var response = await _client.DeleteAsync("/api/vote/user/2/request/1");

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.NoContent,
            $"Expected NoContent but got {response.StatusCode}. Response: {content}");

        // Verify vote was deleted
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrowdQRContext>();
        var deletedVote = await context.Votes.FirstOrDefaultAsync(v => v.UserId == 2 && v.RequestId == 1);
        deletedVote.Should().BeNull();
    }

    /// <summary>
    /// Tests input validation for vote creation with non-existent request.
    /// </summary>
    [Fact]
    public async Task CreateVote_NonExistentRequest_ReturnsBadRequest()
    {
        // Arrange
        await SeedDatabase();
        SetAuthenticationHeader("user-2-audience");

        var invalidVoteDto = new VoteCreateDto
        {
            UserId = 2,
            RequestId = 999 // Non-existent request
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/vote", invalidVoteDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Tests input validation for vote creation with non-existent user.
    /// </summary>
    [Fact]
    public async Task CreateVote_NonExistentUser_ReturnsBadRequest()
    {
        // Arrange
        await SeedDatabase();
        SetAuthenticationHeader("user-999-audience"); // This user won't exist in DB

        var invalidVoteDto = new VoteCreateDto
        {
            UserId = 999, // Non-existent user
            RequestId = 1
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/vote", invalidVoteDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Tests getting all votes as DJ.
    /// </summary>
    [Fact]
    public async Task GetVotes_AsDj_ReturnsAllVotes()
    {
        // Arrange
        await SeedDatabase();

        // Create some votes
        SetAuthenticationHeader("user-2-audience");
        var vote1Response = await _client.PostAsJsonAsync("/api/vote", new VoteCreateDto { UserId = 2, RequestId = 1 });
        vote1Response.StatusCode.Should().Be(HttpStatusCode.Created, "First vote should be created");

        SetAuthenticationHeader("user-3-audience");
        var vote2Response = await _client.PostAsJsonAsync("/api/vote", new VoteCreateDto { UserId = 3, RequestId = 1 });
        vote2Response.StatusCode.Should().Be(HttpStatusCode.Created, "Second vote should be created");

        // Act as DJ
        SetAuthenticationHeader("user-1-dj");
        var response = await _client.GetAsync("/api/vote");

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, $"GET all votes failed. Response: {content}");

        var votes = JsonSerializer.Deserialize<JsonElement[]>(content, _jsonOptions);
        votes.Should().HaveCount(2, $"Expected 2 votes but got response: {content}");
    }

    /// <summary>
    /// Tests that non-DJ users cannot get all votes.
    /// </summary>
    [Fact]
    public async Task GetVotes_AsAudience_ReturnsForbidden()
    {
        // Arrange
        await SeedDatabase();
        SetAuthenticationHeader("user-2-audience");

        // Act
        var response = await _client.GetAsync("/api/vote");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// Sets the authentication header for the HTTP client.
    /// </summary>
    /// <param name="authValue">The authentication value.</param>
    private void SetAuthenticationHeader(string authValue)
    {
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Test", authValue);
    }

    /// <summary>
    /// Seeds the test database with initial data using a fresh context.
    /// </summary>
    private static async Task SeedDatabase()
    {
        // Create a fresh context with the same database name to seed data
        var options = new DbContextOptionsBuilder<CrowdQRContext>()
            .UseInMemoryDatabase(DatabaseName)
            .EnableSensitiveDataLogging()
            .Options;

        await using var context = new CrowdQRContext(options);

        // Ensure the database is created
        await context.Database.EnsureCreatedAsync();

        // Clear existing data
        if (context.Votes.Any()) context.Votes.RemoveRange(context.Votes);
        if (context.Requests.Any()) context.Requests.RemoveRange(context.Requests);
        if (context.Sessions.Any()) context.Sessions.RemoveRange(context.Sessions);
        if (context.Events.Any()) context.Events.RemoveRange(context.Events);
        if (context.Users.Any()) context.Users.RemoveRange(context.Users);
        await context.SaveChangesAsync();

        // Seed fresh test data
        await TestDbContextFactory.SeedTestDataAsync(context);

        Console.WriteLine("Database seeding completed successfully.");
    }

    /// <summary>
    /// Verifies the database state for debugging purposes using a fresh context.
    /// </summary>
    /// <param name="testName">The name of the test for debugging.</param>
    private static async Task VerifyDatabaseState(string testName)
    {
        // Create a fresh context with the same database name to verify data
        var options = new DbContextOptionsBuilder<CrowdQRContext>()
            .UseInMemoryDatabase(DatabaseName)
            .EnableSensitiveDataLogging()
            .Options;

        await using var context = new CrowdQRContext(options);

        var userCount = await context.Users.CountAsync();
        var eventCount = await context.Events.CountAsync();
        var requestCount = await context.Requests.CountAsync();

        // Log counts for debugging
        Console.WriteLine($"[{testName}] Database state - Users: {userCount}, Events: {eventCount}, Requests: {requestCount}");

        if (requestCount > 0)
        {
            var requests = await context.Requests.ToListAsync();
            foreach (var req in requests)
            {
                Console.WriteLine($"[{testName}] Request ID: {req.RequestId}, UserId: {req.UserId}, EventId: {req.EventId}, Song: {req.SongName}");
            }
        }

        // Basic assertion to ensure data exists
        userCount.Should().BeGreaterThan(0, "Users should exist in database");
        eventCount.Should().BeGreaterThan(0, "Events should exist in database");
        requestCount.Should().BeGreaterThan(0, "Requests should exist in database");
    }
}

/// <summary>
/// Test authentication handler for integration tests.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="TestAuthenticationHandler"/> class.
/// </remarks>
/// <param name="options">The options.</param>
/// <param name="logger">The logger.</param>
/// <param name="encoder">The URL encoder.</param>
public class TestAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    /// <summary>
    /// Handles the authentication.
    /// </summary>
    /// <returns>The authentication result.</returns>
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorizationHeader = Request.Headers.Authorization.FirstOrDefault();

        if (authorizationHeader == null || !authorizationHeader.StartsWith("Test "))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing or invalid Authorization Header"));
        }

        var authValue = authorizationHeader["Test ".Length..];
        var claims = new List<Claim>();

        // Parse test authentication values
        switch (authValue)
        {
            case "user-1-dj":
                claims.Add(new Claim(ClaimTypes.NameIdentifier, "1"));
                claims.Add(new Claim(ClaimTypes.Role, "DJ"));
                claims.Add(new Claim(ClaimTypes.Name, "test_dj"));
                claims.Add(new Claim(ClaimTypes.Email, "dj@test.com"));
                claims.Add(new Claim("email_verified", "true"));
                break;
            case "user-2-audience":
                claims.Add(new Claim(ClaimTypes.NameIdentifier, "2"));
                claims.Add(new Claim(ClaimTypes.Role, "Audience"));
                claims.Add(new Claim(ClaimTypes.Name, "audience1"));
                break;
            case "user-3-audience":
                claims.Add(new Claim(ClaimTypes.NameIdentifier, "3"));
                claims.Add(new Claim(ClaimTypes.Role, "Audience"));
                claims.Add(new Claim(ClaimTypes.Name, "audience2"));
                break;
            case "user-999-audience":
                claims.Add(new Claim(ClaimTypes.NameIdentifier, "999"));
                claims.Add(new Claim(ClaimTypes.Role, "Audience"));
                claims.Add(new Claim(ClaimTypes.Name, "nonexistent"));
                break;
            default:
                return Task.FromResult(AuthenticateResult.Fail("Invalid test user"));
        }

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}