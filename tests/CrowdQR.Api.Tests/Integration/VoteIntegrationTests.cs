using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CrowdQR.Shared.Models.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using System.Security.Claims;
using CrowdQR.Api.Data;
using CrowdQR.Api.Tests.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;

namespace CrowdQR.Api.Tests.Integration;

/// <summary>
/// Integration tests for vote-related API endpoints.
/// </summary>
public class VoteIntegrationTests : BaseIntegrationTest
{
    /// <summary>
    /// Configures test authentication for vote tests.
    /// </summary>
    /// <param name="services">The service collection.</param>
    protected override void ConfigureTestAuthentication(IServiceCollection services)
    {
        // Remove existing authentication services
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
    }

    /// <summary>
    /// Tests the complete vote creation flow.
    /// </summary>
    [Fact]
    public async Task CreateVote_EndToEndFlow_Success()
    {
        // Arrange
        await ClearDatabaseAsync();
        await SeedStandardTestDataAsync();
        SetAuthenticationHeader("user-2-audience");

        var voteDto = new VoteCreateDto
        {
            UserId = 2,
            RequestId = 1
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/vote", voteDto);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            $"Expected Created but got {response.StatusCode}. Response: {content}");

        // Verify vote was created in database
        using var context = GetDbContext();
        var createdVote = await context.Votes.FirstOrDefaultAsync(v => v.UserId == 2 && v.RequestId == 1);
        createdVote.Should().NotBeNull();
    }

    /// <summary>
    /// Sets the authentication header for the HTTP client.
    /// </summary>
    /// <param name="authValue">The authentication value.</param>
    private void SetAuthenticationHeader(string authValue)
    {
        Client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Test", authValue);
    }

    /// <summary>
    /// Tests that duplicate votes are prevented.
    /// </summary>
    [Fact]
    public async Task CreateVote_DuplicateVote_ReturnsConflict()
    {
        // Arrange
        await ClearDatabaseAsync();
        await SeedStandardTestDataAsync();
        SetAuthenticationHeader("user-2-audience");

        var voteDto = new VoteCreateDto
        {
            UserId = 2,
            RequestId = 1
        };

        // Create first vote
        var firstResponse = await Client.PostAsJsonAsync("/api/vote", voteDto);
        var firstContent = await firstResponse.Content.ReadAsStringAsync();

        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created,
            $"Expected Created for first vote but got {firstResponse.StatusCode}. Response: {firstContent}");

        // Act - Try to create duplicate vote
        var secondResponse = await Client.PostAsJsonAsync("/api/vote", voteDto);

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
        await ClearDatabaseAsync();
        // Clear any existing authorization header to ensure no authentication
        Client.DefaultRequestHeaders.Authorization = null;

        var voteDto = new VoteCreateDto
        {
            UserId = 2,
            RequestId = 1
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/vote", voteDto);

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
        await ClearDatabaseAsync();
        SetAuthenticationHeader("user-2-audience");

        var voteDto = new VoteCreateDto
        {
            UserId = 3, // Different user
            RequestId = 1
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/vote", voteDto);

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
        await ClearDatabaseAsync();
        await SeedStandardTestDataAsync();
        SetAuthenticationHeader("user-1-dj");

        var voteDto = new VoteCreateDto
        {
            UserId = 2, // Different user
            RequestId = 1
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/vote", voteDto);

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
        await ClearDatabaseAsync();
        await SeedStandardTestDataAsync(); // Add this line!

        // Create some votes first
        SetAuthenticationHeader("user-2-audience");
        var vote1Response = await Client.PostAsJsonAsync("/api/vote", new VoteCreateDto { UserId = 2, RequestId = 1 });
        vote1Response.StatusCode.Should().Be(HttpStatusCode.Created, "First vote should be created");

        SetAuthenticationHeader("user-3-audience");
        var vote2Response = await Client.PostAsJsonAsync("/api/vote", new VoteCreateDto { UserId = 3, RequestId = 1 });
        vote2Response.StatusCode.Should().Be(HttpStatusCode.Created, "Second vote should be created");

        // Act
        SetAuthenticationHeader("user-2-audience"); // Reset auth for GET request
        var response = await Client.GetAsync("/api/vote/request/1");

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, $"GET request failed. Response: {content}");

        var votes = JsonSerializer.Deserialize<JsonElement[]>(content, JsonOptions);
        votes.Should().HaveCount(2, $"Expected 2 votes but got response: {content}");
    }

    /// <summary>
    /// Tests deleting a vote.
    /// </summary>
    [Fact]
    public async Task DeleteVote_ValidRequest_ReturnsNoContent()
    {
        // Arrange
        await ClearDatabaseAsync();
        await SeedStandardTestDataAsync(); // Add this line!
        SetAuthenticationHeader("user-2-audience");

        // Create vote first
        var voteDto = new VoteCreateDto { UserId = 2, RequestId = 1 };
        var createResponse = await Client.PostAsJsonAsync("/api/vote", voteDto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created, "Vote should be created before deletion");

        // Act
        var response = await Client.DeleteAsync("/api/vote/user/2/request/1");

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.NoContent,
            $"Expected NoContent but got {response.StatusCode}. Response: {content}");

        // Verify vote was deleted
        using var context = GetDbContext();
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
        await ClearDatabaseAsync();
        SetAuthenticationHeader("user-2-audience");

        var invalidVoteDto = new VoteCreateDto
        {
            UserId = 2,
            RequestId = 999 // Non-existent request
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/vote", invalidVoteDto);

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
        await ClearDatabaseAsync();
        SetAuthenticationHeader("user-999-audience"); // This user won't exist in DB

        var invalidVoteDto = new VoteCreateDto
        {
            UserId = 999, // Non-existent user
            RequestId = 1
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/vote", invalidVoteDto);

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
        await ClearDatabaseAsync();
        await SeedStandardTestDataAsync(); // Add this line!

        // Create some votes
        SetAuthenticationHeader("user-2-audience");
        var vote1Response = await Client.PostAsJsonAsync("/api/vote", new VoteCreateDto { UserId = 2, RequestId = 1 });
        vote1Response.StatusCode.Should().Be(HttpStatusCode.Created, "First vote should be created");

        SetAuthenticationHeader("user-3-audience");
        var vote2Response = await Client.PostAsJsonAsync("/api/vote", new VoteCreateDto { UserId = 3, RequestId = 1 });
        vote2Response.StatusCode.Should().Be(HttpStatusCode.Created, "Second vote should be created");

        // Act as DJ
        SetAuthenticationHeader("user-1-dj");
        var response = await Client.GetAsync("/api/vote");

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, $"GET all votes failed. Response: {content}");

        var votes = JsonSerializer.Deserialize<JsonElement[]>(content, JsonOptions);
        votes.Should().HaveCount(2, $"Expected 2 votes but got response: {content}");
    }

    /// <summary>
    /// Tests that non-DJ users cannot get all votes.
    /// </summary>
    [Fact]
    public async Task GetVotes_AsAudience_ReturnsForbidden()
    {
        // Arrange
        await ClearDatabaseAsync();
        SetAuthenticationHeader("user-2-audience");

        // Act
        var response = await Client.GetAsync("/api/vote");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}