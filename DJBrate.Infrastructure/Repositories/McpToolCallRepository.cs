using DJBrate.Domain.Entities;
using DJBrate.Domain.Interfaces;
using DJBrate.Infrastructure.Data;

namespace DJBrate.Infrastructure.Repositories;

// Sprema i cita zapise MCP poziva preko osnovnog repositoryja.
public class McpToolCallRepository : Repository<McpToolCall>, IMcpToolCallRepository
{
    public McpToolCallRepository(AppDbContext context) : base(context) { }
}
