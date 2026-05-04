using DJBrate.Domain.Entities;

namespace DJBrate.Application.Interfaces;

public interface IPlaylistService
{
    Task<Playlist?> GetPlaylistByIdAsync(Guid id);
    Task<IEnumerable<Playlist>> GetPlaylistsByUserIdAsync(Guid userId);
    Task<Playlist> CreatePlaylistAsync(Playlist playlist);
    Task<bool> UpdateCoverImageAsync(Guid playlistId, Guid userId, string imageUrl);
    Task SyncCoverToSpotifyAsync(Guid playlistId, Guid userId, byte[] imageBytes);
    Task<List<AiConversationMessage>> GetConversationAsync(Guid playlistId, Guid userId);
    Task<User?> GetUserAsync(Guid userId);
    Task<string?> EnableSharingAsync(Guid playlistId, Guid userId);
    Task<bool> DisableSharingAsync(Guid playlistId, Guid userId);
    Task<Playlist?> GetByShareTokenAsync(string token);
    Task<List<AiConversationMessage>> GetSharedConversationAsync(string token);
    Task<MoodSession?> GetOriginalSessionAsync(Guid playlistId, Guid userId);
}
