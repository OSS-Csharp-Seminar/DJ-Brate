using DJBrate.Domain.Entities;

namespace DJBrate.Application.Interfaces;

// Definira dohvat i toggle like/dislike feedbacka.
public interface ITrackFeedbackService
{
    Task<Dictionary<Guid, string>> GetForPlaylistAsync(Guid userId, Guid playlistId);
    Task ToggleFeedbackAsync(Guid userId, PlaylistTrack track, string feedbackType);
}
