using DJBrate.Domain.Entities;

namespace DJBrate.Application.Interfaces;

public interface ITrackFeedbackService
{
    Task<Dictionary<Guid, string>> GetForPlaylistAsync(Guid userId, Guid playlistId);
    Task ToggleFeedbackAsync(Guid userId, PlaylistTrack track, string feedbackType);
}
