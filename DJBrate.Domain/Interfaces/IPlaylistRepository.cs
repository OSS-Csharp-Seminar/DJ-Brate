using DJBrate.Domain.Entities;

namespace DJBrate.Domain.Interfaces;

public interface IPlaylistRepository : IRepository<Playlist>
{
    Task<IEnumerable<Playlist>> GetByUserIdAsync(Guid userId);
}
