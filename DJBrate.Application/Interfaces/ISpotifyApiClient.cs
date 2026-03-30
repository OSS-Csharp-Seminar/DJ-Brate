using DJBrate.Application.Models.Spotify;

namespace DJBrate.Application.Interfaces;

public interface ISpotifyApiClient
{
    Task<List<SpotifyTrack>> GetTopTracksAsync(string accessToken, string timeRange);
    Task<List<SpotifyArtist>> GetTopArtistsAsync(string accessToken, string timeRange);
    Task<List<SpotifyTrack>> GetRecommendationsAsync(string accessToken, List<string> seedArtistIds, List<string> seedTrackIds, AudioFeatureTargets features);
    Task<string> CreatePlaylistAsync(string accessToken, string spotifyUserId, string name, string description);
    Task AddTracksToPlaylistAsync(string accessToken, string playlistId, List<string> trackUris);
}
