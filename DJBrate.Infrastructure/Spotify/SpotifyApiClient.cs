using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DJBrate.Application.Interfaces;
using DJBrate.Application.Models.Spotify;

namespace DJBrate.Infrastructure.Spotify;

public class SpotifyApiClient : ISpotifyApiClient
{
    private readonly HttpClient _http;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public SpotifyApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<SpotifyTrack>> GetTopTracksAsync(string accessToken, string timeRange)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"me/top/tracks?time_range={timeRange}&limit=50");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        return doc.RootElement.GetProperty("items")
                  .Deserialize<List<SpotifyTrack>>(JsonOptions) ?? [];
    }

    public async Task<List<SpotifyArtist>> GetTopArtistsAsync(string accessToken, string timeRange)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"me/top/artists?time_range={timeRange}&limit=50");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        return doc.RootElement.GetProperty("items")
                  .Deserialize<List<SpotifyArtist>>(JsonOptions) ?? [];
    }

    public async Task<List<SpotifyTrack>> GetRecommendationsAsync(
        string accessToken,
        List<string> seedArtistIds,
        List<string> seedTrackIds,
        AudioFeatureTargets features)
    {
        var query = new StringBuilder("recommendations?limit=30");

        if (seedArtistIds.Count > 0)
            query.Append($"&seed_artists={string.Join(",", seedArtistIds.Take(3))}");
        if (seedTrackIds.Count > 0)
            query.Append($"&seed_tracks={string.Join(",", seedTrackIds.Take(2))}");

        if (features.Valence.HasValue)    query.Append($"&target_valence={features.Valence:F2}");
        if (features.Energy.HasValue)     query.Append($"&target_energy={features.Energy:F2}");
        if (features.Tempo.HasValue)      query.Append($"&target_tempo={features.Tempo:F0}");
        if (features.Danceability.HasValue) query.Append($"&target_danceability={features.Danceability:F2}");
        if (features.Acousticness.HasValue) query.Append($"&target_acousticness={features.Acousticness:F2}");

        using var request = new HttpRequestMessage(HttpMethod.Get, query.ToString());
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        return doc.RootElement.GetProperty("tracks")
                  .Deserialize<List<SpotifyTrack>>(JsonOptions) ?? [];
    }

    public async Task<string> CreatePlaylistAsync(
        string accessToken, string spotifyUserId, string name, string description)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post,
            $"users/{spotifyUserId}/playlists");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = JsonContent.Create(new { name, description, @public = false });

        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        return doc.RootElement.GetProperty("id").GetString()!;
    }

    public async Task AddTracksToPlaylistAsync(string accessToken, string playlistId, List<string> trackUris)
    {
        foreach (var batch in trackUris.Chunk(100))
        {
            using var request = new HttpRequestMessage(HttpMethod.Post,
                $"playlists/{playlistId}/tracks");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Content = JsonContent.Create(new { uris = batch });

            var response = await _http.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }
    }
}
