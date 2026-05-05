namespace DJBrate.Application.Models.Stats;

public class UserStatsDto
{
    public int TotalPlaylists { get; set; }
    public int TotalSessions { get; set; }
    public int CompletedSessions { get; set; }
    public int FailedSessions { get; set; }
    public double AvgGenerationSeconds { get; set; }
    public List<MoodCount> MoodBreakdown { get; set; } = [];
    public List<GenreCount> TopGenres { get; set; } = [];
    public int LikeCount { get; set; }
    public int SkipCount { get; set; }
    public List<DailyPlaylistCount> Last30Days { get; set; } = [];
    public List<ToolUsage> ToolUsage { get; set; } = [];
    public Dictionary<string, List<TopTrackEntry>> TopTracksByRange { get; set; } = new();
    public Dictionary<string, List<TopArtistEntry>> TopArtistsByRange { get; set; } = new();
    public List<MoodTimelineEntry> MoodTimeline { get; set; } = [];
}

public record MoodCount(string Mood, int Count);
public record GenreCount(string Genre, int Count);
public record DailyPlaylistCount(DateOnly Date, int Count);
public record ToolUsage(string ToolName, int CallCount, double SuccessRate, double AvgMs);
public record TopTrackEntry(int Rank, string TrackName, string ArtistName, string SpotifyTrackId);
public record TopArtistEntry(int Rank, string ArtistName, string SpotifyArtistId, List<string> Genres);
public record MoodTimelineEntry(DateOnly Date, string Mood);
