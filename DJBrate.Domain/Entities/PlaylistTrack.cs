using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DJBrate.Domain.Entities;

[Table("playlist_tracks")]
public class PlaylistTrack
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("playlist_id")]
    public Guid PlaylistId { get; set; }

    [Required]
    [Column("spotify_track_id")]
    public string SpotifyTrackId { get; set; } = null!;

    [Required]
    [Column("track_name")]
    public string TrackName { get; set; } = null!;

    [Required]
    [Column("spotify_artist_id")]
    public string SpotifyArtistId { get; set; } = null!;

    [Required]
    [Column("artist_name")]
    public string ArtistName { get; set; } = null!;

    [Column("album_name")]
    public string? AlbumName { get; set; }

    [Column("duration_ms")]
    public int? DurationMs { get; set; }

    [Column("preview_url")]
    public string? PreviewUrl { get; set; }

    [Column("valence")]
    public float? Valence { get; set; }

    [Column("energy")]
    public float? Energy { get; set; }

    [Column("tempo")]
    public float? Tempo { get; set; }

    [Column("danceability")]
    public float? Danceability { get; set; }

    [Column("acousticness")]
    public float? Acousticness { get; set; }

    [Required]
    [Column("position")]
    public int Position { get; set; }

    public Playlist Playlist { get; set; } = null!;
    public ICollection<TrackFeedback> TrackFeedbacks { get; set; } = new List<TrackFeedback>();
}
