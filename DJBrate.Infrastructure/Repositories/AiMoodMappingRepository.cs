using DJBrate.Domain.Entities;
using DJBrate.Domain.Interfaces;
using DJBrate.Infrastructure.Data;

namespace DJBrate.Infrastructure.Repositories;

public class AiMoodMappingRepository : Repository<AiMoodMapping>, IAiMoodMappingRepository
{
    public AiMoodMappingRepository(AppDbContext context) : base(context) { }
}
