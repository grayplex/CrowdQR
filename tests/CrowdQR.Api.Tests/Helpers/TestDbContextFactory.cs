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
    /// Creates an in-memory database context for testing.
    /// </summary>
    /// <param name="databaseName">The name of the in-memory database.</param>
    /// <returns>A configured CrowdQRContext using in-memory database.</returns>
    public static CrowdQRContext CreateInMemoryContext(string? databaseName = null)
    {
        var dbName = databaseName ?? $"TestDb_{Guid.NewGuid()}";

        var options = new DbContextOptionsBuilder<CrowdQRContext>()
            .UseInMemoryDatabase(dbName)
            .EnableSensitiveDataLogging()
            .Options;

        return new CrowdQRContext(options);
    }

    /// <summary>
    /// Creates an in-memory database context with seeded test data.
    /// </summary>
    /// <param name="databaseName">The name of the in-memory database.</param>
    /// <returns>A configured CrowdQRContext with test data.</returns>
    public static CrowdQRContext CreateInMemoryContextWithData(string? databaseName = null)
    {
        var context = CreateInMemoryContext(databaseName);
        SeedTestData(context);
        return context;
    }

    /// <summary>
    /// Seeds the database with test data.
    /// </summary>
    /// <param name="context">The database context to seed.</param>
    public static void SeedTestData(CrowdQRContext context)
    {
        // Ensure the database is created
        context.Database.EnsureCreated();

        // Clear existing data first (for in-memory database, check if any exists)
        if (context.Votes.Any()) context.RemoveRange(context.Votes);
        if (context.Requests.Any()) context.RemoveRange(context.Requests);
        if (context.Sessions.Any()) context.RemoveRange(context.Sessions);
        if (context.Events.Any()) context.RemoveRange(context.Events);
        if (context.Users.Any()) context.RemoveRange(context.Users);

        context.SaveChanges();

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

        context.SaveChanges();
    }

    /// <summary>
    /// Async version for backward compatibility.
    /// </summary>
    /// <param name="context">The database context to seed.</param>
    public static async Task SeedTestDataAsync(CrowdQRContext context)
    {
        SeedTestData(context);
        await Task.CompletedTask; // For async compatibility
    }

    /// <summary>
    /// Creates a fresh in-memory context and seeds it with test data.
    /// </summary>
    /// <param name="databaseName">Optional database name for the in-memory database.</param>
    /// <returns>A seeded database context.</returns>
    public static CrowdQRContext CreateSeededContext(string? databaseName = null)
    {
        var context = CreateInMemoryContext(databaseName);
        SeedTestData(context);
        return context;
    }
}
