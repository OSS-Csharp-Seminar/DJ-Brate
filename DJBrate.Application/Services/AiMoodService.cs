using System.Text.Json;
using DJBrate.Application.Interfaces;
using DJBrate.Application.Mcp;
using DJBrate.Application.Models.Ai;
using DJBrate.Application.Models.Spotify;
using DJBrate.Domain.Entities;
using DJBrate.Domain.Interfaces;

namespace DJBrate.Application.Services;

public class AiMoodService : IAiMoodService
{
    private const int MaxToolCallRounds = 5;

    private const string DefaultSystemPrompt = """
        You are DJ Brate, an AI DJ that creates personalized Spotify playlists based on the user's mood.

        Your job:
        1. Read the user's mood prompt carefully
        2. Use the available tools to understand their music taste (call get_user_top_tracks and/or get_user_top_artists)
        3. Based on their taste + mood, call get_recommendations with appropriate seed artists/tracks and audio feature targets
        4. After getting recommendations, respond with a final JSON result

        Audio feature guidelines:
        - valence: 0.0 = sad/angry, 1.0 = happy/cheerful
        - energy: 0.0 = calm/relaxed, 1.0 = intense/energetic
        - tempo: 60-80 = slow, 100-120 = moderate, 130-160 = fast
        - danceability: 0.0 = not danceable, 1.0 = very danceable
        - acousticness: 0.0 = electronic/produced, 1.0 = acoustic/unplugged

        When you have the final track list, respond with ONLY this JSON (no markdown, no extra text):
        {
            "playlist_name": "a creative playlist name based on the mood",
            "playlist_description": "a short description of the playlist vibe",
            "detected_mood": "the mood you detected (e.g. chill, energetic, melancholic, focused)",
            "reasoning": "brief explanation of your choices",
            "audio_features": {
                "valence": 0.0-1.0,
                "energy": 0.0-1.0,
                "tempo": 60-180,
                "danceability": 0.0-1.0,
                "acousticness": 0.0-1.0
            }
        }

        Important: You MUST call at least get_user_top_artists or get_user_top_tracks first to understand the user's taste, then call get_recommendations. Do not skip tool calls.
        """;

    private readonly IAiClient _aiClient;
    private readonly McpDispatcher _mcpDispatcher;
    private readonly IAiConversationMessageRepository _messageRepo;
    private readonly IAiMoodMappingRepository _moodMappingRepo;
    private readonly IAiModelConfigRepository _configRepo;

    public AiMoodService(
        IAiClient aiClient,
        McpDispatcher mcpDispatcher,
        IAiConversationMessageRepository messageRepo,
        IAiMoodMappingRepository moodMappingRepo,
        IAiModelConfigRepository configRepo)
    {
        _aiClient        = aiClient;
        _mcpDispatcher   = mcpDispatcher;
        _messageRepo     = messageRepo;
        _moodMappingRepo = moodMappingRepo;
        _configRepo      = configRepo;
    }

    public async Task<AiMoodResult> GeneratePlaylistAsync(MoodSession session, User user)
    {
        var config = await _configRepo.GetActiveConfigAsync();
        var systemPrompt = config?.SystemPrompt ?? DefaultSystemPrompt;

        var tools = McpToolDefinitions.GetAllTools();
        var conversation = new List<AiMessage>();
        var sequenceOrder = 0;

        var userPrompt = BuildUserPrompt(session);
        conversation.Add(new AiMessage { Role = "user", Text = userPrompt });
        await SaveMessage(session.Id, "user", userPrompt, sequenceOrder++);

        var lastRecommendationResult = "";

        for (var round = 0; round < MaxToolCallRounds; round++)
        {
            var response = await _aiClient.SendMessageAsync(systemPrompt, conversation, tools);

            if (response.HasToolCalls)
            {
                foreach (var toolCall in response.ToolCalls)
                {
                    conversation.Add(new AiMessage { Role = "assistant", ToolCall = toolCall });
                    await SaveMessage(session.Id, "assistant", $"[tool_call: {toolCall.Name}]", sequenceOrder++);

                    var result = await _mcpDispatcher.ExecuteToolAsync(
                        session.Id, user, toolCall.Name, toolCall.Arguments);

                    if (toolCall.Name == McpToolDefinitions.ToolNames.GetRecommendations)
                        lastRecommendationResult = result;

                    conversation.Add(new AiMessage
                    {
                        Role = "function",
                        ToolResult = new AiToolResult
                        {
                            ToolCallId = toolCall.Name,
                            Result     = result
                        }
                    });
                    await SaveMessage(session.Id, "function", result, sequenceOrder++);
                }
            }
            else if (response.Text is not null)
            {
                await SaveMessage(session.Id, "assistant", response.Text, sequenceOrder);
                return await ParseFinalResponse(session.Id, response.Text, lastRecommendationResult);
            }
        }

        throw new InvalidOperationException("AI did not produce a final response within the allowed tool call rounds.");
    }

