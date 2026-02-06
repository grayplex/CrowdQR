using System.Net;
using System.Net.Http.Json;
using CrowdQR.Api.Models;
using CrowdQR.Shared.Models.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace CrowdQR.Api.Tests.Integration;

/// <summary>
/// Integration tests verifying query count budgets for critical API endpoints.
/// These tests detect N+1 regressions and ensure query optimizations remain effective.
/// </summary>
/// <remarks>
/// Query budget thresholds per endpoint type:
/// - Simple GET by ID: 1 query
/// - GET with related data: 1-2 queries
/// - Dashboard summary: 2-3 queries
/// - Report generation: 2-3 queries
/// </remarks>
public class QueryCountTests : BaseIntegrationTest
{
    /// <summary>
    /// Configures test authentication for query count tests.
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
    /// Sets the authentication header for the HTTP client.
    /// </summary>
    /// <param name="authValue">The authentication value.</param>
    private void SetAuthenticationHeader(string authValue)
    {
        Client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Test", authValue);
    }

    /// <summary>
    /// Seeds database with data that would expose N+1 patterns if projections fail.
    /// Creates 1 event with 5 requests, each having 2+ votes.
    /// </summary>
    private async Task SeedQueryCountTestDataAsync()
    {
        await SeedDatabaseAsync(context =>
        {
            // Create DJ user
            var djUser = new User
            {
                UserId = 1,
                Username = "test_dj",
                Email = "dj@test.com",
                Role = UserRole.DJ,
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow
            };

            // Create audience users
            var user2 = new User
            {
                UserId = 2,
                Username = "audience1",
                Role = UserRole.Audience,
                CreatedAt = DateTime.UtcNow
            };

            var user3 = new User
            {
                UserId = 3,
                Username = "audience2",
                Role = UserRole.Audience,
                CreatedAt = DateTime.UtcNow
            };

            var user4 = new User
            {
                UserId = 4,
                Username = "audience3",
                Role = UserRole.Audience,
                CreatedAt = DateTime.UtcNow
            };

            context.Users.AddRange(djUser, user2, user3, user4);

            // Create event
            var testEvent = new Event
            {
                EventId = 1,
                DjUserId = 1,
                Name = "Query Count Test Event",
                Slug = "query-test",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            context.Events.Add(testEvent);

            // Create 5 requests
            var requests = new[]
            {
                new Request
                {
                    RequestId = 1,
                    UserId = 2,
                    EventId = 1,
                    SongName = "Song 1",
                    ArtistName = "Artist 1",
                    Status = RequestStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                },
                new Request
                {
                    RequestId = 2,
                    UserId = 3,
                    EventId = 1,
                    SongName = "Song 2",
                    ArtistName = "Artist 2",
                    Status = RequestStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                },
                new Request
                {
                    RequestId = 3,
                    UserId = 4,
                    EventId = 1,
                    SongName = "Song 3",
                    ArtistName = "Artist 3",
                    Status = RequestStatus.Approved,
                    CreatedAt = DateTime.UtcNow
                },
                new Request
                {
                    RequestId = 4,
                    UserId = 2,
                    EventId = 1,
                    SongName = "Song 4",
                    ArtistName = "Artist 4",
                    Status = RequestStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                },
                new Request
                {
                    RequestId = 5,
                    UserId = 3,
                    EventId = 1,
                    SongName = "Song 5",
                    ArtistName = "Artist 5",
                    Status = RequestStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                }
            };

            context.Requests.AddRange(requests);

            // Create votes (2-3 per request to expose N+1 if not optimized)
            var votes = new[]
            {
                // Request 1: 3 votes
                new Vote { VoteId = 1, RequestId = 1, UserId = 2, CreatedAt = DateTime.UtcNow },
                new Vote { VoteId = 2, RequestId = 1, UserId = 3, CreatedAt = DateTime.UtcNow },
                new Vote { VoteId = 3, RequestId = 1, UserId = 4, CreatedAt = DateTime.UtcNow },
                // Request 2: 2 votes
                new Vote { VoteId = 4, RequestId = 2, UserId = 2, CreatedAt = DateTime.UtcNow },
                new Vote { VoteId = 5, RequestId = 2, UserId = 4, CreatedAt = DateTime.UtcNow },
                // Request 3: 3 votes
                new Vote { VoteId = 6, RequestId = 3, UserId = 2, CreatedAt = DateTime.UtcNow },
                new Vote { VoteId = 7, RequestId = 3, UserId = 3, CreatedAt = DateTime.UtcNow },
                new Vote { VoteId = 8, RequestId = 3, UserId = 4, CreatedAt = DateTime.UtcNow },
                // Request 4: 2 votes
                new Vote { VoteId = 9, RequestId = 4, UserId = 3, CreatedAt = DateTime.UtcNow },
                new Vote { VoteId = 10, RequestId = 4, UserId = 4, CreatedAt = DateTime.UtcNow },
                // Request 5: 1 vote
                new Vote { VoteId = 11, RequestId = 5, UserId = 2, CreatedAt = DateTime.UtcNow }
            };

            context.Votes.AddRange(votes);

            // Create sessions for dashboard tests
            var sessions = new[]
            {
                new Session
                {
                    SessionId = 1,
                    UserId = 2,
                    EventId = 1,
                    ClientIP = "127.0.0.1",
                    LastSeen = DateTime.UtcNow,
                    RequestCount = 2
                },
                new Session
                {
                    SessionId = 2,
                    UserId = 3,
                    EventId = 1,
                    ClientIP = "127.0.0.2",
                    LastSeen = DateTime.UtcNow,
                    RequestCount = 1
                }
            };

            context.Sessions.AddRange(sessions);

            context.SaveChanges();
        });
    }

    /// <summary>
    /// Verifies GetEvent endpoint uses 1-2 queries regardless of number of requests/votes.
    /// </summary>
    [Fact]
    public async Task GetEvent_WithRequestsAndVotes_StaysUnderQueryBudget()
    {
        // Arrange: Seed an event with 5 requests and 11 votes (would be 12+ queries if N+1)
        await ClearDatabaseAsync();
        await SeedQueryCountTestDataAsync();
        ResetQueryCount();

        // Act: Call GET /api/event/1
        var response = await Client.GetAsync("/api/event/1");

        // Assert: Query count under budget (1-2 queries for event + requests with vote counts)
        var queryCount = GetQueryCount();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        queryCount.Should().BeLessThanOrEqualTo(2,
            $"GetEvent should use 1-2 queries (projection with subquery), but used {queryCount}. " +
            "This suggests N+1 pattern reintroduced - check for .Include().ThenInclude() instead of projections.");
    }

    /// <summary>
    /// Verifies GetRequestsByEvent endpoint uses 1-2 queries with vote counting.
    /// </summary>
    [Fact]
    public async Task GetRequestsByEvent_WithVotes_StaysUnderQueryBudget()
    {
        // Arrange
        await ClearDatabaseAsync();
        await SeedQueryCountTestDataAsync();
        ResetQueryCount();

        // Act
        var response = await Client.GetAsync("/api/request/event/1");

        // Assert
        var queryCount = GetQueryCount();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        queryCount.Should().BeLessThanOrEqualTo(2,
            $"GetRequestsByEvent should use 1-2 queries, but used {queryCount}. " +
            "Check for N+1 pattern in vote counting.");
    }

    /// <summary>
    /// Verifies dashboard GetEventSummary uses 2-3 queries for requests and sessions.
    /// </summary>
    [Fact]
    public async Task GetEventSummary_WithRequestsAndSessions_StaysUnderQueryBudget()
    {
        // Arrange
        await ClearDatabaseAsync();
        await SeedQueryCountTestDataAsync();
        SetAuthenticationHeader("user-1-dj");
        ResetQueryCount();

        // Act
        var response = await Client.GetAsync("/api/dashboard/event/1/summary");

        // Assert: Dashboard summary uses 2-3 queries (exists check + requests + sessions)
        var queryCount = GetQueryCount();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        queryCount.Should().BeLessThanOrEqualTo(3,
            $"GetEventSummary should use 2-3 queries, but used {queryCount}. " +
            "Check for cartesian explosion or N+1 patterns.");
    }

    /// <summary>
    /// Verifies GetEvents list endpoint uses 1 query with projection.
    /// </summary>
    [Fact]
    public async Task GetEvents_ListEndpoint_StaysUnderQueryBudget()
    {
        // Arrange
        await ClearDatabaseAsync();
        await SeedQueryCountTestDataAsync();
        ResetQueryCount();

        // Act
        var response = await Client.GetAsync("/api/event");

        // Assert: List endpoint should use 1 query (projection)
        var queryCount = GetQueryCount();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        queryCount.Should().BeLessThanOrEqualTo(1,
            $"GetEvents should use 1 query (projection), but used {queryCount}. " +
            "Check for missing AsNoTracking or redundant queries.");
    }

    /// <summary>
    /// Verifies GetVotesByRequest uses 1 query with AsNoTracking.
    /// </summary>
    [Fact]
    public async Task GetVotesByRequest_StaysUnderQueryBudget()
    {
        // Arrange
        await ClearDatabaseAsync();
        await SeedQueryCountTestDataAsync();
        SetAuthenticationHeader("user-1-dj");
        ResetQueryCount();

        // Act
        var response = await Client.GetAsync("/api/vote/request/1");

        // Assert
        var queryCount = GetQueryCount();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        queryCount.Should().BeLessThanOrEqualTo(1,
            $"GetVotesByRequest should use 1 query, but used {queryCount}. " +
            "Check for missing AsNoTracking.");
    }

    /// <summary>
    /// Verifies dashboard GetTopRequests uses 1-2 queries with server-side sorting.
    /// </summary>
    [Fact]
    public async Task GetTopRequests_StaysUnderQueryBudget()
    {
        // Arrange
        await ClearDatabaseAsync();
        await SeedQueryCountTestDataAsync();
        SetAuthenticationHeader("user-1-dj");
        ResetQueryCount();

        // Act
        var response = await Client.GetAsync("/api/dashboard/event/1/top-requests?status=Pending&limit=10");

        // Assert
        var queryCount = GetQueryCount();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        queryCount.Should().BeLessThanOrEqualTo(2,
            $"GetTopRequests should use 1-2 queries, but used {queryCount}. " +
            "Check for N+1 in vote counting.");
    }

    /// <summary>
    /// Verifies dashboard GetDJEventStats uses 1-2 queries with SQL aggregation.
    /// </summary>
    [Fact]
    public async Task GetDJEventStats_StaysUnderQueryBudget()
    {
        // Arrange
        await ClearDatabaseAsync();
        await SeedQueryCountTestDataAsync();
        SetAuthenticationHeader("user-1-dj");
        ResetQueryCount();

        // Act
        var response = await Client.GetAsync("/api/dashboard/dj/1/event-stats");

        // Assert
        var queryCount = GetQueryCount();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        queryCount.Should().BeLessThanOrEqualTo(2,
            $"GetDJEventStats should use 1-2 queries, but used {queryCount}. " +
            "Check for missing projections or split queries.");
    }
}
