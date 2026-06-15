using DJBrate.Domain.Entities;

namespace DJBrate.Domain.Interfaces;

public interface IMoodSessionRepository : IRepository<MoodSession>
{
    Task<IEnumerable<MoodSession>> GetByUserIdAsync(Guid userId);
}
