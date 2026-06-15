using DJBrate.Domain.Entities;

namespace DJBrate.Domain.Interfaces;

// Definira upite za AI mood sesije.
public interface IMoodSessionRepository : IRepository<MoodSession>
{
    Task<IEnumerable<MoodSession>> GetByUserIdAsync(Guid userId);
}
