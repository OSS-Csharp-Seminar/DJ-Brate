namespace DJBrate.Application.Models.Spotify;

// Predstavlja Spotify short, medium i long term periode.
public enum SpotifyTimeRange
{
    ShortTerm,
    MediumTerm,
    LongTerm
}

// Pretvara enum period u vrijednost koju Spotify API ocekuje.
public static class SpotifyTimeRangeExtensions
{
    public static string ToApiString(this SpotifyTimeRange range) => range switch
    {
        SpotifyTimeRange.ShortTerm => "short_term",
        SpotifyTimeRange.LongTerm  => "long_term",
        _                          => "medium_term"
    };
}
