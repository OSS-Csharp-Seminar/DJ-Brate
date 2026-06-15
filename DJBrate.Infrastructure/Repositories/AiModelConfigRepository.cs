using DJBrate.Domain.Entities;
using DJBrate.Domain.Interfaces;
using DJBrate.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DJBrate.Infrastructure.Repositories;

// Omogucava dohvat i spremanje konfiguracije AI modela.
public class AiModelConfigRepository : Repository<AiModelConfig>, IAiModelConfigRepository
{
    public AiModelConfigRepository(AppDbContext context) : base(context) { }

    public async Task<AiModelConfig?> GetActiveConfigAsync()
        => await _dbSet.FirstOrDefaultAsync(c => c.IsActive); 
        //iz AiModelConfigs tablice dohvaća prvi zapis koji ima IsActive postavljeno na true 
        // sto znači da je to trenutno aktivna konfiguracija AI modela. i vraća model, temperature, max tokens, system prompt.
}
