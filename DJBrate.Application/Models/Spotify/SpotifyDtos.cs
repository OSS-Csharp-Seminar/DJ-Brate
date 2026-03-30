using System.Text.Json.Serialization;

namespace DJBrate.Application.Models.Spotify;

public class SpotifyTrack
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("uri")]
    public string Uri { get; set; } = null!;

    [JsonPropertyName("duration_ms")]
    public int DurationMs { get; set; }

    [JsonPropertyName("preview_url")]
    public string? PreviewUrl { get; set; }

    [JsonPropertyName("artists")]
    public List<SpotifyArtistRef> Artists { get; set; } = [];

    [JsonPropertyName("album")]
    public SpotifyAlbum Album { get; set; } = null!;
}

public class SpotifyArtistRef
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;
}

public class SpotifyAlbum
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;
}

public class SpotifyArtist
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("genres")]
    public List<string> Genres { get; set; } = [];
}

public class AudioFeatureTargets
{
    public float? Valence { get; set; }
    public float? Energy { get; set; }
    public float? Tempo { get; set; }
    public float? Danceability { get; set; }
    public float? Acousticness { get; set; }
}
