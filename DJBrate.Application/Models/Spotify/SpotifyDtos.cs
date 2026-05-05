using System.Text.Json.Serialization;

namespace DJBrate.Application.Models.Spotify;

public class SpotifyTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = null!;

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
}

public class SpotifyProfileResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("images")]
    public List<SpotifyImage> Images { get; set; } = [];
}

public class SpotifyImage
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = null!;
}

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

    [JsonIgnore]
    public string? AiNote { get; set; }
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

    [JsonPropertyName("images")]
    public List<SpotifyImage> Images { get; set; } = [];
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
