using DJBrate.Application.Interfaces;
using DJBrate.Domain.Entities;
using DJBrate.Domain.Interfaces;

namespace DJBrate.Application.Services;

public class TrackFeedbackService : ITrackFeedbackService
{
    private readonly ITrackFeedbackRepository _repo;

    public TrackFeedbackService(ITrackFeedbackRepository repo)
    {
        _repo = repo;
    }

    // Dohvaca korisnikov feedback i mapira ga po ID-u pjesme u playlisti.
    public async Task<Dictionary<Guid, string>> GetForPlaylistAsync(Guid userId, Guid playlistId)
        => await _repo.GetPlaylistFeedbackMapAsync(userId, playlistId);

    // Dodaje, mijenja ili uklanja like/dislike feedback za pjesmu.
    public async Task ToggleFeedbackAsync(Guid userId, PlaylistTrack track, string feedbackType)
    {
        var existing = await _repo.GetByUserAndSpotifyTrackAsync(userId, track.SpotifyTrackId);

        if (existing?.FeedbackType == feedbackType)
        {
            await _repo.DeleteAsync(userId, track.SpotifyTrackId);
            return;
        }

        await _repo.UpsertAsync(new TrackFeedback
        {
            UserId         = userId,
            SpotifyTrackId = track.SpotifyTrackId,
            FeedbackType   = feedbackType
        });
    }
}
