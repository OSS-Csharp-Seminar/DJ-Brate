using DJBrate.Domain.Entities;

namespace DJBrate.Domain.Interfaces;

public interface IAiModelConfigRepository : IRepository<AiModelConfig>
{
    Task<AiModelConfig?> GetActiveConfigAsync();
}
