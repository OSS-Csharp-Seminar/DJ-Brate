using DJBrate.Domain.Entities;

namespace DJBrate.Application.Interfaces;

public interface IPlaylistService
{
    Task<Playlist?> GetPlaylistByIdAsync(Guid id);
    Task<IEnumerable<Playlist>> GetPlaylistsByUserIdAsync(Guid userId);
    Task<Playlist> CreatePlaylistAsync(Playlist playlist);
}
