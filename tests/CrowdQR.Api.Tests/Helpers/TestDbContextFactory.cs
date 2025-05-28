using CrowdQR.Api.Data;
using CrowdQR.Api.Models;
using CrowdQR.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace CrowdQR.Api.Tests.Helpers;

/// <summary>
/// Factory for creating test database contexts with in-memory database.
/// </summary>
public static class TestDbContextFactory
{
    /// <summary>
    /// Creates a new in-memory database context for testing.
    /// </summary>
    /// <param name="databaseName">Optional database name. If null, a unique name is generated.</param>
    /// <returns>A configured test database context.</returns>
    public static CrowdQRContext CreateInMemoryContext(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<CrowdQRContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .EnableSensitiveDataLogging()
            .Options;

        return new CrowdQRContext(options);
    }

    /// <summary>
    /// Seeds the database with test data.
    /// </summary>
    /// <param name="context">The database context to seed.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task SeedTestDataAsync(CrowdQRContext context)
    {
        // Clear existing data
        context.RemoveRange(context.Votes);
        context.RemoveRange(context.Requests);
        context.RemoveRange(context.Sessions);
        context.RemoveRange(context.Events);
        context.RemoveRange(context.Users);
        await context.SaveChangesAsync();

        // Add test users
        var djUser = new User
        {
            UserId = 1,
            Username = "test_dj",
            Email = "dj@test.com",
            Role = UserRole.DJ,
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };

        var audienceUser1 = new User
        {
            UserId = 2,
            Username = "audience1",
            Role = UserRole.Audience,
            CreatedAt = DateTime.UtcNow
        };

        var audienceUser2 = new User
        {
            UserId = 3,
            Username = "audience2",
            Role = UserRole.Audience,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.AddRange(djUser, audienceUser1, audienceUser2);

        // Add test event
        var testEvent = new Event
        {
            EventId = 1,
            DjUserId = 1,
            Name = "Test Event",
            Slug = "test-event",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Events.Add(testEvent);

        // Add test requests
        var request1 = new Request
        {
            RequestId = 1,
            UserId = 2,
            EventId = 1,
            SongName = "Test Song 1",
            ArtistName = "Test Artist 1",
            Status = RequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        var request2 = new Request
        {
            RequestId = 2,
            UserId = 3,
            EventId = 1,
            SongName = "Test Song 2",
            ArtistName = "Test Artist 2",
            Status = RequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        context.Requests.AddRange(request1, request2);

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Creates a context with seeded test data.
    /// </summary>
    /// <param name="databaseName">Optional database name.</param>
    /// <returns>A seeded test database context.</returns>
    public static async Task<CrowdQRContext> CreateSeededContextAsync(string? databaseName = null)
    {
        var context = CreateInMemoryContext(databaseName);
        await SeedTestDataAsync(context);
        return context;
    }
}
