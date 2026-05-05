namespace DJBrate.Application.Models.Spotify;

public static class SpotifyConstants
{
    public const string TokenUrl     = "https://accounts.spotify.com/api/token";
    public const string AuthorizeUrl = "https://accounts.spotify.com/authorize";
    public const string ProfileUrl   = "https://api.spotify.com/v1/me";

    public const string Scopes =
        "user-read-private user-read-email user-top-read " +
        "playlist-modify-public playlist-modify-private " +
        "ugc-image-upload " +
        "streaming user-read-playback-state user-modify-playback-state";

    public const string DefaultUserRole  = "user";
    public const string AdminRole        = "admin";
    public const string PlaceholderEmailSuffix = "@spotify.placeholder";

    public const int TokenRefreshBufferMinutes = 5;
    public const int OAuthStateTtlMinutes      = 10;
    public const int SyncIntervalHours         = 24;
    public const int CookieExpiryDays          = 7;
}
