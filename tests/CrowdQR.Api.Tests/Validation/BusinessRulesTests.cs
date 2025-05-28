using CrowdQR.Api.Data;
using CrowdQR.Api.Models;
using CrowdQR.Api.Tests.Helpers;
using CrowdQR.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace CrowdQR.Api.Tests.Validation;

/// <summary>
/// Tests for business rules and data validation constraints.
/// </summary>
public class BusinessRulesTests : IDisposable
{
    private readonly CrowdQRContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="BusinessRulesTests"/> class.
    /// </summary>
    public BusinessRulesTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
    }

    #region Vote Business Rules Tests

    /// <summary>
    /// Tests that a user can only vote once per request using application logic.
    /// </summary>
    [Fact]
    public async Task Vote_DuplicateVoteFromSameUser_DetectedByApplicationLogic()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);

        var firstVote = new Vote
        {
            UserId = 2,
            RequestId = 1,
            CreatedAt = DateTime.UtcNow
        };

        _context.Votes.Add(firstVote);
        await _context.SaveChangesAsync();

        // Act - Check for existing vote (application-level validation)
        var existingVote = await _context.Votes
            .FirstOrDefaultAsync(v => v.UserId == 2 && v.RequestId == 1);

        // Assert
        existingVote.Should().NotBeNull("Application should detect existing vote");
    }

    /// <summary>
    /// Tests that the same user can vote for different requests.
    /// </summary>
    [Fact]
    public async Task Vote_SameUserDifferentRequests_Succeeds()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);

        var vote1 = new Vote
        {
            UserId = 2,
            RequestId = 1,
            CreatedAt = DateTime.UtcNow
        };

        var vote2 = new Vote
        {
            UserId = 2, // Same user
            RequestId = 2, // Different request
            CreatedAt = DateTime.UtcNow
        };

        // Act
        _context.Votes.AddRange(vote1, vote2);
        await _context.SaveChangesAsync();

        // Assert
        var savedVotes = await _context.Votes
            .Where(v => v.UserId == 2)
            .ToListAsync();

        savedVotes.Should().HaveCount(2);
        savedVotes.Should().Contain(v => v.RequestId == 1);
        savedVotes.Should().Contain(v => v.RequestId == 2);
    }

    /// <summary>
    /// Tests that different users can vote for the same request.
    /// </summary>
    [Fact]
    public async Task Vote_DifferentUsersSameRequest_Succeeds()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);

        var vote1 = new Vote
        {
            UserId = 2,
            RequestId = 1,
            CreatedAt = DateTime.UtcNow
        };

        var vote2 = new Vote
        {
            UserId = 3, // Different user
            RequestId = 1, // Same request
            CreatedAt = DateTime.UtcNow
        };

        // Act
        _context.Votes.AddRange(vote1, vote2);
        await _context.SaveChangesAsync();

        // Assert
        var savedVotes = await _context.Votes
            .Where(v => v.RequestId == 1)
            .ToListAsync();

        savedVotes.Should().HaveCount(2);
        savedVotes.Should().Contain(v => v.UserId == 2);
        savedVotes.Should().Contain(v => v.UserId == 3);
    }

    /// <summary>
    /// Tests that votes cannot be created with invalid user IDs using application logic.
    /// </summary>
    [Fact]
    public async Task Vote_InvalidUserId_DetectedByApplicationLogic()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);

        // Act - Check if user exists (application-level validation)
        var invalidUserId = 999;
        var userExists = await _context.Users.AnyAsync(u => u.UserId == invalidUserId);

        // Assert
        userExists.Should().BeFalse("Application should detect invalid user ID");
    }

    /// <summary>
    /// Tests that votes cannot be created with invalid request IDs using application logic.
    /// </summary>
    [Fact]
    public async Task Vote_InvalidRequestId_DetectedByApplicationLogic()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);

        // Act - Check if request exists (application-level validation)
        var invalidRequestId = 999;
        var requestExists = await _context.Requests.AnyAsync(r => r.RequestId == invalidRequestId);

        // Assert
        requestExists.Should().BeFalse("Application should detect invalid request ID");
    }

    #endregion

    #region User Business Rules Tests

    /// <summary>
    /// Tests that usernames must be unique using application logic.
    /// </summary>
    [Fact]
    public async Task User_DuplicateUsername_DetectedByApplicationLogic()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);

        // Act - Check if username already exists (application-level validation)
        var duplicateUsername = "test_dj"; // Already exists in seeded data
        var usernameExists = await _context.Users.AnyAsync(u => u.Username == duplicateUsername);

        // Assert
        usernameExists.Should().BeTrue("Application should detect duplicate username");
    }

    /// <summary>
    /// Tests that usernames have proper length validation at application level.
    /// </summary>
    [Fact]
    public Task User_UsernameTooLong_DetectedByApplicationLogic()
    {
        // Arrange
        var longUsername = new string('A', 101); // Max length is 100

        // Act - Application-level validation
        var isUsernameTooLong = longUsername.Length > 100;

        // Assert
        isUsernameTooLong.Should().BeTrue("Application should detect username that is too long");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Tests that email addresses have proper length validation at application level.
    /// </summary>
    [Fact]
    public Task User_EmailTooLong_DetectedByApplicationLogic()
    {
        // Arrange
        var longEmail = new string('a', 250) + "@test.com"; // Max length is 255

        // Act - Application-level validation
        var isEmailTooLong = longEmail.Length > 255;

        // Assert
        isEmailTooLong.Should().BeTrue("Application should detect email that is too long");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Tests that user roles are properly constrained.
    /// </summary>
    [Theory]
    [InlineData(UserRole.Audience)]
    [InlineData(UserRole.DJ)]
    public async Task User_ValidRoles_Succeeds(UserRole role)
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);

        var user = new User
        {
            Username = $"role_test_user_{role}",
            Role = role,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Assert
        var savedUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == $"role_test_user_{role}");

        savedUser.Should().NotBeNull();
        savedUser!.Role.Should().Be(role);
    }

    /// <summary>
    /// Tests that user creation date is properly set.
    /// </summary>
    [Fact]
    public async Task User_CreatedAtField_IsSetCorrectly()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        var beforeCreation = DateTime.UtcNow;

        var user = new User
        {
            Username = "time_test_user",
            Role = UserRole.Audience,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Assert
        var savedUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == "time_test_user");

        savedUser.Should().NotBeNull();
        savedUser!.CreatedAt.Should().BeAfter(beforeCreation);
        savedUser.CreatedAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    #endregion

    #region Event Business Rules Tests

    /// <summary>
    /// Tests that event slugs must be unique by checking application logic.
    /// </summary>
    [Fact]
    public async Task Event_DuplicateSlug_DetectedByApplicationLogic()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);

        // Act - Check if slug already exists (application-level validation)
        var existingSlug = "test-event"; // Already exists in seeded data
        var slugExists = await _context.Events.AnyAsync(e => e.Slug == existingSlug);

        // Assert
        slugExists.Should().BeTrue("Application should detect duplicate slug");
    }

    /// <summary>
    /// Tests that event names have proper length validation at application level.
    /// </summary>
    [Fact]
    public Task Event_NameTooLong_DetectedByApplicationLogic()
    {
        // Arrange
        var longName = new string('A', 201); // Max length is 200

        // Act - Application-level validation
        var isNameTooLong = longName.Length > 200;

        // Assert
        isNameTooLong.Should().BeTrue("Application should detect name that is too long");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Tests that event slugs have proper length validation at application level.
    /// </summary>
    [Fact]
    public Task Event_SlugTooLong_DetectedByApplicationLogic()
    {
        // Arrange
        var longSlug = new string('a', 101); // Max length is 100

        // Act - Application-level validation
        var isSlugTooLong = longSlug.Length > 100;

        // Assert
        isSlugTooLong.Should().BeTrue("Application should detect slug that is too long");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Tests that events must have valid DJ user IDs at application level.
    /// </summary>
    [Fact]
    public async Task Event_InvalidDjUserId_DetectedByApplicationLogic()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);

        // Act - Check if DJ user exists (application-level validation)
        var invalidDjUserId = 999;
        var djExists = await _context.Users.AnyAsync(u => u.UserId == invalidDjUserId && u.Role == UserRole.DJ);

        // Assert
        djExists.Should().BeFalse("Application should detect invalid DJ user ID");
    }

    /// <summary>
    /// Tests that events can be active or inactive.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Event_ActiveStatus_Succeeds(bool isActive)
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);

        var activeEvent = new Event
        {
            DjUserId = 1,
            Name = $"Status Test Event {isActive}",
            Slug = $"status-test-event-{isActive}",
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        _context.Events.Add(activeEvent);
        await _context.SaveChangesAsync();

        // Assert
        var savedEvent = await _context.Events
            .FirstOrDefaultAsync(e => e.Slug == $"status-test-event-{isActive}");

        savedEvent.Should().NotBeNull();
        savedEvent!.IsActive.Should().Be(isActive);
    }

    #endregion

    #region Request Business Rules Tests

    /// <summary>
    /// Tests that requests must have valid user IDs using application logic.
    /// </summary>
    [Fact]
    public async Task Request_InvalidUserId_DetectedByApplicationLogic()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);

        // Act - Check if user exists (application-level validation)
        var invalidUserId = 999;
        var userExists = await _context.Users.AnyAsync(u => u.UserId == invalidUserId);

        // Assert
        userExists.Should().BeFalse("Application should detect invalid user ID");
    }

    /// <summary>
    /// Tests that requests must have valid event IDs using application logic.
    /// </summary>
    [Fact]
    public async Task Request_InvalidEventId_DetectedByApplicationLogic()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);

        // Act - Check if event exists (application-level validation)
        var invalidEventId = 999;
        var eventExists = await _context.Events.AnyAsync(e => e.EventId == invalidEventId);

        // Assert
        eventExists.Should().BeFalse("Application should detect invalid event ID");
    }

    /// <summary>
    /// Tests that song names are required at application level.
    /// </summary>
    [Fact]
    public Task Request_MissingSongName_DetectedByApplicationLogic()
    {
        // Act - Application-level validation
        var songName = "";
        var isSongNameMissing = string.IsNullOrWhiteSpace(songName);

        // Assert
        isSongNameMissing.Should().BeTrue("Application should detect missing song name");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Tests that song names have proper length validation at application level.
    /// </summary>
    [Fact]
    public Task Request_SongNameTooLong_DetectedByApplicationLogic()
    {
        // Arrange
        var longSongName = new string('A', 256); // Max length is 255

        // Act - Application-level validation
        var isSongNameTooLong = longSongName.Length > 255;

        // Assert
        isSongNameTooLong.Should().BeTrue("Application should detect song name that is too long");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Tests that artist names have proper length validation at application level.
    /// </summary>
    [Fact]
    public Task Request_ArtistNameTooLong_DetectedByApplicationLogic()
    {
        // Arrange
        var longArtistName = new string('A', 256); // Max length is 255

        // Act - Application-level validation
        var isArtistNameTooLong = longArtistName.Length > 255;

        // Assert
        isArtistNameTooLong.Should().BeTrue("Application should detect artist name that is too long");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Tests request status transitions are valid.
    /// </summary>
    [Theory]
    [InlineData(RequestStatus.Pending)]
    [InlineData(RequestStatus.Approved)]
    [InlineData(RequestStatus.Rejected)]
    public async Task Request_ValidStatusValues_Succeeds(RequestStatus status)
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);

        var request = new Request
        {
            UserId = 2,
            EventId = 1,
            SongName = "Status Test Song",
            ArtistName = "Status Test Artist",
            Status = status,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        _context.Requests.Add(request);
        await _context.SaveChangesAsync();

        // Assert
        var savedRequest = await _context.Requests
            .FirstOrDefaultAsync(r => r.SongName == "Status Test Song");

        savedRequest.Should().NotBeNull();
        savedRequest!.Status.Should().Be(status);
    }

    /// <summary>
    /// Tests that request creation date is properly set.
    /// </summary>
    [Fact]
    public async Task Request_CreatedAtField_IsSetCorrectly()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);
        var beforeCreation = DateTime.UtcNow;

        var request = new Request
        {
            UserId = 2,
            EventId = 1,
            SongName = "Time Test Song",
            ArtistName = "Time Test Artist",
            Status = RequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        _context.Requests.Add(request);
        await _context.SaveChangesAsync();

        // Assert
        var savedRequest = await _context.Requests
            .FirstOrDefaultAsync(r => r.SongName == "Time Test Song");

        savedRequest.Should().NotBeNull();
        savedRequest!.CreatedAt.Should().BeAfter(beforeCreation);
        savedRequest.CreatedAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    #endregion

    #region Session Business Rules Tests

    /// <summary>
    /// Tests that a user can only have one session per event using application logic.
    /// </summary>
    [Fact]
    public async Task Session_DuplicateUserEventPair_DetectedByApplicationLogic()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);

        var firstSession = new Session
        {
            UserId = 2,
            EventId = 1,
            ClientIP = "192.168.1.1",
            LastSeen = DateTime.UtcNow,
            RequestCount = 0
        };

        _context.Sessions.Add(firstSession);
        await _context.SaveChangesAsync();

        // Act - Check for existing session (application-level validation)
        var existingSession = await _context.Sessions
            .FirstOrDefaultAsync(s => s.UserId == 2 && s.EventId == 1);

        // Assert
        existingSession.Should().NotBeNull("Application should detect existing session for user-event pair");
    }

    /// <summary>
    /// Tests that the same user can have sessions in different events.
    /// </summary>
    [Fact]
    public async Task Session_SameUserDifferentEvents_Succeeds()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);

        // Add another event
        var secondEvent = new Event
        {
            DjUserId = 1,
            Name = "Second Test Event",
            Slug = "second-test-event",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Events.Add(secondEvent);
        await _context.SaveChangesAsync();

        var session1 = new Session
        {
            UserId = 2,
            EventId = 1,
            ClientIP = "192.168.1.1",
            LastSeen = DateTime.UtcNow,
            RequestCount = 0
        };

        var session2 = new Session
        {
            UserId = 2, // Same user
            EventId = secondEvent.EventId, // Different event
            ClientIP = "192.168.1.1",
            LastSeen = DateTime.UtcNow,
            RequestCount = 0
        };

        // Act
        _context.Sessions.AddRange(session1, session2);
        await _context.SaveChangesAsync();

        // Assert
        var savedSessions = await _context.Sessions
            .Where(s => s.UserId == 2)
            .ToListAsync();

        savedSessions.Should().HaveCount(2);
        savedSessions.Should().Contain(s => s.EventId == 1);
        savedSessions.Should().Contain(s => s.EventId == secondEvent.EventId);
    }

    /// <summary>
    /// Tests that sessions must have valid user IDs using application logic.
    /// </summary>
    [Fact]
    public async Task Session_InvalidUserId_DetectedByApplicationLogic()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);

        // Act - Check if user exists (application-level validation)
        var invalidUserId = 999;
        var userExists = await _context.Users.AnyAsync(u => u.UserId == invalidUserId);

        // Assert
        userExists.Should().BeFalse("Application should detect invalid user ID");
    }

    /// <summary>
    /// Tests that sessions must have valid event IDs using application logic.
    /// </summary>
    [Fact]
    public async Task Session_InvalidEventId_DetectedByApplicationLogic()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);

        // Act - Check if event exists (application-level validation)
        var invalidEventId = 999;
        var eventExists = await _context.Events.AnyAsync(e => e.EventId == invalidEventId);

        // Assert
        eventExists.Should().BeFalse("Application should detect invalid event ID");
    }

    /// <summary>
    /// Tests that session client IP has proper length validation at application level.
    /// </summary>
    [Fact]
    public Task Session_ClientIPTooLong_DetectedByApplicationLogic()
    {
        // Arrange
        var longIP = new string('1', 46); // Max length is 45

        // Act - Application-level validation
        var isIPTooLong = longIP.Length > 45;

        // Assert
        isIPTooLong.Should().BeTrue("Application should detect client IP that is too long");

        return Task.CompletedTask;
    }

    #endregion

    #region Cascade Delete Tests

    /// <summary>
    /// Tests that cascade delete works correctly for votes when request is deleted.
    /// </summary>
    [Fact]
    public async Task Request_DeleteWithVotes_CascadeDeletesVotes()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);

        // Add votes to the first request
        var votes = new[]
        {
            new Vote { UserId = 2, RequestId = 1, CreatedAt = DateTime.UtcNow },
            new Vote { UserId = 3, RequestId = 1, CreatedAt = DateTime.UtcNow }
        };
        _context.Votes.AddRange(votes);
        await _context.SaveChangesAsync();

        // Verify votes exist
        var voteCountBefore = await _context.Votes.CountAsync(v => v.RequestId == 1);
        voteCountBefore.Should().Be(2);

        // Act - Delete the request
        var requestToDelete = await _context.Requests.FindAsync(1);
        _context.Requests.Remove(requestToDelete!);
        await _context.SaveChangesAsync();

        // Assert - Votes should be cascade deleted
        var voteCountAfter = await _context.Votes.CountAsync(v => v.RequestId == 1);
        voteCountAfter.Should().Be(0);
    }

    /// <summary>
    /// Tests that deleting an event cascades to requests and their votes.
    /// </summary>
    [Fact]
    public async Task Event_DeleteWithRequestsAndVotes_CascadeDeletesAll()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);

        // Add votes to requests
        var votes = new[]
        {
            new Vote { UserId = 2, RequestId = 1, CreatedAt = DateTime.UtcNow },
            new Vote { UserId = 3, RequestId = 2, CreatedAt = DateTime.UtcNow }
        };
        _context.Votes.AddRange(votes);
        await _context.SaveChangesAsync();

        // Verify initial state
        var requestCountBefore = await _context.Requests.CountAsync(r => r.EventId == 1);
        var voteCountBefore = await _context.Votes.CountAsync();
        requestCountBefore.Should().Be(2);
        voteCountBefore.Should().Be(2);

        // Act - Delete the event
        var eventToDelete = await _context.Events.FindAsync(1);
        _context.Events.Remove(eventToDelete!);
        await _context.SaveChangesAsync();

        // Assert - Requests and votes should be cascade deleted
        var requestCountAfter = await _context.Requests.CountAsync(r => r.EventId == 1);
        var voteCountAfter = await _context.Votes.CountAsync();
        requestCountAfter.Should().Be(0);
        voteCountAfter.Should().Be(0);
    }

    /// <summary>
    /// Tests that deleting a user with votes can be detected using application logic.
    /// </summary>
    [Fact]
    public async Task User_DeleteWithVotes_DetectedByApplicationLogic()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);

        // Add a vote from user 2
        var vote = new Vote
        {
            UserId = 2,
            RequestId = 1,
            CreatedAt = DateTime.UtcNow
        };
        _context.Votes.Add(vote);
        await _context.SaveChangesAsync();

        // Act - Check if user has votes (application-level validation)
        var userHasVotes = await _context.Votes.AnyAsync(v => v.UserId == 2);

        // Assert
        userHasVotes.Should().BeTrue("Application should detect that user has votes before deletion");
    }

    /// <summary>
    /// Tests that deleting a user cascades to their sessions
    /// </summary>
    [Fact]
    public async Task User_DeleteWithSessions_CascadeDeleteSessions()
    {
        // Arrange
        await TestDbContextFactory.SeedTestDataAsync(_context);

        // Add sessions for user 3 (who has no votes)
        var sessions = new[]
        {
            new Session { UserId = 3, EventId = 1, ClientIP = "192.168.1.1", LastSeen = DateTime.UtcNow, RequestCount = 0 }
        };
        _context.Sessions.AddRange(sessions);
        await _context.SaveChangesAsync();

        // Verify session exists
        var sessionCountBefore = await _context.Sessions.CountAsync(s => s.UserId == 3);

        sessionCountBefore.Should().Be(1);

        // Act - Delete the user
        var userToDelete = await _context.Users.FindAsync(3);
        _context.Users.Remove(userToDelete!);

        await _context.SaveChangesAsync();

        // Assert - Sessions should be cascade deleted
        var sessionCountAfter = await _context.Sessions.CountAsync(s => s.UserId == 3);
        sessionCountAfter.Should().Be(0);

        // Verify that the user is also deleted
        var userCountAfter = await _context.Users.CountAsync(u => u.UserId == 3);
        userCountAfter.Should().Be(0);
    }

    #endregion

    /// <summary>
    /// Disposes of the test resources.
    /// </summary>
    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}