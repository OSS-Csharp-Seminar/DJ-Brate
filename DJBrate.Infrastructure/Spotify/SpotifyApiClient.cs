using SpotifyAPI.Web;
using DJBrate.Application.Interfaces;
using DJBrate.Application.Models.Spotify;

namespace DJBrate.Infrastructure.Spotify;

public class SpotifyApiClient : ISpotifyApiClient
{
    private const int TopItemsLimit     = 50;
    private const int PlaylistBatchSize = 100;

    // Stvara Spotify SDK klijent s korisnikovim access tokenom.
    private static SpotifyClient Client(string accessToken) => new(accessToken);

    // Dohvaca osnovni profil trenutno prijavljenog Spotify korisnika.
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

    // Dohvaca korisnikove top pjesme za odabrani vremenski period.
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
    } //metoda poziva SpotifyPersonalization API da dohvati top pjesme i top izvođače korisnika za zadani vremenski interval 
    // te rezultat pretvara u listu SpotifyTrack DTO objekata i SpotifyArtist DTO objekata.

    public async Task<SpotifyTrack?> SearchTrackAsync(string accessToken, string artist, string title) // salje upit na Spotify Search API da pronadje pjesmu po nazivu i izvođaču, 
                                                                                                      // ako je pjesma pronađena vraća SpotifyTrack DTO objekat, ako nije vraća null.
    {
        var queries = string.IsNullOrWhiteSpace(artist)
            ? new[] { $"track:\"{title}\"" }
            : new[]
            {
                $"track:\"{title}\" artist:\"{artist}\"",
                $"{title} {artist}",
                $"track:\"{title}\""
            };

        foreach (var query in queries)
        {
            var result = await Client(accessToken).Search.Item(
                new SearchRequest(SearchRequest.Types.Track, query) { Limit = 1 });
            var track = result.Tracks.Items?.FirstOrDefault();
            if (track is not null)
                return MapFullTrack(track);
        }

        return null;
    }

    // Dohvaca Spotify izvodaca i njegove zanrove prema ID-u.
    public async Task<SpotifyArtist?> GetArtistAsync(string accessToken, string artistId)
    {
        var artist = await Client(accessToken).Artists.Get(artistId);
        return artist is null ? null : MapFullArtist(artist);
    }

    // Stvara privatnu playlistu na korisnikovom Spotify racunu.
    public async Task<string> CreatePlaylistAsync(
        string accessToken, string name, string description)
    {
        var playlist = await Client(accessToken).Playlists.Create(
            new PlaylistCreateRequest(name) { Description = description, Public = false });
        return playlist.Id!;
    }

    // Dodaje pjesme u Spotify playlistu u paketima do 100 URI-jeva.
    public async Task AddTracksToPlaylistAsync(string accessToken, string playlistId, List<string> trackUris)
    {
        foreach (var batch in trackUris.Chunk(PlaylistBatchSize))
            await Client(accessToken).Playlists.AddPlaylistItems(
                playlistId,
                new PlaylistAddItemsRequest(batch.ToList()));
    }

    // Uklanja zadane pjesme iz Spotify playliste.
    public async Task RemoveTracksFromPlaylistAsync(string accessToken, string playlistId, List<string> trackUris)
    {
        var items = trackUris
            .Select(uri => new PlaylistRemoveItemsRequestV2.Item { Uri = uri })
            .ToList();
        await Client(accessToken).Playlists.RemovePlaylistItems(playlistId, new PlaylistRemoveItemsRequestV2 { Items = items });
    }

    // Salje Base64 JPEG kao naslovnu sliku Spotify playliste.
    public async Task UploadPlaylistCoverAsync(string accessToken, string playlistId, string base64JpegImage)
    {
        await Client(accessToken).Playlists.UploadCover(playlistId, base64JpegImage);
    }

    // Pretvara interni time range u vrijednost Spotify SDK-a.
    private static PersonalizationTopRequest.TimeRange ToLibraryTimeRange(SpotifyTimeRange timeRange) =>
        timeRange switch
        {
            SpotifyTimeRange.ShortTerm => PersonalizationTopRequest.TimeRange.ShortTerm,
            SpotifyTimeRange.LongTerm  => PersonalizationTopRequest.TimeRange.LongTerm,
            _                          => PersonalizationTopRequest.TimeRange.MediumTerm
        };

    // Pretvara Spotify SDK track u aplikacijski SpotifyTrack DTO.
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

    // Pretvara Spotify SDK artist u aplikacijski SpotifyArtist DTO.
    private static SpotifyArtist MapFullArtist(FullArtist a) => new()
    {
        Id     = a.Id,
        Name   = a.Name,
        Genres = a.Genres ?? []
    };
}
