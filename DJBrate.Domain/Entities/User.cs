using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DJBrate.Domain.Entities;

[Table("users")]
public class User
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("display_name")]
    public string DisplayName { get; set; } = null!;

    [Required]
    [Column("email")]
    public string Email { get; set; } = null!;

    [Required]
    [Column("role")]
    public string Role { get; set; } = "user";

    [Column("avatar_url")]
    public string? AvatarUrl { get; set; }

    [Column("spotify_id")]
    public string? SpotifyId { get; set; }

    [Column("spotify_access_token")]
    public string? SpotifyAccessToken { get; set; }

    [Column("spotify_refresh_token")]
    public string? SpotifyRefreshToken { get; set; }

    [Column("token_expires_at")]
    public DateTime? TokenExpiresAt { get; set; }

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("last_login_at")]
    public DateTime? LastLoginAt { get; set; }

    public ICollection<MoodSession> MoodSessions { get; set; } = new List<MoodSession>();
    public ICollection<Playlist> Playlists { get; set; } = new List<Playlist>();
    public ICollection<TrackFeedback> TrackFeedbacks { get; set; } = new List<TrackFeedback>();
    public ICollection<UserTopTrack> UserTopTracks { get; set; } = new List<UserTopTrack>();
    public ICollection<UserTopArtist> UserTopArtists { get; set; } = new List<UserTopArtist>();
    public ICollection<ListeningStat> ListeningStats { get; set; } = new List<ListeningStat>();
}
