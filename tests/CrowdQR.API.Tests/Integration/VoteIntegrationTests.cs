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
using Microsoft.Extensions.Internal;

namespace CrowdQR.Api.Tests.Integration;

/// <summary>
/// Integration tests for vote-related API endpoints.
/// </summary>
public class VoteIntegrationTests : IClassFixture<WebApplicationFactory<CrowdQR.Api.Program>>
{
    private readonly WebApplicationFactory<CrowdQR.Api.Program> _factory;
    private readonly HttpClient _client;

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
                // Remove ALL existing DbContext registrations
                var dbContextDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<CrowdQRContext>));
                if (dbContextDescriptor != null)
                {
                    services.Remove(dbContextDescriptor);
                }

                var dbContextServiceDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(CrowdQRContext));
                if (dbContextServiceDescriptor != null)
                {
                    services.Remove(dbContextServiceDescriptor);
                }

                // Remove any other EF Core service registrations that might conflict
                var efCoreServices = services.Where(s => s.ServiceType.Namespace?.StartsWith("Microsoft.EntityFrameworkCore") == true).ToList();
                foreach (var service in efCoreServices)
                {
                    services.Remove(service);
                }

                // Add in-memory database for testing
                services.AddDbContext<CrowdQRContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb_" + Guid.NewGuid());
                });

                // Add test authentication
                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                        "Test", options => { });
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
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Test", "user-2-audience");

        var voteDto = new VoteCreateDto
        {
            UserId = 2,
            RequestId = 1
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/vote", voteDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeEmpty();

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
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Test", "user-2-audience");

        var voteDto = new VoteCreateDto
        {
            UserId = 2,
            RequestId = 1
        };

        // Create first vote
        var firstResponse = await _client.PostAsJsonAsync("/api/vote", voteDto);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act - Try to create duplicate vote
        var secondResponse = await _client.PostAsJsonAsync("/api/vote", voteDto);

        // Assert
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    /// <summary>
    /// Tests authorization for vote creation.
    /// </summary>
    [Fact]
    public async Task CreateVote_Unauthorized_ReturnsUnauthorized()
    {
        // Arrange
        await SeedDatabase();
        // No authorization header set

        var voteDto = new VoteCreateDto
        {
            UserId = 2,
            RequestId = 1
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/vote", voteDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Tests that users cannot vote for others (non-DJ).
    /// </summary>
    [Fact]
    public async Task CreateVote_UserVotingForAnother_ReturnsForbid()
    {
        // Arrange
        await SeedDatabase();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Test", "user-2-audience");

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
    /// Tests getting votes for a request.
    /// </summary>
    [Fact]
    public async Task GetVotesByRequest_WithExistingVotes_ReturnsVotes()
    {
        // Arrange
        await SeedDatabase();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Test", "user-2-audience");

        // Create some votes first
        var vote1 = new VoteCreateDto { UserId = 2, RequestId = 1 };
        await _client.PostAsJsonAsync("/api/vote", vote1);

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Test", "user-3-audience");
        var vote2 = new VoteCreateDto { UserId = 3, RequestId = 1 };
        await _client.PostAsJsonAsync("/api/vote", vote2);

        // Act
        var response = await _client.GetAsync("/api/vote/request/1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var votes = JsonSerializer.Deserialize<JsonElement[]>(content);
        votes.Should().HaveCount(2);
    }

    /// <summary>
    /// Tests deleting a vote.
    /// </summary>
    [Fact]
    public async Task DeleteVote_ValidRequest_ReturnsNoContent()
    {
        // Arrange
        await SeedDatabase();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Test", "user-2-audience");

        // Create vote first
        var voteDto = new VoteCreateDto { UserId = 2, RequestId = 1 };
        await _client.PostAsJsonAsync("/api/vote", voteDto);

        // Act
        var response = await _client.DeleteAsync("/api/vote/user/2/request/1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify vote was deleted
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrowdQRContext>();
        var deletedVote = await context.Votes.FirstOrDefaultAsync(v => v.UserId == 2 && v.RequestId == 1);
        deletedVote.Should().BeNull();
    }

    /// <summary>
    /// Tests input validation for vote creation.
    /// </summary>
    [Fact]
    public async Task CreateVote_InvalidInput_ReturnsBadRequest()
    {
        // Arrange
        await SeedDatabase();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Test", "user-2-audience");

        var invalidVoteDto = new VoteCreateDto
        {
            UserId = 0, // Invalid user ID
            RequestId = 1
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/vote", invalidVoteDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Seeds the test database with initial data.
    /// </summary>
    private async Task SeedDatabase()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrowdQRContext>();
        await TestDbContextFactory.SeedTestDataAsync(context);
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

        if (authorizationHeader == null)
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing Authorization Header"));
        }

        var authValue = authorizationHeader.Split(' ').Last();
        var claims = new List<Claim>();

        // Parse test authentication values
        switch (authValue)
        {
            case "user-1-dj":
                claims.Add(new Claim(ClaimTypes.NameIdentifier, "1"));
                claims.Add(new Claim(ClaimTypes.Role, "DJ"));
                claims.Add(new Claim(ClaimTypes.Name, "test_dj"));
                claims.Add(new Claim(ClaimTypes.Email, "dj@test.com"));
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
            default:
                return Task.FromResult(AuthenticateResult.Fail("Invalid test user"));
        }

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}