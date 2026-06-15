using DJBrate.Application.Interfaces;
using DJBrate.Domain.Entities;
using DJBrate.Domain.Interfaces;

namespace DJBrate.Application.Services;

public class MoodSessionService : IMoodSessionService
{
    private readonly IMoodSessionRepository _sessionRepository;
    private readonly IPlaylistRepository _playlistRepository;
    private readonly IAiModelConfigRepository _configRepository;
    private readonly IAiMoodService _aiMoodService;
    private readonly ISpotifyApiClient _spotifyClient;
    private readonly ISpotifyTokenService _tokenService;

    public MoodSessionService(
        IMoodSessionRepository sessionRepository,
        IPlaylistRepository playlistRepository,
        IAiModelConfigRepository configRepository,
        IAiMoodService aiMoodService,
        ISpotifyApiClient spotifyClient,
        ISpotifyTokenService tokenService)
    {
        _sessionRepository  = sessionRepository;
        _playlistRepository = playlistRepository;
        _configRepository   = configRepository;
        _aiMoodService      = aiMoodService;
        _spotifyClient      = spotifyClient;
        _tokenService       = tokenService;
    }

    public async Task<MoodSession?> GetSessionByIdAsync(Guid id)
        => await _sessionRepository.GetByIdAsync(id);

    public async Task<IEnumerable<MoodSession>> GetSessionsByUserIdAsync(Guid userId)
        => await _sessionRepository.GetByUserIdAsync(userId);

    public async Task<MoodSession> CreateSessionAsync(MoodSession session)
    {
        await _sessionRepository.AddAsync(session);
        return session;
    }

    public async Task<PlaylistGenerationResult> GenerateAsync(
        User user,
        string? promptText,
        string? selectedMood,
        string[]? selectedGenres,
        float? energyLevel,
        float? danceability,
        string? playlistNameOverride,
        string? playlistDescriptionOverride)
    {
        var config = await _configRepository.GetActiveConfigAsync()
            ?? throw new InvalidOperationException("No active AI model config found.");

        var session = new MoodSession
        {
            UserId         = user.Id,
            AiConfigId     = config.Id,
            PromptText     = promptText,
            SelectedMood   = selectedMood,
            SelectedGenres = selectedGenres,
            EnergyLevel    = energyLevel,
            Danceability   = danceability,
            Status         = MoodSessionStatuses.Creating
        };
        await _sessionRepository.AddAsync(session);

        var aiTask = _aiMoodService.GeneratePlaylistAsync(session, user, config);
        await ((Task)aiTask).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        if (!aiTask.IsCompletedSuccessfully)
        {
            await MarkFailedAsync(session);
            throw new InvalidOperationException(
                aiTask.Exception?.InnerException?.Message ?? "AI generation failed.");
        }

        var result = aiTask.Result;

        if (result.RecommendedTracks.Count == 0)
        {
            await MarkFailedAsync(session);
            throw new InvalidOperationException(
                "AI couldn't find tracks for that mood. Try a different prompt or mood tag.");
        }

        var finalName = string.IsNullOrWhiteSpace(playlistNameOverride)
            ? result.PlaylistName
            : playlistNameOverride.Trim();

        var finalDescription = string.IsNullOrWhiteSpace(playlistDescriptionOverride)
            ? result.PlaylistDescription
            : playlistDescriptionOverride.Trim();

        var defaultCover = result.RecommendedTracks
            .Select(t => t.Album?.Images.FirstOrDefault()?.Url)
            .FirstOrDefault(url => !string.IsNullOrEmpty(url));

        var playlist = new Playlist
        {
            SessionId   = session.Id,
            UserId      = user.Id,
            Name        = finalName,
            Description = finalDescription,
            ImageUrl    = defaultCover,
            TrackCount  = result.RecommendedTracks.Count,
            PlaylistTracks = result.RecommendedTracks.Select((t, i) => new PlaylistTrack
            {
                SpotifyTrackId  = t.Id,
                TrackName       = t.Name,
                SpotifyArtistId = t.Artists.FirstOrDefault()?.Id ?? "",
                ArtistName      = t.Artists.FirstOrDefault()?.Name ?? "Unknown",
                AlbumName       = t.Album?.Name,
                AlbumImageUrl   = t.Album?.Images.FirstOrDefault()?.Url,
                DurationMs      = t.DurationMs,
                PreviewUrl      = t.PreviewUrl,
                Valence         = result.AudioFeatures.Valence,
                Energy          = result.AudioFeatures.Energy,
                Tempo           = result.AudioFeatures.Tempo,
                Danceability    = result.AudioFeatures.Danceability,
                Acousticness    = result.AudioFeatures.Acousticness,
                AiNote          = t.AiNote,
                Position        = i + 1
            }).ToList()
        };

        var accessToken = await _tokenService.EnsureValidTokenAsync(user);
        var spotifyPlaylistId = await _spotifyClient.CreatePlaylistAsync(
            accessToken, finalName, finalDescription);
        playlist.SpotifyPlaylistId = spotifyPlaylistId;

        var trackUris = result.RecommendedTracks.Select(t => t.Uri).ToList();
        await _spotifyClient.AddTracksToPlaylistAsync(accessToken, spotifyPlaylistId, trackUris);

        await _playlistRepository.AddAsync(playlist);

        session.Status      = MoodSessionStatuses.Completed;
        session.CompletedAt = DateTime.UtcNow;
        await _sessionRepository.UpdateAsync(session);

        return new PlaylistGenerationResult
        {
            Playlist = playlist,
            Insights = result
        };
    }

    public async Task<string> RefineAsync(User user, Playlist playlist, string userMessage)
    {
        var config = await _configRepository.GetActiveConfigAsync()
            ?? throw new InvalidOperationException("No active AI model config found.");

        var session = new MoodSession
        {
            UserId             = user.Id,
            AiConfigId         = config.Id,
            SessionType        = MoodSessionTypes.Edit,
            RefinesPlaylistId  = playlist.Id,
            Status             = MoodSessionStatuses.Creating
        };
        await _sessionRepository.AddAsync(session);

        var refineTask = _aiMoodService.RefinePlaylistAsync(session, playlist, user, userMessage, config);
        await ((Task)refineTask).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        if (!refineTask.IsCompletedSuccessfully)
        {
            await MarkFailedAsync(session);
            throw new InvalidOperationException(
                refineTask.Exception?.InnerException?.Message ?? "AI refinement failed.");
        }

        session.Status      = MoodSessionStatuses.Completed;
        session.CompletedAt = DateTime.UtcNow;
        await _sessionRepository.UpdateAsync(session);

        return refineTask.Result;
    }

    private async Task MarkFailedAsync(MoodSession session)
    {
        session.Status      = MoodSessionStatuses.Failed;
        session.CompletedAt = DateTime.UtcNow;
        await _sessionRepository.UpdateAsync(session);
    }
}
