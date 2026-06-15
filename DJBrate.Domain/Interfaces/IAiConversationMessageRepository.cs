using DJBrate.Domain.Entities;

namespace DJBrate.Domain.Interfaces;

public interface IAiConversationMessageRepository : IRepository<AiConversationMessage>
{
    Task<List<AiConversationMessage>> GetBySessionIdAsync(Guid sessionId);
    Task<List<AiConversationMessage>> GetByPlaylistIdAsync(Guid playlistId);
}
