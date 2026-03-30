using DJBrate.Domain.Entities;
using DJBrate.Domain.Interfaces;
using DJBrate.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DJBrate.Infrastructure.Repositories;

public class UserTopArtistRepository : Repository<UserTopArtist>, IUserTopArtistRepository
{
    public UserTopArtistRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<UserTopArtist>> GetByUserAndTimeRangeAsync(Guid userId, string timeRange)
        => await _dbSet.Where(a => a.UserId == userId && a.TimeRange == timeRange)
                       .OrderBy(a => a.RankPosition)
                       .ToListAsync();

    public async Task DeleteByUserAndTimeRangeAsync(Guid userId, string timeRange)
    {
        var items = await _dbSet.Where(a => a.UserId == userId && a.TimeRange == timeRange).ToListAsync();
        _context.RemoveRange(items);
        await _context.SaveChangesAsync();
    }
}
