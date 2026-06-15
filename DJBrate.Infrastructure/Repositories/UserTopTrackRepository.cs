using DJBrate.Domain.Entities;
using DJBrate.Domain.Interfaces;
using DJBrate.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DJBrate.Infrastructure.Repositories;

public class UserTopTrackRepository : Repository<UserTopTrack>, IUserTopTrackRepository
{
    public UserTopTrackRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<UserTopTrack>> GetByUserAndTimeRangeAsync(Guid userId, string timeRange)
        => await _dbSet.Where(t => t.UserId == userId && t.TimeRange == timeRange)
                       .OrderBy(t => t.RankPosition)
                       .ToListAsync();

    public async Task DeleteByUserAndTimeRangeAsync(Guid userId, string timeRange)
    {
        var items = await _dbSet.Where(t => t.UserId == userId && t.TimeRange == timeRange).ToListAsync();
        _context.RemoveRange(items);
        await _context.SaveChangesAsync();
    }
}
