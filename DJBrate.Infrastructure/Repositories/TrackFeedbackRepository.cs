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

    // Dohvaca feedback korisnika za jednu pjesmu u playlisti.
    public async Task<TrackFeedback?> GetByUserAndTrackAsync(Guid userId, Guid playlistTrackId)
        => await _db.TrackFeedbacks
            .FirstOrDefaultAsync(f => f.UserId == userId && f.PlaylistTrackId == playlistTrackId);

    // Dohvaca sve korisnikove feedback zapise za playlistu.
    public async Task<List<TrackFeedback>> GetByUserAndPlaylistAsync(Guid userId, Guid playlistId)
        => await _db.TrackFeedbacks
            .Where(f => f.UserId == userId && f.PlaylistTrack.PlaylistId == playlistId)
            .ToListAsync();

    // Dodaje novi feedback ili mijenja postojeci zapis.
    public async Task UpsertAsync(TrackFeedback feedback)
    {
        var existing = await _db.TrackFeedbacks
            .FirstOrDefaultAsync(f => f.UserId == feedback.UserId && f.PlaylistTrackId == feedback.PlaylistTrackId);

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

    // Brise feedback korisnika za odabranu pjesmu.
    public async Task DeleteAsync(Guid userId, Guid playlistTrackId)
    {
        var existing = await _db.TrackFeedbacks
            .FirstOrDefaultAsync(f => f.UserId == userId && f.PlaylistTrackId == playlistTrackId);

        if (existing is not null)
        {
            _db.TrackFeedbacks.Remove(existing);
            await _db.SaveChangesAsync();
        }
    }
}
