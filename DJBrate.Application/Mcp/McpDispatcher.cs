using System.Diagnostics;
using System.Text.Json;
using DJBrate.Application.Interfaces;
using DJBrate.Application.Models.Spotify;
using DJBrate.Domain.Entities;
using DJBrate.Domain.Interfaces;

namespace DJBrate.Application.Mcp;

public class McpDispatcher
{
    private readonly IUserTopTrackRepository  _topTrackRepo;
    private readonly IUserTopArtistRepository _topArtistRepo;
    private readonly IMcpToolCallRepository   _toolCallRepo;
    private readonly IPlaylistRepository      _playlistRepo;
    private readonly ISpotifyApiClient        _spotifyClient;
    private readonly ISpotifyTokenService     _tokenService;

    public McpDispatcher(
        IUserTopTrackRepository  topTrackRepo,
        IUserTopArtistRepository topArtistRepo,
        IMcpToolCallRepository   toolCallRepo,
        IPlaylistRepository      playlistRepo,
        ISpotifyApiClient        spotifyClient,
        ISpotifyTokenService     tokenService)
    {
        _topTrackRepo  = topTrackRepo;
        _topArtistRepo = topArtistRepo;
        _toolCallRepo  = toolCallRepo;
        _playlistRepo  = playlistRepo;
        _spotifyClient = spotifyClient;
        _tokenService  = tokenService;
    }

    public async Task<string> ExecuteToolAsync(McpExecutionContext ctx, string toolName, JsonDocument arguments)
    {
        var sw = Stopwatch.StartNew();
        var toolCall = new McpToolCall
        {
            SessionId       = ctx.SessionId,
            ToolName        = toolName,
            InputParameters = arguments
        };

        var result = toolName switch
        {
            McpToolDefinitions.ToolNames.GetUserTopTracks         => await HandleGetTopTracks(ctx.User, arguments),
            McpToolDefinitions.ToolNames.GetUserTopArtists        => await HandleGetTopArtists(ctx.User, arguments),
            McpToolDefinitions.ToolNames.GetCurrentPlaylistTracks => await HandleGetCurrentTracks(ctx),
            McpToolDefinitions.ToolNames.RemoveTracks             => await HandleRemoveTracks(ctx, arguments),
            McpToolDefinitions.ToolNames.AddTracks                => await HandleAddTracks(ctx, arguments),
            _ => throw new ArgumentException($"Unknown tool: {toolName}")
        };

        sw.Stop();
        toolCall.Success      = true;
        toolCall.OutputResult = JsonDocument.Parse(result);
        toolCall.DurationMs   = (int)sw.ElapsedMilliseconds;
        await _toolCallRepo.AddAsync(toolCall);

        return result;
    }

    private async Task<string> HandleGetTopTracks(User user, JsonDocument args)
    {
        var timeRange = ParseTimeRange(args);
        var tracks = await _topTrackRepo.GetByUserAndTimeRangeAsync(user.Id, timeRange.ToApiString());
        var result = tracks.Select(t => new { spotify_id = t.SpotifyTrackId, name = t.TrackName, artist = t.ArtistName, rank = t.RankPosition });
        return JsonSerializer.Serialize(result);
    }

    private async Task<string> HandleGetTopArtists(User user, JsonDocument args)
    {
        var timeRange = ParseTimeRange(args);
        var artists = await _topArtistRepo.GetByUserAndTimeRangeAsync(user.Id, timeRange.ToApiString());
        var result = artists.Select(a => new { spotify_id = a.SpotifyArtistId, name = a.ArtistName, genres = a.Genres ?? [], rank = a.RankPosition });
        return JsonSerializer.Serialize(result);
    }

    private async Task<string> HandleGetCurrentTracks(McpExecutionContext ctx)
    {
        if (ctx.PlaylistId is null) return "[]";
        var playlist = await _playlistRepo.GetByIdWithTracksAsync(ctx.PlaylistId.Value);
        if (playlist is null) return "[]";
        var result = playlist.PlaylistTracks
            .OrderBy(t => t.Position)
            .Select(t => new { spotify_id = t.SpotifyTrackId, title = t.TrackName, artist = t.ArtistName, position = t.Position });
        return JsonSerializer.Serialize(result);
    }

