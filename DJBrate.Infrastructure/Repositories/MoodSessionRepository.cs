using DJBrate.Domain.Entities;
using DJBrate.Domain.Interfaces;
using DJBrate.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DJBrate.Infrastructure.Repositories;

public class MoodSessionRepository : Repository<MoodSession>, IMoodSessionRepository
{
    public MoodSessionRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<MoodSession>> GetByUserIdAsync(Guid userId)
        => await _dbSet.Where(s => s.UserId == userId).ToListAsync();
}
