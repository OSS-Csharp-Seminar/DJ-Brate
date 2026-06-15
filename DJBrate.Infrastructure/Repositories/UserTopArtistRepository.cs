using DJBrate.Domain.Entities;
using DJBrate.Domain.Interfaces;
using DJBrate.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DJBrate.Infrastructure.Repositories;

public class UserTopArtistRepository : Repository<UserTopArtist>, IUserTopArtistRepository
{
    public UserTopArtistRepository(AppDbContext context) : base(context) { }

    // Dohvaca top izvodace korisnika za odabrani Spotify period.
    public async Task<IEnumerable<UserTopArtist>> GetByUserAndTimeRangeAsync(Guid userId, string timeRange)
        => await _dbSet.Where(a => a.UserId == userId && a.TimeRange == timeRange)
                       .OrderBy(a => a.RankPosition)
                       .ToListAsync();

    // Brise stare top izvodace prije nove sinkronizacije perioda.
    public async Task DeleteByUserAndTimeRangeAsync(Guid userId, string timeRange)
    {
        var items = await _dbSet.Where(a => a.UserId == userId && a.TimeRange == timeRange).ToListAsync();
        _context.RemoveRange(items);
        await _context.SaveChangesAsync();
    }
}
