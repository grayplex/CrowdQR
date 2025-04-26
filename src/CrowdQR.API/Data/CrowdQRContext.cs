using Microsoft.EntityFrameworkCore;
using CrowdQR.Api.Models;
using CrowdQR.Shared.Models.Enums;

namespace CrowdQR.Api.Data;

/// <summary>
/// Database context for the CrowdQR application.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="CrowdQRContext"/> class.
/// </remarks>
/// <param name="options">The options to be used by the context.</param>

public class CrowdQRContext(DbContextOptions<CrowdQRContext> options) : DbContext(options)
{
    /// <summary>
    /// Gets or sets the users in the database.
    /// </summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>
    /// Gets or sets the events in the database.
    /// </summary>
    public DbSet<Event> Events => Set<Event>();

    /// <summary>
    /// Gets or sets the requests in the database.
    /// </summary>
    public DbSet<Request> Requests => Set<Request>();

    /// <summary>
    /// Gets or sets the votes in the database.
    /// </summary>
    public DbSet<Vote> Votes => Set<Vote>();

    /// <summary>
    /// Gets or sets the sessions in the database.
    /// </summary>
    public DbSet<Session> Sessions => Set<Session>();

    /// <summary>
    /// Gets or sets the track metadata in the database.
    /// </summary>
    public DbSet<TrackMetadata> TrackMetadata => Set<TrackMetadata>();

    /// <summary>
    /// Configures the model that was discovered by convention from the entity types.
    /// </summary>
    /// <param name="modelBuilder">The builder being used to construct the model for this context.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("User");
            entity.HasIndex(e => e.Username).IsUnique();
            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.Role).HasConversion<string>();
        });

        // Configure Event entity
        modelBuilder.Entity<Event>(entity =>
        {
            entity.ToTable("Event");
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.Property(e => e.EventId).HasColumnName("EventID");
            entity.Property(e => e.DjUserId).HasColumnName("DJUserID");

            entity.HasOne(e => e.DJ)
                  .WithMany(u => u.HostedEvents)
                  .HasForeignKey(e => e.DjUserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Request entity
        modelBuilder.Entity<Request>(entity =>
        {
            entity.ToTable("Request");
            entity.HasIndex(e => e.EventId);
            entity.HasIndex(e => e.Status);
            entity.Property(e => e.RequestId).HasColumnName("RequestID");
            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.EventId).HasColumnName("EventID");
            entity.Property(e => e.Status).HasConversion<string>();

            entity.HasOne(r => r.User)
                  .WithMany(u => u.Requests)
                  .HasForeignKey(r => r.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.Event)
                  .WithMany(e => e.Requests)
                  .HasForeignKey(r => r.EventId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Vote entity
        modelBuilder.Entity<Vote>(entity =>
        {
            entity.ToTable("Vote");
            entity.HasIndex(e => new { e.UserId, e.RequestId }).IsUnique()
                  .HasDatabaseName("one_vote_per_user");
            entity.Property(e => e.VoteId).HasColumnName("VoteID");
            entity.Property(e => e.RequestId).HasColumnName("RequestID");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(v => v.Request)
                  .WithMany(r => r.Votes)
                  .HasForeignKey(v => v.RequestId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(v => v.User)
                  .WithMany(u => u.Votes)
                  .HasForeignKey(v => v.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Session entity
        modelBuilder.Entity<Session>(entity =>
        {
            entity.ToTable("Session");
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.EventId, e.UserId }).IsUnique()
                  .HasDatabaseName("one_session_per_user_event");
            entity.Property(e => e.SessionId).HasColumnName("SessionID");
            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.EventId).HasColumnName("EventID");

            entity.HasOne(s => s.User)
                  .WithMany(u => u.Sessions)
                  .HasForeignKey(s => s.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(s => s.Event)
                  .WithMany(e => e.Sessions)
                  .HasForeignKey(s => s.EventId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure TrackMetadata entity
        modelBuilder.Entity<TrackMetadata>(entity =>
        {
            entity.ToTable("TrackMetadata");
            entity.Property(e => e.TrackId).HasColumnName("TrackID");
            entity.Property(e => e.RequestId).HasColumnName("RequestID");
            entity.Property(e => e.SpotifyId).HasColumnName("SpotifyID");
            entity.Property(e => e.YoutubeId).HasColumnName("YoutubeID");
            entity.Property(e => e.AlbumArtUrl).HasColumnName("AlbumArtURL");

            entity.HasOne(t => t.Request)
                  .WithOne(r => r.TrackMetadata)
                  .HasForeignKey<TrackMetadata>(t => t.RequestId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
