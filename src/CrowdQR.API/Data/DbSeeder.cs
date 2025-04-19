using CrowdQR.Api.Models;
using CrowdQR.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace CrowdQR.Api.Data;

/// <summary>
/// Provides methods for seeding the database with initial data.
/// </summary>
public static class DbSeeder
{
    /// <summary>
    /// Seeds the database with initial data if it doesn't already exist.
    /// </summary>
    /// <param name="context">The database context.</param>
    public static async Task SeedAsync(CrowdQRContext context)
    {
        // Ensure database exists and is up-to-date
        await context.Database.MigrateAsync();

        // Only seed if no users exist
        if (await context.Users.AnyAsync())
        {
            return;
        }

        // Add DJ users
        var djUser = new User
        {
            Username = "dj_master",
            Role = UserRole.DJ,
            CreatedAt = DateTime.UtcNow
        };

        var djUser2 = new User
        {
            Username = "dj_groove",
            Role = UserRole.DJ,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(djUser);
        context.Users.Add(djUser2);
        await context.SaveChangesAsync();

        // Add audience users
        var audienceUsers = new List<User>
        {
            new() { Username = "partygoer1", Role = UserRole.Audience },
            new() { Username = "dancefloor_queen", Role = UserRole.Audience },
            new() { Username = "music_lover", Role = UserRole.Audience },
            new() { Username = "beat_enthusiast", Role = UserRole.Audience },
            new() { Username = "rhythm_fanatic", Role = UserRole.Audience }
        };

        context.Users.AddRange(audienceUsers);
        await context.SaveChangesAsync();

        // Add events
        var events = new List<Event>
        {
            new() {
                Name = "Saturday Night Fever",
                Slug = "saturday-night-fever",
                DjUserId = djUser.UserId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Name = "Club Grooves",
                Slug = "club-grooves",
                DjUserId = djUser2.UserId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        context.Events.AddRange(events);
        await context.SaveChangesAsync();

        // Add requests
        var requests = new List<Request>
        {
            new()
            {
                UserId = audienceUsers[0].UserId,
                EventId = events[0].EventId,
                SongName = "Stayin' Alive",
                ArtistName = "Bee Gees",
                Status = RequestStatus.Pending,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                UserId = audienceUsers[1].UserId,
                EventId = events[0].EventId,
                SongName = "Don't Stop 'Til You Get Enough",
                ArtistName = "Michael Jackson",
                Status = RequestStatus.Approved,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                UserId = audienceUsers[2].UserId,
                EventId = events[0].EventId,
                SongName = "Good Times",
                ArtistName = "Chic",
                Status = RequestStatus.Pending,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                UserId = audienceUsers[3].UserId,
                EventId = events[1].EventId,
                SongName = "One More Time",
                ArtistName = "Daft Punk",
                Status = RequestStatus.Pending,
                CreatedAt = DateTime.UtcNow
            }
        };

        context.Requests.AddRange(requests);
        await context.SaveChangesAsync();

        // Add votes
        var votes = new List<Vote>
        {
            new() { RequestId = requests[0].RequestId, UserId = audienceUsers[1].UserId },
            new() { RequestId = requests[0].RequestId, UserId = audienceUsers[2].UserId },
            new() { RequestId = requests[0].RequestId, UserId = audienceUsers[3].UserId },
            new() { RequestId = requests[1].RequestId, UserId = audienceUsers[0].UserId },
            new() { RequestId = requests[1].RequestId, UserId = audienceUsers[2].UserId },
            new() { RequestId = requests[2].RequestId, UserId = audienceUsers[0].UserId }
        };

        context.Votes.AddRange(votes);

        // Add track metadata
        var trackMetadata = new List<TrackMetadata>
        {
            new()
            {
                RequestId = requests[0].RequestId,
                SpotifyId = "4wXchxfTTggLtzkoUhO86Q",
                YoutubeId = "I_izvAbhExY",
                Duration = 285,
                AlbumArtUrl = "https://i.scdn.co/image/ab67616d0000b2731d5cf954640067e2143e6990"
            },
            new()
            {
                RequestId = requests[1].RequestId,
                SpotifyId = "46eu3SBuFCXWsPT39Yg3tJ",
                YoutubeId = "yURRmWtbTbo",
                Duration = 366,
                AlbumArtUrl = "https://i.scdn.co/image/ab67616d0000b273f2e2ead45c2b229b5b3fdd85"
            }
        };

        context.TrackMetadata.AddRange(trackMetadata);

        // Add sessions
        var sessions = new List<Session>
        {
            new()
            {
                UserId = audienceUsers[0].UserId,
                EventId = events[0].EventId,
                ClientIP = "192.168.1.1",
                LastSeen = DateTime.UtcNow,
                RequestCount = 1
            },
            new()
            {
                UserId = audienceUsers[1].UserId,
                EventId = events[0].EventId,
                ClientIP = "192.168.1.2",
                LastSeen = DateTime.UtcNow,
                RequestCount = 1
            }
        };

        context.Sessions.AddRange(sessions);
        await context.SaveChangesAsync();
    }
}
