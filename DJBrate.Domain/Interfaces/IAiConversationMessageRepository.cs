using DJBrate.Domain.Entities;

namespace DJBrate.Domain.Interfaces;

// Definira dohvat AI poruka po sesiji ili playlisti.
public interface IAiConversationMessageRepository : IRepository<AiConversationMessage>
{
    Task<List<AiConversationMessage>> GetBySessionIdAsync(Guid sessionId);
    Task<List<AiConversationMessage>> GetByPlaylistIdAsync(Guid playlistId);
}
