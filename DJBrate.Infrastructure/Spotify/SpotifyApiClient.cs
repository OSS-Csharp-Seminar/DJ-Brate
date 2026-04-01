using System.Globalization;
using SpotifyAPI.Web;
using DJBrate.Application.Interfaces;
using DJBrate.Application.Models.Spotify;

namespace DJBrate.Infrastructure.Spotify;

public class SpotifyApiClient : ISpotifyApiClient
{
    private const int TopItemsLimit        = 50;
    private const int RecommendationsLimit = 30;
    private const int MaxSeedArtists       = 3;
    private const int MaxSeedTracks        = 2;
    private const int PlaylistBatchSize    = 100;

    private static SpotifyClient Client(string accessToken) => new(accessToken);

    public async Task<List<SpotifyTrack>> GetTopTracksAsync(string accessToken, SpotifyTimeRange timeRange)
    {
        var result = await Client(accessToken).Personalization.GetTopTracks(new PersonalizationTopRequest
        {
            TimeRangeParam = ToLibraryTimeRange(timeRange),
            Limit = TopItemsLimit
        });
        return result.Items?.Select(MapFullTrack).ToList() ?? [];
    }

    public async Task<List<SpotifyArtist>> GetTopArtistsAsync(string accessToken, SpotifyTimeRange timeRange)
    {
        var result = await Client(accessToken).Personalization.GetTopArtists(new PersonalizationTopRequest
        {
            TimeRangeParam = ToLibraryTimeRange(timeRange),
            Limit = TopItemsLimit
        });
        return result.Items?.Select(MapFullArtist).ToList() ?? [];
    }

    public async Task<List<SpotifyTrack>> GetRecommendationsAsync(
        string accessToken,
        List<string> seedArtistIds,
        List<string> seedTrackIds,
        AudioFeatureTargets features)
    {
        var req = new RecommendationsRequest { Limit = RecommendationsLimit };

        foreach (var id in seedArtistIds.Take(MaxSeedArtists)) req.SeedArtists.Add(id);
        foreach (var id in seedTrackIds.Take(MaxSeedTracks))   req.SeedTracks.Add(id);

        SetTarget(req, "valence",      features.Valence,      "F2");
        SetTarget(req, "energy",       features.Energy,       "F2");
        SetTarget(req, "tempo",        features.Tempo,        "F0");
        SetTarget(req, "danceability", features.Danceability, "F2");
        SetTarget(req, "acousticness", features.Acousticness, "F2");

        var result = await Client(accessToken).Browse.GetRecommendations(req);
        return result.Tracks.Select(MapFullTrack).ToList();
    }

    public async Task<string> CreatePlaylistAsync(
        string accessToken, string spotifyUserId, string name, string description)
    {
        var playlist = await Client(accessToken).Playlists.Create(
            new PlaylistCreateRequest(name) { Description = description, Public = false });

        return playlist.Id!;
    }

    public async Task AddTracksToPlaylistAsync(string accessToken, string playlistId, List<string> trackUris)
    {
        foreach (var batch in trackUris.Chunk(PlaylistBatchSize))
            await Client(accessToken).Playlists.AddPlaylistItems(
                playlistId,
                new PlaylistAddItemsRequest(batch.ToList()));
    }

    private static void SetTarget(RecommendationsRequest req, string key, float? value, string format)
    {
        if (value.HasValue)
            req.Target[key] = value.Value.ToString(format, CultureInfo.InvariantCulture);
    }

    private static PersonalizationTopRequest.TimeRange ToLibraryTimeRange(SpotifyTimeRange timeRange) =>
        timeRange switch
        {
            SpotifyTimeRange.ShortTerm => PersonalizationTopRequest.TimeRange.ShortTerm,
            SpotifyTimeRange.LongTerm  => PersonalizationTopRequest.TimeRange.LongTerm,
            _                          => PersonalizationTopRequest.TimeRange.MediumTerm
        };

    private static SpotifyTrack MapFullTrack(FullTrack t) => new()
    {
        Id         = t.Id,
        Name       = t.Name,
        Uri        = t.Uri,
        DurationMs = t.DurationMs,
        PreviewUrl = t.PreviewUrl,
        Artists    = t.Artists.Select(a => new SpotifyArtistRef { Id = a.Id, Name = a.Name }).ToList(),
        Album      = new SpotifyAlbum { Name = t.Album.Name }
    };

    private static SpotifyArtist MapFullArtist(FullArtist a) => new()
    {
        Id     = a.Id,
        Name   = a.Name,
        Genres = a.Genres ?? []
    };
}
