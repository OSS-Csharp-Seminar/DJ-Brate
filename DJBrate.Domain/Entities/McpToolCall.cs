using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace DJBrate.Domain.Entities;

[Table("mcp_tool_calls")]
public class McpToolCall
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("session_id")]
    public Guid SessionId { get; set; }

    [Required]
    [Column("tool_name")]
    public string ToolName { get; set; } = null!;

    [Required]
    [Column("input_parameters", TypeName = "jsonb")]
    public JsonDocument InputParameters { get; set; } = null!;

    [Column("output_result", TypeName = "jsonb")]
    public JsonDocument? OutputResult { get; set; }

    [Required]
    [Column("success")]
    public bool Success { get; set; } = true;

    [Column("error_message")]
    public string? ErrorMessage { get; set; }

    [Column("duration_ms")]
    public int? DurationMs { get; set; }

    [Required]
    [Column("called_at")]
    public DateTime CalledAt { get; set; } = DateTime.UtcNow;

    public MoodSession MoodSession { get; set; } = null!;
}
