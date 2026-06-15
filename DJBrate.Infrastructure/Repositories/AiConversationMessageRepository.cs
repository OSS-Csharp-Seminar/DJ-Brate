using DJBrate.Domain.Entities;
using DJBrate.Domain.Interfaces;
using DJBrate.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DJBrate.Infrastructure.Repositories;

public class AiConversationMessageRepository : Repository<AiConversationMessage>, IAiConversationMessageRepository
{
    public AiConversationMessageRepository(AppDbContext context) : base(context) { }

    public async Task<List<AiConversationMessage>> GetBySessionIdAsync(Guid sessionId)
        => await _dbSet
            .Where(m => m.SessionId == sessionId)
            .OrderBy(m => m.SequenceOrder)
            .ToListAsync();

    public async Task<List<AiConversationMessage>> GetByPlaylistIdAsync(Guid playlistId)
    {
        var playlist = await _context.Set<Playlist>().FindAsync(playlistId);
        if (playlist is null) return [];

        var sessionIds = await _context.Set<MoodSession>()
            .Where(s => s.Id == playlist.SessionId || s.RefinesPlaylistId == playlistId)
            .Select(s => s.Id)
            .ToListAsync();

        return await _dbSet
            .Where(m => sessionIds.Contains(m.SessionId))
            .OrderBy(m => m.CreatedAt)
            .ThenBy(m => m.SequenceOrder)
            .ToListAsync();
    }
}
