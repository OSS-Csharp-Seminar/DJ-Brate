using DJBrate.Application.Interfaces;
using DJBrate.Application.Models.Spotify;
using DJBrate.Domain.Entities;
using DJBrate.Domain.Interfaces;

namespace DJBrate.Application.Services;

public class SpotifyDataSyncService : ISpotifyDataSyncService
{
    private static readonly SpotifyTimeRange[] TimeRanges =
        [SpotifyTimeRange.ShortTerm, SpotifyTimeRange.MediumTerm, SpotifyTimeRange.LongTerm];

    private readonly IUserRepository           _userRepository;
    private readonly IUserTopTrackRepository   _topTrackRepository;
    private readonly IUserTopArtistRepository  _topArtistRepository;
    private readonly ISpotifyApiClient         _spotifyApiClient;
    private readonly ISpotifyTokenService      _tokenService;

    public SpotifyDataSyncService(
        IUserRepository userRepository,
        IUserTopTrackRepository topTrackRepository,
        IUserTopArtistRepository topArtistRepository,
        ISpotifyApiClient spotifyApiClient,
        ISpotifyTokenService tokenService)
    {
        _userRepository      = userRepository;
        _topTrackRepository  = topTrackRepository;
        _topArtistRepository = topArtistRepository;
        _spotifyApiClient    = spotifyApiClient;
        _tokenService        = tokenService;
    }

    public async Task SyncUserTopDataAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new InvalidOperationException($"User {userId} not found.");

        var accessToken = await _tokenService.EnsureValidTokenAsync(user);

        foreach (var timeRange in TimeRanges)
        {
            var timeRangeStr = timeRange.ToApiString();
            var tracks  = await _spotifyApiClient.GetTopTracksAsync(accessToken, timeRange);
            var artists = await _spotifyApiClient.GetTopArtistsAsync(accessToken, timeRange);

            await _topTrackRepository.DeleteByUserAndTimeRangeAsync(userId, timeRangeStr);
            for (var i = 0; i < tracks.Count; i++)
            {
                var t = tracks[i];
                await _topTrackRepository.AddAsync(new UserTopTrack
                {
                    UserId          = userId,
                    SpotifyTrackId  = t.Id,
                    TrackName       = t.Name,
                    SpotifyArtistId = t.Artists.FirstOrDefault()?.Id ?? "",
                    ArtistName      = t.Artists.FirstOrDefault()?.Name ?? "",
                    TimeRange       = timeRangeStr,
                    RankPosition    = i + 1,
                    SyncedAt        = DateTime.UtcNow
                });
            }

            await _topArtistRepository.DeleteByUserAndTimeRangeAsync(userId, timeRangeStr);
            for (var i = 0; i < artists.Count; i++)
            {
                var a = artists[i];
                await _topArtistRepository.AddAsync(new UserTopArtist
                {
                    UserId          = userId,
                    SpotifyArtistId = a.Id,
                    ArtistName      = a.Name,
                    Genres          = a.Genres.ToArray(),
                    TimeRange       = timeRangeStr,
                    RankPosition    = i + 1,
                    SyncedAt        = DateTime.UtcNow
                });
            }
        }
    }
}