    private async Task<string> HandleRemoveTracks(McpExecutionContext ctx, JsonDocument args)
    {
        if (ctx.PlaylistId is null) return """{"removed": 0}""";

        var ids = new List<string>();
        if (args.RootElement.TryGetProperty("spotify_track_ids", out var arr))
            foreach (var el in arr.EnumerateArray())
            {
                var id = el.GetString();
                if (!string.IsNullOrWhiteSpace(id)) ids.Add(id);
            }
        if (ids.Count == 0) return """{"removed": 0}""";

        var playlist = await _playlistRepo.GetByIdAsync(ctx.PlaylistId.Value);
        if (playlist?.SpotifyPlaylistId is not null)
        {
            var token = await _tokenService.EnsureValidTokenAsync(ctx.User);
            var uris = ids.Select(id => $"spotify:track:{id}").ToList();
            var removeTask = _spotifyClient.RemoveTracksFromPlaylistAsync(token, playlist.SpotifyPlaylistId, uris);
            await ((Task)removeTask).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }

        await _playlistRepo.RemoveTracksAsync(ctx.PlaylistId.Value, ids);
        return JsonSerializer.Serialize(new { removed = ids.Count });
    }

    private async Task<string> HandleAddTracks(McpExecutionContext ctx, JsonDocument args)
    {
        if (ctx.PlaylistId is null) return """{"added": 0}""";

        var token = await _tokenService.EnsureValidTokenAsync(ctx.User);
        var resolved = new List<SpotifyTrack>();

        if (args.RootElement.TryGetProperty("tracks", out var tracksEl))
        {
            foreach (var item in tracksEl.EnumerateArray())
            {
                var artist = item.TryGetProperty("artist", out var a) ? a.GetString() : null;
                var title  = item.TryGetProperty("title",  out var t) ? t.GetString() : null;
                if (string.IsNullOrWhiteSpace(artist) || string.IsNullOrWhiteSpace(title)) continue;

                var match = await _spotifyClient.SearchTrackAsync(token, artist, title);
                if (match is not null) resolved.Add(match);
            }
        }
        if (resolved.Count == 0) return """{"added": 0}""";

        var playlist = await _playlistRepo.GetByIdAsync(ctx.PlaylistId.Value);
        if (playlist?.SpotifyPlaylistId is not null)
        {
            var uris = resolved.Select(t => t.Uri).ToList();
            var addTask = _spotifyClient.AddTracksToPlaylistAsync(token, playlist.SpotifyPlaylistId, uris);
            await ((Task)addTask).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }

        var tracks = resolved.Select(t => new PlaylistTrack
        {
            SpotifyTrackId  = t.Id,
            TrackName       = t.Name,
            SpotifyArtistId = t.Artists.FirstOrDefault()?.Id ?? "",
            ArtistName      = t.Artists.FirstOrDefault()?.Name ?? "Unknown",
            AlbumName       = t.Album?.Name,
            AlbumImageUrl   = t.Album?.Images.FirstOrDefault()?.Url,
            DurationMs      = t.DurationMs,
            PreviewUrl      = t.PreviewUrl,
            Position        = 0
        }).ToList();

        await _playlistRepo.AddTracksAsync(ctx.PlaylistId.Value, tracks);
        return JsonSerializer.Serialize(new { added = resolved.Count });
    }

    private static SpotifyTimeRange ParseTimeRange(JsonDocument args)
    {
        if (args.RootElement.TryGetProperty("time_range", out var tr))
            return tr.GetString() switch
            {
                "short_term" => SpotifyTimeRange.ShortTerm,
                "long_term"  => SpotifyTimeRange.LongTerm,
                _            => SpotifyTimeRange.MediumTerm
            };
        return SpotifyTimeRange.MediumTerm;
    }
}
