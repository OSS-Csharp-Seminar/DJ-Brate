using System.Text.Json;

namespace DJBrate.Application.Models.Ai;

public class AiMessage
{
    public string Role { get; set; } = null!;
    public string? Text { get; set; }
    public AiToolCall? ToolCall { get; set; }
    public AiToolResult? ToolResult { get; set; }
}

public class AiToolCall
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public JsonDocument Arguments { get; set; } = null!;
}

public class AiToolResult
{
    public string ToolCallId { get; set; } = null!;
    public string Result { get; set; } = null!;
}

public class AiResponse
{
    public string? Text { get; set; }
    public List<AiToolCall> ToolCalls { get; set; } = [];
    public bool HasToolCalls => ToolCalls.Count > 0;
}

public class AiToolDefinition
{
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public JsonDocument Parameters { get; set; } = null!;
}
