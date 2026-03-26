using DJBrate.Application.Interfaces;
using DJBrate.Domain.Entities;
using DJBrate.Domain.Interfaces;

namespace DJBrate.Application.Services;

public class PlaylistService : IPlaylistService
{
    private readonly IPlaylistRepository _playlistRepository;

    public PlaylistService(IPlaylistRepository playlistRepository)
    {
        _playlistRepository = playlistRepository;
    }

    public async Task<Playlist?> GetPlaylistByIdAsync(Guid id)
        => await _playlistRepository.GetByIdAsync(id);

    public async Task<IEnumerable<Playlist>> GetPlaylistsByUserIdAsync(Guid userId)
        => await _playlistRepository.GetByUserIdAsync(userId);

    public async Task<Playlist> CreatePlaylistAsync(Playlist playlist)
    {
        await _playlistRepository.AddAsync(playlist);
        return playlist;
    }
}
