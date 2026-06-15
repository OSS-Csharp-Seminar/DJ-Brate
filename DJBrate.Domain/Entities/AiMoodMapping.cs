using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DJBrate.Domain.Entities;

[Table("ai_mood_mappings")]
public class AiMoodMapping
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("session_id")]
    public Guid SessionId { get; set; }

    [Column("detected_mood")]
    public string? DetectedMood { get; set; }

    [Column("detected_genres")]
    public string[]? DetectedGenres { get; set; }

    [Column("target_valence")]
    public float? TargetValence { get; set; }

    [Column("target_energy")]
    public float? TargetEnergy { get; set; }

    [Column("target_tempo")]
    public float? TargetTempo { get; set; }

    [Column("target_danceability")]
    public float? TargetDanceability { get; set; }

    [Column("target_acousticness")]
    public float? TargetAcousticness { get; set; }

    [Required]
    [Column("flow_used")]
    public string FlowUsed { get; set; } = null!;

    [Column("ai_reasoning")]
    public string? AiReasoning { get; set; }

    public MoodSession MoodSession { get; set; } = null!;
}
