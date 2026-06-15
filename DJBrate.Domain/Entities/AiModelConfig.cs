using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DJBrate.Domain.Entities;

[Table("ai_model_configs")]
public class AiModelConfig
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("config_name")]
    public string ConfigName { get; set; } = null!;

    [Required]
    [Column("system_prompt")]
    public string SystemPrompt { get; set; } = null!;

    [Required]
    [Column("model_name")]
    public string ModelName { get; set; } = null!;

    [Column("temperature")]
    public float? Temperature { get; set; }

    [Column("max_tokens")]
    public int? MaxTokens { get; set; }

    [Required]
    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<MoodSession> MoodSessions { get; set; } = new List<MoodSession>();
}
