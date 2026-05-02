using DJBrate.Application.Models.Spotify;

namespace DJBrate.Application.Interfaces;

public interface ISpotifyApiClient
{
    Task<SpotifyProfileResponse> GetProfileAsync(string accessToken);
    Task<List<SpotifyTrack>> GetTopTracksAsync(string accessToken, SpotifyTimeRange timeRange);
    Task<List<SpotifyArtist>> GetTopArtistsAsync(string accessToken, SpotifyTimeRange timeRange);
    Task<SpotifyTrack?> SearchTrackAsync(string accessToken, string artist, string title);
    Task<SpotifyArtist?> GetArtistAsync(string accessToken, string artistId);
    Task<string> CreatePlaylistAsync(string accessToken, string name, string description);
    Task AddTracksToPlaylistAsync(string accessToken, string playlistId, List<string> trackUris);
    Task RemoveTracksFromPlaylistAsync(string accessToken, string playlistId, List<string> trackUris);
    Task UploadPlaylistCoverAsync(string accessToken, string playlistId, string base64JpegImage);
}
