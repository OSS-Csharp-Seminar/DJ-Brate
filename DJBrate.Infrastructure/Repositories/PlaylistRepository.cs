using DJBrate.Domain.Entities;
using DJBrate.Domain.Interfaces;
using DJBrate.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DJBrate.Infrastructure.Repositories;

public class PlaylistRepository : Repository<Playlist>, IPlaylistRepository
{
    public PlaylistRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Playlist>> GetByUserIdAsync(Guid userId)
        => await _dbSet
            .Include(p => p.MoodSession)
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

    public async Task<Playlist?> GetByIdWithTracksAsync(Guid id)
        => await _dbSet
            .Include(p => p.PlaylistTracks)
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<Playlist?> GetByShareTokenAsync(string token)
        => await _dbSet
            .Include(p => p.PlaylistTracks)
            .FirstOrDefaultAsync(p => p.ShareToken == token && p.IsShared);

    public async Task RemoveTracksAsync(Guid playlistId, List<string> spotifyTrackIds)
    {
        var tracks = await _context.Set<PlaylistTrack>()
            .Where(t => t.PlaylistId == playlistId && spotifyTrackIds.Contains(t.SpotifyTrackId))
            .ToListAsync();
        _context.Set<PlaylistTrack>().RemoveRange(tracks);

        var playlist = await _dbSet.FindAsync(playlistId);
        if (playlist is not null)
            playlist.TrackCount = await _context.Set<PlaylistTrack>()
                .CountAsync(t => t.PlaylistId == playlistId) - tracks.Count;

        await _context.SaveChangesAsync();

        var remaining = await _context.Set<PlaylistTrack>()
            .Where(t => t.PlaylistId == playlistId)
            .OrderBy(t => t.Position)
            .ToListAsync();
        for (var i = 0; i < remaining.Count; i++)
            remaining[i].Position = i + 1;
        await _context.SaveChangesAsync();
    }

    public async Task AddTracksAsync(Guid playlistId, List<PlaylistTrack> tracks)
    {
        var maxPosition = await _context.Set<PlaylistTrack>()
            .Where(t => t.PlaylistId == playlistId)
            .Select(t => (int?)t.Position)
            .MaxAsync() ?? 0;

        for (var i = 0; i < tracks.Count; i++)
        {
            tracks[i].PlaylistId = playlistId;
            tracks[i].Position   = maxPosition + i + 1;
        }

        await _context.Set<PlaylistTrack>().AddRangeAsync(tracks);

        var playlist = await _dbSet.FindAsync(playlistId);
        if (playlist is not null)
            playlist.TrackCount += tracks.Count;

        await _context.SaveChangesAsync();
    }
}
