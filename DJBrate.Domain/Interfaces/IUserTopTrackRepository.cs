using DJBrate.Domain.Entities;

namespace DJBrate.Domain.Interfaces;

public interface IUserTopTrackRepository : IRepository<UserTopTrack>
{
    Task<IEnumerable<UserTopTrack>> GetByUserAndTimeRangeAsync(Guid userId, string timeRange);
    Task DeleteByUserAndTimeRangeAsync(Guid userId, string timeRange);
}
