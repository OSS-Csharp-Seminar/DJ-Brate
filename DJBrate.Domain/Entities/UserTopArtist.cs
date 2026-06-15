using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DJBrate.Domain.Entities;

[Table("user_top_artists")]
public class UserTopArtist
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Required]
    [Column("spotify_artist_id")]
    public string SpotifyArtistId { get; set; } = null!;

    [Required]
    [Column("artist_name")]
    public string ArtistName { get; set; } = null!;

    [Column("genres")]
    public string[]? Genres { get; set; }

    [Required]
    [Column("time_range")]
    public string TimeRange { get; set; } = null!;

    [Required]
    [Column("rank_position")]
    public int RankPosition { get; set; }

    [Required]
    [Column("synced_at")]
    public DateTime SyncedAt { get; set; }

    public User User { get; set; } = null!;
}
