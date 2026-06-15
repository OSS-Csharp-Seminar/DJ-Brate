using DJBrate.Domain.Entities;
using DJBrate.Domain.Interfaces;
using DJBrate.Infrastructure.Data;

namespace DJBrate.Infrastructure.Repositories;

// Sprema i cita AI mood mapping zapise preko osnovnog repositoryja.
public class AiMoodMappingRepository : Repository<AiMoodMapping>, IAiMoodMappingRepository
{
    public AiMoodMappingRepository(AppDbContext context) : base(context) { }
}
