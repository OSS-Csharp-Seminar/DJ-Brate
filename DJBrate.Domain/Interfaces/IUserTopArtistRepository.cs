using DJBrate.Domain.Entities;

namespace DJBrate.Domain.Interfaces;

// Definira dohvat i zamjenu top izvodaca po vremenskom periodu.
public interface IUserTopArtistRepository : IRepository<UserTopArtist>
{
    Task<IEnumerable<UserTopArtist>> GetByUserAndTimeRangeAsync(Guid userId, string timeRange);
    Task DeleteByUserAndTimeRangeAsync(Guid userId, string timeRange);
}
