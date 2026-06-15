using DJBrate.Domain.Entities;

namespace DJBrate.Domain.Interfaces;

public interface ITrackFeedbackRepository
{
    Task<TrackFeedback?> GetByUserAndSpotifyTrackAsync(Guid userId, string spotifyTrackId);
    Task<Dictionary<Guid, string>> GetPlaylistFeedbackMapAsync(Guid userId, Guid playlistId);
    Task<List<TrackFeedback>> GetPlaylistFeedbackAsync(Guid userId, Guid playlistId);
    Task UpsertAsync(TrackFeedback feedback);
    Task DeleteAsync(Guid userId, string spotifyTrackId);
}
