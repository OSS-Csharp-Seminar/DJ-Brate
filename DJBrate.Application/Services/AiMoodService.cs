using System.Text.Json;
using System.Text.RegularExpressions;
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

    private static readonly Regex RequestedTrackPattern = new(
        @"(?:must\s+(?:include|have))\s+(.+?)(?:\s+by\s+(.+?))?(?:\s*[,;.]|$)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private const string DefaultSystemPrompt = """
        You are DJ Brate, an AI DJ that creates personalized Spotify playlists based on the user's prompt and mood.

        Your job:
        1. Read the user's prompt and mood tags carefully. The prompt is the most important signal — it drives everything about the playlist's composition, specificity, and vibe.
        2. Decide whether you need the user's listening history. By default, DO NOT call any tools - just generate the playlist from the prompt. Only call get_user_top_tracks or get_user_top_artists if the user explicitly asks for a playlist based on their history, asks for songs similar to what they already listen to, or asks for a personalized mix rooted in their taste.
        3. Produce a final JSON response with 30-35 concrete track suggestions (artist + title).

        Audio feature guidelines (you estimate these as descriptive targets, not Spotify-queried):
        - valence: 0.0 = sad/angry, 1.0 = happy/cheerful
        - energy: 0.0 = calm/relaxed, 1.0 = intense/energetic
        - tempo: 60-80 = slow, 100-120 = moderate, 130-160 = fast
        - danceability: 0.0 = not danceable, 1.0 = very danceable
        - acousticness: 0.0 = electronic/produced, 1.0 = acoustic/unplugged

        Respond with ONLY this JSON (no markdown, no extra text):
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
            },
            "tracks": [
                { "artist": "Artist Name", "title": "Track Title" },
                { "artist": "Artist Name", "title": "Track Title" }
            ]
        }

        Rules for the tracks array:
        - Suggest real songs you are confident exist on Spotify.
        - Do NOT default to only the most famous songs in a genre. Mix obvious anthems with deeper cuts, album tracks, B-sides, and lesser-known releases from respected artists in the scene. Aim for a playlist a real fan would be impressed by — not a "top 10 most streamed" list.
        - The user's prompt drives composition. If they ask for specific artists, eras, sub-genres, or a particular vibe, honor that over any default instinct toward greatest-hits picks. If they explicitly ask for "classics", "greatest hits", or "most popular", then lean toward the obvious picks.
        - Span multiple eras and sub-styles when it fits the request. Introduce the listener to tracks they might not already know.
        - Artist name must be the primary artist only, no "feat." additions.
        - Title must be the base track title, no remix/live/acoustic qualifiers unless essential to the request.
        - Aim for 30-35 tracks so a few search misses still leave a full playlist.
        """;

    private readonly IAiClient _aiClient;
    private readonly McpDispatcher _mcpDispatcher;
    private readonly IAiConversationMessageRepository _messageRepo;
    private readonly IAiMoodMappingRepository _moodMappingRepo;
    private readonly ISpotifyApiClient _spotifyClient;
    private readonly ISpotifyTokenService _tokenService;
    private readonly ITrackFeedbackRepository _feedbackRepo;

    public AiMoodService(
        IAiClient aiClient,
        McpDispatcher mcpDispatcher,
        IAiConversationMessageRepository messageRepo,
        IAiMoodMappingRepository moodMappingRepo,
        ISpotifyApiClient spotifyClient,
        ISpotifyTokenService tokenService,
        ITrackFeedbackRepository feedbackRepo)
    {
        _aiClient        = aiClient;
        _mcpDispatcher   = mcpDispatcher;
        _messageRepo     = messageRepo;
        _moodMappingRepo = moodMappingRepo;
        _spotifyClient   = spotifyClient;
        _tokenService    = tokenService;
        _feedbackRepo    = feedbackRepo;
    }

    public async Task<AiMoodResult> GeneratePlaylistAsync(MoodSession session, User user, AiModelConfig config)
    {
        var systemPrompt = string.IsNullOrWhiteSpace(config.SystemPrompt)
            ? DefaultSystemPrompt
            : config.SystemPrompt;

        var requestedTracks = ExtractRequestedTracks(session.PromptText);
        var preSearched = new List<SpotifyTrack>();
        if (requestedTracks.Count > 0)
        {
            var accessToken = await _tokenService.EnsureValidTokenAsync(user);
            foreach (var (title, artist) in requestedTracks)
            {
                var match = await _spotifyClient.SearchTrackAsync(accessToken, artist ?? "", title);
                if (match is not null)
                    preSearched.Add(match);
            }

            if (preSearched.Count > 0)
            {
                var trackLines = new List<string>();
                foreach (var t in preSearched)
                {
                    var artistRef = t.Artists.FirstOrDefault();
                    var line = $"- \"{t.Name}\" by {artistRef?.Name ?? "Unknown"}";
                    if (artistRef is not null)
                    {
                        var fullArtist = await _spotifyClient.GetArtistAsync(accessToken, artistRef.Id);
                        if (fullArtist?.Genres.Count > 0)
                            line += $" (genres: {string.Join(", ", fullArtist.Genres)})";
                    }
                    trackLines.Add(line);
                }
                var trackInfo = string.Join("\n", trackLines);
                systemPrompt += $"""

                IMPORTANT: The user specifically requested these tracks. They have been verified on Spotify and will be force-included in the playlist:
                {trackInfo}
                Generate the rest of the playlist to complement these tracks — match their style, energy, and sub-genre closely. Use the genre tags above to guide your picks. Do NOT include these exact tracks in your tracks array, they are already added separately.
                """;
            }
        }

        var tools = McpToolDefinitions.GetCreateModeTools();
        var conversation = new List<AiMessage>();
        var sequenceOrder = 0;

        var userPrompt = BuildUserPrompt(session);
        conversation.Add(new AiMessage { Role = AiMessageRoles.User, Text = userPrompt });
        await SaveMessage(session.Id, AiMessageRoles.User, userPrompt, sequenceOrder++);

        for (var round = 0; round < MaxToolCallRounds; round++)
        {
            var response = await _aiClient.SendMessageAsync(
                systemPrompt, conversation, tools, config.Temperature, config.MaxTokens);

            if (response.HasToolCalls)
            {
                foreach (var toolCall in response.ToolCalls)
                {
                    conversation.Add(new AiMessage { Role = AiMessageRoles.Assistant, ToolCall = toolCall });
                    await SaveMessage(session.Id, AiMessageRoles.Assistant, $"[tool_call: {toolCall.Name}]", sequenceOrder++);

                    var result = await _mcpDispatcher.ExecuteToolAsync(
                        new McpExecutionContext { SessionId = session.Id, User = user, IsEditMode = false },
                        toolCall.Name, toolCall.Arguments);

                    conversation.Add(new AiMessage
                    {
                        Role = AiMessageRoles.Function,
                        ToolResult = new AiToolResult
                        {
                            ToolCallId = toolCall.Name,
                            Result     = result
                        }
                    });
                    await SaveMessage(session.Id, AiMessageRoles.Function, result, sequenceOrder++);
                }
            }
            else if (response.Text is not null)
            {
                await SaveMessage(session.Id, AiMessageRoles.Assistant, response.Text, sequenceOrder++);

                var parseCheck = Task.Run(() => JsonDocument.Parse(ExtractJson(response.Text)));
                await ((Task)parseCheck).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

                if (!parseCheck.IsCompletedSuccessfully)
                {
                    const string nudge =
                        "Your previous response was not valid JSON. Respond again with ONLY the JSON object specified in the system prompt — no markdown fences, no commentary, no trailing text. Make sure all strings use double quotes and there are no trailing commas.";
                    conversation.Add(new AiMessage { Role = AiMessageRoles.Assistant, Text = response.Text });
                    conversation.Add(new AiMessage { Role = AiMessageRoles.User, Text = nudge });
                    await SaveMessage(session.Id, AiMessageRoles.User, nudge, sequenceOrder++);
                    continue;
                }

                parseCheck.Result.Dispose();

                var aiResult = await ParseFinalResponse(session.Id, user, response.Text);

                if (preSearched.Count > 0)
                {
                    var existingIds = new HashSet<string>(aiResult.RecommendedTracks.Select(t => t.Id));
                    var unique = preSearched.Where(t => !existingIds.Contains(t.Id)).ToList();
                    aiResult.RecommendedTracks.InsertRange(0, unique);
                }

                return aiResult;
            }
        }

        throw new InvalidOperationException("AI did not produce a valid JSON response within the allowed rounds.");
    }

    public async Task<string> RefinePlaylistAsync(MoodSession editSession, Playlist playlist, User user, string userMessage, AiModelConfig config)
    {
        var feedbacks  = await _feedbackRepo.GetByUserAndPlaylistAsync(user.Id, playlist.Id);
        var liked      = feedbacks.Where(f => f.FeedbackType == FeedbackTypes.Like).Select(f => f.SpotifyTrackId).ToList();
        var skipped    = feedbacks.Where(f => f.FeedbackType == FeedbackTypes.Skip).Select(f => f.SpotifyTrackId).ToList();

        var feedbackContext = "";
        if (liked.Count > 0 || skipped.Count > 0)
        {
            var parts = new List<string>();
            if (liked.Count > 0)   parts.Add($"Liked track IDs (user enjoys these, keep or add similar): {string.Join(", ", liked)}");
            if (skipped.Count > 0) parts.Add($"Disliked track IDs (user dislikes these, avoid similar): {string.Join(", ", skipped)}");
            feedbackContext = $"\n\nUser feedback on current tracks:\n{string.Join("\n", parts)}";
        }

        var editSystemPrompt = $"""
            You are DJ Brate, an AI DJ editing a Spotify playlist based on the user's instructions.

            You have tools to inspect and modify the playlist:
            - get_current_playlist_tracks: ALWAYS call this first to see what's currently in the playlist.
            - remove_tracks_from_current_playlist: remove specific tracks by their Spotify IDs.
            - add_tracks_to_current_playlist: search for and add new tracks by artist + title.

            Rules:
            - Always inspect the playlist before making changes.
            - Honor the user's request precisely. If they say "remove all slow songs", do it. If they say "add 5 hip-hop tracks", add 5.
            - When adding tracks, suggest real songs you are confident exist on Spotify.
            - Artist name must be the primary artist only (no "feat.").
            - After making all changes, respond with a short plain-text summary of what you did (no JSON).{feedbackContext}
            """;

        var ctx = new McpExecutionContext
        {
            SessionId  = editSession.Id,
            User       = user,
            PlaylistId = playlist.Id,
            IsEditMode = true
        };

        var history = await _messageRepo.GetByPlaylistIdAsync(playlist.Id);
        var conversation = history
            .Where(m => m.Role is AiMessageRoles.User or AiMessageRoles.Assistant)
            .Select(m => new AiMessage { Role = m.Role, Text = m.Content })
            .ToList();

        conversation.Add(new AiMessage { Role = AiMessageRoles.User, Text = userMessage });

        var tools = McpToolDefinitions.GetEditModeTools();
        var sequenceOrder = history.Count;

        await SaveMessage(editSession.Id, AiMessageRoles.User, userMessage, sequenceOrder++);

        for (var round = 0; round < MaxToolCallRounds; round++)
        {
            var response = await _aiClient.SendMessageAsync(
                editSystemPrompt, conversation, tools, config.Temperature, config.MaxTokens);

            if (response.HasToolCalls)
            {
                foreach (var toolCall in response.ToolCalls)
                {
                    conversation.Add(new AiMessage { Role = AiMessageRoles.Assistant, ToolCall = toolCall });
                    await SaveMessage(editSession.Id, AiMessageRoles.Assistant, $"[tool_call: {toolCall.Name}]", sequenceOrder++);

                    var toolResult = await _mcpDispatcher.ExecuteToolAsync(ctx, toolCall.Name, toolCall.Arguments);

                    conversation.Add(new AiMessage
                    {
                        Role = AiMessageRoles.Function,
                        ToolResult = new AiToolResult { ToolCallId = toolCall.Name, Result = toolResult }
                    });
                    await SaveMessage(editSession.Id, AiMessageRoles.Function, toolResult, sequenceOrder++);
                }
            }
            else if (response.Text is not null)
            {
                await SaveMessage(editSession.Id, AiMessageRoles.Assistant, response.Text, sequenceOrder++);
                return response.Text;
            }
        }

        return "Done.";
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

    private static List<(string Title, string? Artist)> ExtractRequestedTracks(string? prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            return [];

        var results = new List<(string Title, string? Artist)>();
        foreach (Match match in RequestedTrackPattern.Matches(prompt))
        {
            var title = match.Groups[1].Value.Trim().Trim('\'', '"');
            var artist = match.Groups[2].Success
                ? match.Groups[2].Value.Trim().Trim('\'', '"')
                : null;
            if (!string.IsNullOrWhiteSpace(title))
                results.Add((title, artist));
        }
        return results;
    }

    private async Task<AiMoodResult> ParseFinalResponse(Guid sessionId, User user, string aiText)
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
            FlowUsed          = "ai_direct_tracks",
            AiReasoning       = reasoning
        });

        var tracks = await ResolveTracksAsync(user, root);

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

    private async Task<List<SpotifyTrack>> ResolveTracksAsync(User user, JsonElement root)
    {
        if (!root.TryGetProperty("tracks", out var tracksElement) || tracksElement.ValueKind != JsonValueKind.Array)
            return [];

        var accessToken = await _tokenService.EnsureValidTokenAsync(user);
        var resolved = new List<SpotifyTrack>();
        var seenIds = new HashSet<string>();

        foreach (var item in tracksElement.EnumerateArray())
        {
            var artist = item.TryGetProperty("artist", out var a) ? a.GetString() : null;
            var title  = item.TryGetProperty("title",  out var t) ? t.GetString() : null;
            if (string.IsNullOrWhiteSpace(artist) || string.IsNullOrWhiteSpace(title))
                continue;

            var match = await _spotifyClient.SearchTrackAsync(accessToken, artist, title);
            if (match is not null && seenIds.Add(match.Id))
                resolved.Add(match);
        }

        return resolved;
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
