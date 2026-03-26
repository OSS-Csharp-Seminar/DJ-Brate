using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DJBrate.Domain.Entities;

[Table("track_feedbacks")]
public class TrackFeedback
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Required]
    [Column("playlist_track_id")]
    public Guid PlaylistTrackId { get; set; }

    [Required]
    [Column("spotify_track_id")]
    public string SpotifyTrackId { get; set; } = null!;

    [Required]
    [Column("feedback_type")]
    public string FeedbackType { get; set; } = null!;

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public PlaylistTrack PlaylistTrack { get; set; } = null!;
}
