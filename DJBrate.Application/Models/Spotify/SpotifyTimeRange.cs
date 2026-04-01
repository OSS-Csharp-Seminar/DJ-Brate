namespace DJBrate.Application.Models.Spotify;

public enum SpotifyTimeRange
{
    ShortTerm,
    MediumTerm,
    LongTerm
}

public static class SpotifyTimeRangeExtensions
{
    public static string ToApiString(this SpotifyTimeRange range) => range switch
    {
        SpotifyTimeRange.ShortTerm => "short_term",
        SpotifyTimeRange.LongTerm  => "long_term",
        _                          => "medium_term"
    };
}
