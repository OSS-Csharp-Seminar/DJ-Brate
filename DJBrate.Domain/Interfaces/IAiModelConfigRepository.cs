using DJBrate.Domain.Entities;

namespace DJBrate.Domain.Interfaces;

// Definira dohvat aktivne AI konfiguracije.
public interface IAiModelConfigRepository : IRepository<AiModelConfig>
{
    Task<AiModelConfig?> GetActiveConfigAsync();
}
