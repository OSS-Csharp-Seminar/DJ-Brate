using DJBrate.Domain.Entities;
using DJBrate.Domain.Interfaces;
using DJBrate.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DJBrate.Infrastructure.Repositories;

public class AiModelConfigRepository : Repository<AiModelConfig>, IAiModelConfigRepository
{
    public AiModelConfigRepository(AppDbContext context) : base(context) { }

    public async Task<AiModelConfig?> GetActiveConfigAsync()
        => await _dbSet.FirstOrDefaultAsync(c => c.IsActive);
}
