using System.Text.Json;
using DJBrate.Application.Models.Ai;

namespace DJBrate.Application.Mcp;

public static class McpToolDefinitions
{
    public static List<AiToolDefinition> GetAllTools() =>
    [
        new AiToolDefinition
        {
            Name = ToolNames.GetUserTopTracks,
            Description = "Get the user's top tracks from Spotify. Returns track names, artists, and Spotify IDs. Use this to understand the user's music taste before making recommendations.",
            Parameters = JsonDocument.Parse("""
            {
                "type": "object",
                "properties": {
                    "time_range": {
                        "type": "string",
                        "enum": ["short_term", "medium_term", "long_term"],
                        "description": "short_term = last 4 weeks, medium_term = last 6 months, long_term = all time"
                    }
                },
                "required": ["time_range"]
            }
            """)
        },
        new AiToolDefinition
        {
            Name = ToolNames.GetUserTopArtists,
            Description = "Get the user's top artists from Spotify. Returns artist names, genres, and Spotify IDs. Use this to understand the user's preferred genres and artists.",
            Parameters = JsonDocument.Parse("""
            {
                "type": "object",
                "properties": {
                    "time_range": {
                        "type": "string",
                        "enum": ["short_term", "medium_term", "long_term"],
                        "description": "short_term = last 4 weeks, medium_term = last 6 months, long_term = all time"
                    }
                },
                "required": ["time_range"]
            }
            """)
        },
        new AiToolDefinition
        {
            Name = ToolNames.GetRecommendations,
            Description = "Get track recommendations from Spotify based on seed artists, seed tracks, and audio feature targets. Use this to generate playlist tracks that match the user's mood.",
            Parameters = JsonDocument.Parse("""
            {
                "type": "object",
                "properties": {
                    "seed_artist_ids": {
                        "type": "array",
                        "items": { "type": "string" },
                        "description": "Spotify artist IDs to seed recommendations (max 3)"
                    },
                    "seed_track_ids": {
                        "type": "array",
                        "items": { "type": "string" },
                        "description": "Spotify track IDs to seed recommendations (max 2)"
                    },
                    "target_valence": {
                        "type": "number",
                        "description": "Target happiness/positivity from 0.0 (sad) to 1.0 (happy)"
                    },
                    "target_energy": {
                        "type": "number",
                        "description": "Target energy level from 0.0 (calm) to 1.0 (intense)"
                    },
                    "target_tempo": {
                        "type": "number",
                        "description": "Target tempo in BPM (e.g. 80 for slow, 120 for moderate, 160 for fast)"
                    },
                    "target_danceability": {
                        "type": "number",
                        "description": "Target danceability from 0.0 (least danceable) to 1.0 (most danceable)"
                    },
                    "target_acousticness": {
                        "type": "number",
                        "description": "Target acousticness from 0.0 (electronic) to 1.0 (acoustic)"
                    }
                },
                "required": ["seed_artist_ids", "seed_track_ids"]
            }
            """)
        }
    ];

    public static class ToolNames
    {
        public const string GetUserTopTracks   = "get_user_top_tracks";
        public const string GetUserTopArtists  = "get_user_top_artists";
        public const string GetRecommendations = "get_recommendations";
    }
}
