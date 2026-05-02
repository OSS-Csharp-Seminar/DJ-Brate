using DJBrate.Domain.Entities;

namespace DJBrate.Application.Mcp;

public class McpExecutionContext
{
    public Guid SessionId { get; init; }
    public User User { get; init; } = null!;
    public Guid? PlaylistId { get; init; }
    public bool IsEditMode { get; init; }
}
