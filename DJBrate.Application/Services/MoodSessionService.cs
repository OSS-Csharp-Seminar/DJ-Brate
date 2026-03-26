using DJBrate.Application.Interfaces;
using DJBrate.Domain.Entities;
using DJBrate.Domain.Interfaces;

namespace DJBrate.Application.Services;

public class MoodSessionService : IMoodSessionService
{
    private readonly IMoodSessionRepository _sessionRepository;

    public MoodSessionService(IMoodSessionRepository sessionRepository)
    {
        _sessionRepository = sessionRepository;
    }

    public async Task<MoodSession?> GetSessionByIdAsync(Guid id)
        => await _sessionRepository.GetByIdAsync(id);

    public async Task<IEnumerable<MoodSession>> GetSessionsByUserIdAsync(Guid userId)
        => await _sessionRepository.GetByUserIdAsync(userId);

    public async Task<MoodSession> CreateSessionAsync(MoodSession session)
    {
        await _sessionRepository.AddAsync(session);
        return session;
    }
}
