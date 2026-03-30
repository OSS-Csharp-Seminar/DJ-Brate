using DJBrate.Domain.Entities;

namespace DJBrate.Domain.Interfaces;

public interface IUserTopArtistRepository : IRepository<UserTopArtist>
{
    Task<IEnumerable<UserTopArtist>> GetByUserAndTimeRangeAsync(Guid userId, string timeRange);
    Task DeleteByUserAndTimeRangeAsync(Guid userId, string timeRange);
}
