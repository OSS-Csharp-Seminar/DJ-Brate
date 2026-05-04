using Microsoft.EntityFrameworkCore;
using DJBrate.Domain.Entities;

namespace DJBrate.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<AiModelConfig> AiModelConfigs { get; set; }
    public DbSet<MoodSession> MoodSessions { get; set; }
    public DbSet<AiConversationMessage> AiConversationMessages { get; set; }
    public DbSet<AiMoodMapping> AiMoodMappings { get; set; }
    public DbSet<McpToolCall> McpToolCalls { get; set; }
    public DbSet<Playlist> Playlists { get; set; }
    public DbSet<PlaylistTrack> PlaylistTracks { get; set; }
    public DbSet<TrackFeedback> TrackFeedbacks { get; set; }
    public DbSet<UserTopTrack> UserTopTracks { get; set; }
    public DbSet<UserTopArtist> UserTopArtists { get; set; }
    public DbSet<ListeningStat> ListeningStats { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.SpotifyId)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<MoodSession>()
            .HasOne(ms => ms.AiConfig)
            .WithMany(ac => ac.MoodSessions)
            .HasForeignKey(ms => ms.AiConfigId);

        modelBuilder.Entity<MoodSession>()
            .HasOne(ms => ms.User)
            .WithMany(u => u.MoodSessions)
            .HasForeignKey(ms => ms.UserId);

        modelBuilder.Entity<AiConversationMessage>()
            .HasOne(m => m.MoodSession)
            .WithMany(ms => ms.AiConversationMessages)
            .HasForeignKey(m => m.SessionId);

        modelBuilder.Entity<AiMoodMapping>()
            .HasOne(m => m.MoodSession)
            .WithOne(ms => ms.AiMoodMapping)
            .HasForeignKey<AiMoodMapping>(m => m.SessionId);

        modelBuilder.Entity<McpToolCall>()
            .HasOne(m => m.MoodSession)
            .WithMany(ms => ms.McpToolCalls)
            .HasForeignKey(m => m.SessionId);

        modelBuilder.Entity<Playlist>()
            .HasOne(p => p.MoodSession)
            .WithOne(ms => ms.Playlist)
            .HasForeignKey<Playlist>(p => p.SessionId);

        modelBuilder.Entity<Playlist>()
            .HasOne(p => p.User)
            .WithMany(u => u.Playlists)
            .HasForeignKey(p => p.UserId);

        modelBuilder.Entity<Playlist>()
            .HasIndex(p => p.ShareToken)
            .IsUnique()
            .HasFilter("share_token IS NOT NULL");

        modelBuilder.Entity<PlaylistTrack>()
            .HasOne(pt => pt.Playlist)
            .WithMany(p => p.PlaylistTracks)
            .HasForeignKey(pt => pt.PlaylistId);

        modelBuilder.Entity<TrackFeedback>()
            .HasIndex(tf => new { tf.UserId, tf.PlaylistTrackId })
            .IsUnique();

        modelBuilder.Entity<TrackFeedback>()
            .HasOne(tf => tf.User)
            .WithMany(u => u.TrackFeedbacks)
            .HasForeignKey(tf => tf.UserId);

        modelBuilder.Entity<TrackFeedback>()
            .HasOne(tf => tf.PlaylistTrack)
            .WithMany(pt => pt.TrackFeedbacks)
            .HasForeignKey(tf => tf.PlaylistTrackId);

        modelBuilder.Entity<UserTopTrack>()
            .HasIndex(ut => new { ut.UserId, ut.SpotifyTrackId, ut.TimeRange })
            .IsUnique();

        modelBuilder.Entity<UserTopTrack>()
            .HasOne(ut => ut.User)
            .WithMany(u => u.UserTopTracks)
            .HasForeignKey(ut => ut.UserId);

        modelBuilder.Entity<UserTopArtist>()
            .HasIndex(ua => new { ua.UserId, ua.SpotifyArtistId, ua.TimeRange })
            .IsUnique();

        modelBuilder.Entity<UserTopArtist>()
            .HasOne(ua => ua.User)
            .WithMany(u => u.UserTopArtists)
            .HasForeignKey(ua => ua.UserId);

        modelBuilder.Entity<ListeningStat>()
            .HasIndex(ls => new { ls.UserId, ls.StatDate })
            .IsUnique();

        modelBuilder.Entity<ListeningStat>()
            .HasOne(ls => ls.User)
            .WithMany(u => u.ListeningStats)
            .HasForeignKey(ls => ls.UserId);
    }
}
