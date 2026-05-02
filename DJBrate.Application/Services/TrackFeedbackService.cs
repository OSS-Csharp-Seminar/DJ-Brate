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

    public async Task<Dictionary<Guid, string>> GetForPlaylistAsync(Guid userId, Guid playlistId)
    {
        var feedbacks = await _repo.GetByUserAndPlaylistAsync(userId, playlistId);
        return feedbacks.ToDictionary(f => f.PlaylistTrackId, f => f.FeedbackType);
    }

    public async Task ToggleFeedbackAsync(Guid userId, PlaylistTrack track, string feedbackType)
    {
        var existing = await _repo.GetByUserAndTrackAsync(userId, track.Id);

        if (existing?.FeedbackType == feedbackType)
        {
            await _repo.DeleteAsync(userId, track.Id);
            return;
        }

        await _repo.UpsertAsync(new TrackFeedback
        {
            UserId          = userId,
            PlaylistTrackId = track.Id,
            SpotifyTrackId  = track.SpotifyTrackId,
            FeedbackType    = feedbackType
        });
    }
}
