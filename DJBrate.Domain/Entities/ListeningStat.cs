using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DJBrate.Domain.Entities;

[Table("listening_stats")]
public class ListeningStat
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Required]
    [Column("stat_date")]
    public DateOnly StatDate { get; set; }

    [Column("dominant_mood")]
    public string? DominantMood { get; set; }

    [Column("top_genre")]
    public string? TopGenre { get; set; }

    [Column("avg_energy")]
    public float? AvgEnergy { get; set; }

    [Column("avg_valence")]
    public float? AvgValence { get; set; }

    [Column("avg_tempo")]
    public float? AvgTempo { get; set; }

    [Required]
    [Column("playlists_generated")]
    public int PlaylistsGenerated { get; set; } = 0;

    [Required]
    [Column("tracks_liked")]
    public int TracksLiked { get; set; } = 0;

    [Required]
    [Column("tracks_skipped")]
    public int TracksSkipped { get; set; } = 0;

    public User User { get; set; } = null!;
}
