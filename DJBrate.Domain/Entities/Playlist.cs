using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DJBrate.Domain.Entities;

[Table("playlists")]
public class Playlist
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("session_id")]
    public Guid SessionId { get; set; }

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("spotify_playlist_id")]
    public string? SpotifyPlaylistId { get; set; }

    [Required]
    [Column("name")]
    public string Name { get; set; } = null!;

    [Column("description")]
    public string? Description { get; set; }

    [Column("image_url")]
    public string? ImageUrl { get; set; }

    [Required]
    [Column("track_count")]
    public int TrackCount { get; set; } = 0;

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("share_token")]
    public string? ShareToken { get; set; }

    [Required]
    [Column("is_shared")]
    public bool IsShared { get; set; } = false;

    public MoodSession MoodSession { get; set; } = null!;
    public User User { get; set; } = null!;
    public ICollection<PlaylistTrack> PlaylistTracks { get; set; } = new List<PlaylistTrack>();
}
