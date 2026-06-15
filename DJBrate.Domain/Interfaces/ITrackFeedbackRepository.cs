using DJBrate.Domain.Entities;

namespace DJBrate.Domain.Interfaces;

// Definira citanje i spremanje like/dislike feedbacka.
public interface ITrackFeedbackRepository
{
    Task<TrackFeedback?> GetByUserAndTrackAsync(Guid userId, Guid playlistTrackId);
    Task<List<TrackFeedback>> GetByUserAndPlaylistAsync(Guid userId, Guid playlistId);
    Task UpsertAsync(TrackFeedback feedback);
    Task DeleteAsync(Guid userId, Guid playlistTrackId);
}
