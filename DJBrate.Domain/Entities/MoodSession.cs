using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DJBrate.Domain.Entities;

[Table("mood_sessions")]
public class MoodSession
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Required]
    [Column("ai_config_id")]
    public Guid AiConfigId { get; set; }

    [Column("prompt_text")]
    public string? PromptText { get; set; }

    [Column("selected_mood")]
    public string? SelectedMood { get; set; }

    [Column("selected_genres")]
    public string[]? SelectedGenres { get; set; }

    [Column("energy_level")]
    public float? EnergyLevel { get; set; }

    [Column("danceability")]
    public float? Danceability { get; set; }

    [Required]
    [Column("status")]
    public string Status { get; set; } = "creating";

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public AiModelConfig AiConfig { get; set; } = null!;
    public ICollection<AiConversationMessage> AiConversationMessages { get; set; } = new List<AiConversationMessage>();
    public AiMoodMapping? AiMoodMapping { get; set; }
    public ICollection<McpToolCall> McpToolCalls { get; set; } = new List<McpToolCall>();
    public Playlist? Playlist { get; set; }
}