    private static string BuildUserPrompt(MoodSession session)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(session.PromptText))
            parts.Add(session.PromptText);

        if (!string.IsNullOrWhiteSpace(session.SelectedMood))
            parts.Add($"My mood: {session.SelectedMood}");

        if (session.SelectedGenres is { Length: > 0 })
            parts.Add($"Preferred genres: {string.Join(", ", session.SelectedGenres)}");

        if (session.EnergyLevel.HasValue)
            parts.Add($"Energy level: {session.EnergyLevel:F1}/1.0");

        if (session.Danceability.HasValue)
            parts.Add($"Danceability: {session.Danceability:F1}/1.0");

        return parts.Count > 0
            ? string.Join(". ", parts)
            : "Surprise me with a good playlist based on my listening history.";
    }

    private async Task<AiMoodResult> ParseFinalResponse(
        Guid sessionId, string aiText, string lastRecommendationResult)
    {
        var json = ExtractJson(aiText);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var audioFeatures = new AudioFeatureTargets();
        if (root.TryGetProperty("audio_features", out var af))
        {
            audioFeatures.Valence      = GetOptionalFloat(af, "valence");
            audioFeatures.Energy       = GetOptionalFloat(af, "energy");
            audioFeatures.Tempo        = GetOptionalFloat(af, "tempo");
            audioFeatures.Danceability = GetOptionalFloat(af, "danceability");
            audioFeatures.Acousticness = GetOptionalFloat(af, "acousticness");
        }

        var detectedMood = root.TryGetProperty("detected_mood", out var dm)
            ? dm.GetString() ?? "unknown"
            : "unknown";

        var reasoning = root.TryGetProperty("reasoning", out var r) ? r.GetString() : null;

        await _moodMappingRepo.AddAsync(new AiMoodMapping
        {
            SessionId         = sessionId,
            DetectedMood      = detectedMood,
            TargetValence     = audioFeatures.Valence,
            TargetEnergy      = audioFeatures.Energy,
            TargetTempo       = audioFeatures.Tempo,
            TargetDanceability = audioFeatures.Danceability,
            TargetAcousticness = audioFeatures.Acousticness,
            FlowUsed          = "mcp_tool_calling",
            AiReasoning       = reasoning
        });

        var tracks = new List<SpotifyTrack>();
        if (!string.IsNullOrEmpty(lastRecommendationResult))
        {
            tracks = ParseRecommendedTracks(lastRecommendationResult);
        }

        return new AiMoodResult
        {
            PlaylistName        = root.TryGetProperty("playlist_name", out var pn) ? pn.GetString()! : "DJ Brate Mix",
            PlaylistDescription = root.TryGetProperty("playlist_description", out var pd) ? pd.GetString()! : "Generated by DJ Brate AI",
            DetectedMood        = detectedMood,
            AiReasoning         = reasoning,
            AudioFeatures       = audioFeatures,
            RecommendedTracks   = tracks
        };
    }

    private static List<SpotifyTrack> ParseRecommendedTracks(string json)
    {
        var doc = JsonDocument.Parse(json);
        var tracks = new List<SpotifyTrack>();

        foreach (var item in doc.RootElement.EnumerateArray())
        {
            tracks.Add(new SpotifyTrack
            {
                Id         = item.GetProperty("spotify_id").GetString()!,
                Name       = item.GetProperty("name").GetString()!,
                Uri        = item.GetProperty("uri").GetString()!,
                DurationMs = item.TryGetProperty("duration_ms", out var dur) ? dur.GetInt32() : 0,
                PreviewUrl = item.TryGetProperty("preview_url", out var pv) ? pv.GetString() : null,
                Artists    = [new SpotifyArtistRef
                {
                    Id   = "",
                    Name = item.TryGetProperty("artist", out var art) ? art.GetString()! : "Unknown"
                }],
                Album = new SpotifyAlbum
                {
                    Name = item.TryGetProperty("album", out var alb) ? alb.GetString()! : ""
                }
            });
        }

        return tracks;
    }

    private static string ExtractJson(string text)
    {
        var start = text.IndexOf('{');
        var end   = text.LastIndexOf('}');
        if (start >= 0 && end > start)
            return text[start..(end + 1)];
        throw new InvalidOperationException("AI response did not contain valid JSON.");
    }

    private static float? GetOptionalFloat(JsonElement element, string property)
        => element.TryGetProperty(property, out var val) ? (float)val.GetDouble() : null;

    private async Task SaveMessage(Guid sessionId, string role, string content, int order)
    {
        await _messageRepo.AddAsync(new AiConversationMessage
        {
            SessionId     = sessionId,
            Role          = role,
            Content       = content,
            SequenceOrder = order
        });
    }
}
