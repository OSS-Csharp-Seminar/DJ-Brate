using DJBrate.Domain.Entities;

namespace DJBrate.Domain.Interfaces;

// Definira upite i promjene playlista i njihovih pjesama.
public interface IPlaylistRepository : IRepository<Playlist>
{
    Task<IEnumerable<Playlist>> GetByUserIdAsync(Guid userId);
    Task<Playlist?> GetByIdWithTracksAsync(Guid id);
    Task<Playlist?> GetByShareTokenAsync(string token);
    Task RemoveTracksAsync(Guid playlistId, List<string> spotifyTrackIds);
    Task AddTracksAsync(Guid playlistId, List<PlaylistTrack> tracks);
}
