using DJBrate.Domain.Entities;

namespace DJBrate.Application.Interfaces;

public interface IMoodSessionService
{
    Task<MoodSession?> GetSessionByIdAsync(Guid id);
    Task<IEnumerable<MoodSession>> GetSessionsByUserIdAsync(Guid userId);
    Task<MoodSession> CreateSessionAsync(MoodSession session);
}
