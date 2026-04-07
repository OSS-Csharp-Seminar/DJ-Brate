using System.Diagnostics;
using System.Text.Json;
using DJBrate.Application.Interfaces;
using DJBrate.Application.Models.Spotify;
using DJBrate.Domain.Entities;
using DJBrate.Domain.Interfaces;

namespace DJBrate.Application.Mcp;

public class McpDispatcher
{
    private readonly ISpotifyApiClient _spotifyClient;
    private readonly ISpotifyTokenService _tokenService;
    private readonly IUserTopTrackRepository _topTrackRepo;
    private readonly IUserTopArtistRepository _topArtistRepo;
    private readonly IMcpToolCallRepository _toolCallRepo;

    public McpDispatcher(
        ISpotifyApiClient spotifyClient,
        ISpotifyTokenService tokenService,
        IUserTopTrackRepository topTrackRepo,
        IUserTopArtistRepository topArtistRepo,
        IMcpToolCallRepository toolCallRepo)
    {
        _spotifyClient = spotifyClient;
        _tokenService  = tokenService;
        _topTrackRepo  = topTrackRepo;
        _topArtistRepo = topArtistRepo;
        _toolCallRepo  = toolCallRepo;
    }

    public async Task<string> ExecuteToolAsync(Guid sessionId, User user, string toolName, JsonDocument arguments)
    {
        var sw = Stopwatch.StartNew();
        var toolCall = new McpToolCall
        {
            SessionId       = sessionId,
            ToolName        = toolName,
            InputParameters = arguments
        };

        try
        {
            var result = toolName switch
            {
                McpToolDefinitions.ToolNames.GetUserTopTracks   => await HandleGetTopTracks(user, arguments),
                McpToolDefinitions.ToolNames.GetUserTopArtists  => await HandleGetTopArtists(user, arguments),
                McpToolDefinitions.ToolNames.GetRecommendations => await HandleGetRecommendations(user, arguments),
                _ => throw new ArgumentException($"Unknown tool: {toolName}")
            };

            sw.Stop();
            toolCall.Success      = true;
            toolCall.OutputResult = JsonDocument.Parse(result);
            toolCall.DurationMs   = (int)sw.ElapsedMilliseconds;
            await _toolCallRepo.AddAsync(toolCall);

            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            toolCall.Success      = false;
            toolCall.ErrorMessage = ex.Message;
            toolCall.DurationMs   = (int)sw.ElapsedMilliseconds;
            await _toolCallRepo.AddAsync(toolCall);

            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    private async Task<string> HandleGetTopTracks(User user, JsonDocument args)
    {
        var timeRange = ParseTimeRange(args);
        var tracks = await _topTrackRepo.GetByUserAndTimeRangeAsync(user.Id, timeRange.ToApiString());

        var result = tracks.Select(t => new
        {
            spotify_id = t.SpotifyTrackId,
            name       = t.TrackName,
            artist     = t.ArtistName,
            rank       = t.RankPosition
        });

        return JsonSerializer.Serialize(result);
    }

    private async Task<string> HandleGetTopArtists(User user, JsonDocument args)
    {
        var timeRange = ParseTimeRange(args);
        var artists = await _topArtistRepo.GetByUserAndTimeRangeAsync(user.Id, timeRange.ToApiString());

        var result = artists.Select(a => new
        {
            spotify_id = a.SpotifyArtistId,
            name       = a.ArtistName,
            genres     = a.Genres ?? [],
            rank       = a.RankPosition
        });

        return JsonSerializer.Serialize(result);
    }

    private async Task<string> HandleGetRecommendations(User user, JsonDocument args)
    {
        var accessToken = await _tokenService.EnsureValidTokenAsync(user);
        var root = args.RootElement;

        var seedArtists = root.TryGetProperty("seed_artist_ids", out var sa)
            ? sa.EnumerateArray().Select(e => e.GetString()!).ToList()
            : [];

        var seedTracks = root.TryGetProperty("seed_track_ids", out var st)
            ? st.EnumerateArray().Select(e => e.GetString()!).ToList()
            : [];

        var features = new AudioFeatureTargets
        {
            Valence      = GetOptionalFloat(root, "target_valence"),
            Energy       = GetOptionalFloat(root, "target_energy"),
            Tempo        = GetOptionalFloat(root, "target_tempo"),
            Danceability = GetOptionalFloat(root, "target_danceability"),
            Acousticness = GetOptionalFloat(root, "target_acousticness")
        };

        var tracks = await _spotifyClient.GetRecommendationsAsync(accessToken, seedArtists, seedTracks, features);

        var result = tracks.Select(t => new
        {
            spotify_id = t.Id,
            name       = t.Name,
            uri        = t.Uri,
            artist     = t.Artists.FirstOrDefault()?.Name ?? "Unknown",
            album      = t.Album.Name,
            duration_ms = t.DurationMs,
            preview_url = t.PreviewUrl
        });

        return JsonSerializer.Serialize(result);
    }

    private static SpotifyTimeRange ParseTimeRange(JsonDocument args)
    {
        if (args.RootElement.TryGetProperty("time_range", out var tr))
        {
            return tr.GetString() switch
            {
                "short_term" => SpotifyTimeRange.ShortTerm,
                "long_term"  => SpotifyTimeRange.LongTerm,
                _            => SpotifyTimeRange.MediumTerm
            };
        }
        return SpotifyTimeRange.MediumTerm;
    }

    private static float? GetOptionalFloat(JsonElement root, string property)
        => root.TryGetProperty(property, out var val) ? (float)val.GetDouble() : null;
}
