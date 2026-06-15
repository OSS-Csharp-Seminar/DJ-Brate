using DJBrate.Domain.Entities;
using DJBrate.Domain.Interfaces;
using DJBrate.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DJBrate.Infrastructure.Repositories;

public class TrackFeedbackRepository : ITrackFeedbackRepository
{
    private readonly AppDbContext _db;

    public TrackFeedbackRepository(AppDbContext db)
    {
        _db = db;
    }

    // Dohvaca feedback korisnika za jednu Spotify pjesmu.
    public async Task<TrackFeedback?> GetByUserAndSpotifyTrackAsync(Guid userId, string spotifyTrackId)
        => await _db.TrackFeedbacks
            .FirstOrDefaultAsync(f => f.UserId == userId && f.SpotifyTrackId == spotifyTrackId);

    // Mapira svaku pjesmu u playlisti na korisnikov feedback prema Spotify ID-u.
    public async Task<Dictionary<Guid, string>> GetPlaylistFeedbackMapAsync(Guid userId, Guid playlistId)
    {
        var rows = await (
            from track in _db.PlaylistTracks
            where track.PlaylistId == playlistId
            join fb in _db.TrackFeedbacks.Where(f => f.UserId == userId)
                on track.SpotifyTrackId equals fb.SpotifyTrackId
            select new { track.Id, fb.FeedbackType }
        ).ToListAsync();

        return rows.ToDictionary(r => r.Id, r => r.FeedbackType);
    }

    // Dohvaca korisnikov feedback za sve pjesme koje se nalaze u playlisti.
    public async Task<List<TrackFeedback>> GetPlaylistFeedbackAsync(Guid userId, Guid playlistId)
    {
        var spotifyIds = _db.PlaylistTracks
            .Where(t => t.PlaylistId == playlistId)
            .Select(t => t.SpotifyTrackId);

        return await _db.TrackFeedbacks
            .Where(f => f.UserId == userId && spotifyIds.Contains(f.SpotifyTrackId))
            .ToListAsync();
    }

    // Dodaje novi feedback ili mijenja postojeci zapis za pjesmu.
    public async Task UpsertAsync(TrackFeedback feedback)
    {
        var existing = await _db.TrackFeedbacks
            .FirstOrDefaultAsync(f => f.UserId == feedback.UserId && f.SpotifyTrackId == feedback.SpotifyTrackId);

        if (existing is null)
        {
            _db.TrackFeedbacks.Add(feedback);
        }
        else
        {
            existing.FeedbackType = feedback.FeedbackType;
            existing.CreatedAt    = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
    }

    // Brise feedback korisnika za odabranu Spotify pjesmu.
    public async Task DeleteAsync(Guid userId, string spotifyTrackId)
    {
        var existing = await _db.TrackFeedbacks
            .FirstOrDefaultAsync(f => f.UserId == userId && f.SpotifyTrackId == spotifyTrackId);

        if (existing is not null)
        {
            _db.TrackFeedbacks.Remove(existing);
            await _db.SaveChangesAsync();
        }
    }
}
