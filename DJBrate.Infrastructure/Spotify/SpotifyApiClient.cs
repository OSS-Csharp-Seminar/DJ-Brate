using SpotifyAPI.Web;
using DJBrate.Application.Interfaces;
using DJBrate.Application.Models.Spotify;

namespace DJBrate.Infrastructure.Spotify;

public class SpotifyApiClient : ISpotifyApiClient
{
    private const int TopItemsLimit     = 50;
    private const int PlaylistBatchSize = 100;

    private static SpotifyClient Client(string accessToken) => new(accessToken);

    public async Task<SpotifyProfileResponse> GetProfileAsync(string accessToken)
    {
        var profile = await Client(accessToken).UserProfile.Current();
        return new SpotifyProfileResponse
        {
            Id          = profile.Id,
            DisplayName = profile.DisplayName,
#pragma warning disable CS0618
            Email       = profile.Email,
#pragma warning restore CS0618
            Images      = profile.Images?.Select(i => new SpotifyImage { Url = i.Url }).ToList() ?? []
        };
    }

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

    public async Task<SpotifyTrack?> SearchTrackAsync(string accessToken, string artist, string title)
    {
        var query = string.IsNullOrWhiteSpace(artist)
            ? $"track:\"{title}\""
            : $"track:\"{title}\" artist:\"{artist}\"";
        var result = await Client(accessToken).Search.Item(
            new SearchRequest(SearchRequest.Types.Track, query) { Limit = 1 });

        var track = result.Tracks.Items?.FirstOrDefault();
        return track is null ? null : MapFullTrack(track);
    }

    public async Task<SpotifyArtist?> GetArtistAsync(string accessToken, string artistId)
    {
        var artist = await Client(accessToken).Artists.Get(artistId);
        return artist is null ? null : MapFullArtist(artist);
    }

    public async Task<string> CreatePlaylistAsync(
        string accessToken, string name, string description)
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

    public async Task RemoveTracksFromPlaylistAsync(string accessToken, string playlistId, List<string> trackUris)
    {
        var items = trackUris
            .Select(uri => new PlaylistRemoveItemsRequestV2.Item { Uri = uri })
            .ToList();
        await Client(accessToken).Playlists.RemovePlaylistItems(playlistId, new PlaylistRemoveItemsRequestV2 { Items = items });
    }

    public async Task UploadPlaylistCoverAsync(string accessToken, string playlistId, string base64JpegImage)
    {
        await Client(accessToken).Playlists.UploadCover(playlistId, base64JpegImage);
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
        Album      = new SpotifyAlbum
        {
            Name   = t.Album.Name,
            Images = t.Album.Images?.Select(i => new SpotifyImage { Url = i.Url }).ToList() ?? []
        }
    };

    private static SpotifyArtist MapFullArtist(FullArtist a) => new()
    {
        Id     = a.Id,
        Name   = a.Name,
        Genres = a.Genres ?? []
    };
}
