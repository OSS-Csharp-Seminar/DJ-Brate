using DJBrate.Application.Interfaces;
using DJBrate.Application.Models.Spotify;
using DJBrate.Application.Models.Stats;
using DJBrate.Domain.Entities;
using DJBrate.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DJBrate.Infrastructure.Services;

public class ListeningStatsService : IListeningStatsService
{
    private const int RecentDayWindow   = 30;
    private const int MoodTimelineWindow = 90;
    private const int MaxMoods          = 8;
    private const int MaxGenres         = 10;
    private const int TopTracksPerRange  = 10;
    private const int TopArtistsPerRange = 10;

    private static readonly SpotifyTimeRange[] AllTimeRanges =
        [SpotifyTimeRange.ShortTerm, SpotifyTimeRange.MediumTerm, SpotifyTimeRange.LongTerm];

    private readonly AppDbContext _db;

    public ListeningStatsService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<UserStatsDto> GetUserStatsAsync(Guid userId)
    {
        var since          = DateTime.UtcNow.AddDays(-RecentDayWindow);
        var timelineSince  = DateTime.UtcNow.AddDays(-MoodTimelineWindow);

        var sessions = await _db.MoodSessions
            .Where(s => s.UserId == userId)
            .Select(s => new { s.Status, s.CreatedAt, s.CompletedAt })
            .ToListAsync();

        var totalPlaylists = await _db.Playlists.CountAsync(p => p.UserId == userId);

        var moodRows = await _db.AiMoodMappings
            .Where(m => m.DetectedMood != null
                && _db.MoodSessions.Any(s => s.Id == m.SessionId && s.UserId == userId))
            .GroupBy(m => m.DetectedMood!)
            .Select(g => new { Mood = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(MaxMoods)
            .ToListAsync();

        var moods = moodRows.Select(r => new MoodCount(r.Mood, r.Count)).ToList();

        var selectedGenres = await _db.MoodSessions
            .Where(s => s.UserId == userId && s.SelectedGenres != null)
            .Select(s => s.SelectedGenres!)
            .ToListAsync();

        var genres = selectedGenres
            .SelectMany(g => g)
            .GroupBy(g => g)
            .Select(g => new GenreCount(g.Key, g.Count()))
            .OrderByDescending(x => x.Count)
            .Take(MaxGenres)
            .ToList();

        var feedback = await _db.TrackFeedbacks
            .Where(f => f.UserId == userId)
            .GroupBy(f => f.FeedbackType)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToListAsync();

        var dailyRows = await _db.Playlists
            .Where(p => p.UserId == userId && p.CreatedAt >= since)
            .Select(p => p.CreatedAt)
            .ToListAsync();

        var daily = dailyRows
            .GroupBy(d => DateOnly.FromDateTime(d))
            .Select(g => new DailyPlaylistCount(g.Key, g.Count()))
            .OrderBy(x => x.Date)
            .ToList();

        var toolRows = await _db.McpToolCalls
            .Where(t => _db.MoodSessions.Any(s => s.Id == t.SessionId && s.UserId == userId))
            .GroupBy(t => t.ToolName)
            .Select(g => new
            {
                ToolName    = g.Key,
                CallCount   = g.Count(),
                SuccessHits = g.Count(t => t.Success),
                AvgMs       = g.Average(t => (double)(t.DurationMs ?? 0))
            })
            .ToListAsync();

        var toolUsage = toolRows
            .Select(r => new ToolUsage(r.ToolName, r.CallCount, (double)r.SuccessHits / r.CallCount, r.AvgMs))
            .ToList();

        var topTracks = new Dictionary<string, List<TopTrackEntry>>();
        foreach (var range in AllTimeRanges)
        {
            var key = range.ToApiString();
            var trackRows = await _db.UserTopTracks
                .Where(t => t.UserId == userId && t.TimeRange == key)
                .OrderBy(t => t.RankPosition)
                .Take(TopTracksPerRange)
                .Select(t => new { t.RankPosition, t.TrackName, t.ArtistName, t.SpotifyTrackId })
                .ToListAsync();
            topTracks[key] = trackRows
                .Select(r => new TopTrackEntry(r.RankPosition, r.TrackName, r.ArtistName, r.SpotifyTrackId))
                .ToList();
        }

        var topArtists = new Dictionary<string, List<TopArtistEntry>>();
        foreach (var range in AllTimeRanges)
        {
            var key = range.ToApiString();
            var artistRows = await _db.UserTopArtists
                .Where(a => a.UserId == userId && a.TimeRange == key)
                .OrderBy(a => a.RankPosition)
                .Take(TopArtistsPerRange)
                .Select(a => new { a.RankPosition, a.ArtistName, a.SpotifyArtistId, a.Genres })
                .ToListAsync();
            topArtists[key] = artistRows
                .Select(r => new TopArtistEntry(r.RankPosition, r.ArtistName, r.SpotifyArtistId, r.Genres?.ToList() ?? []))
                .ToList();
        }

        var timelineRows = await _db.AiMoodMappings
            .Where(m => m.DetectedMood != null
                && _db.MoodSessions.Any(s => s.Id == m.SessionId && s.UserId == userId && s.CreatedAt >= timelineSince))
            .Join(_db.MoodSessions,
                m => m.SessionId,
                s => s.Id,
                (m, s) => new { m.DetectedMood, s.CreatedAt })
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();

        var moodTimeline = timelineRows
            .Select(r => new MoodTimelineEntry(DateOnly.FromDateTime(r.CreatedAt), r.DetectedMood!))
            .ToList();

        var completedSessions = sessions.Count(s => s.Status == MoodSessionStatuses.Completed);
        var failedSessions    = sessions.Count(s => s.Status == MoodSessionStatuses.Failed);

        var avgSec = sessions
            .Where(s => s.CompletedAt.HasValue && s.Status == MoodSessionStatuses.Completed)
            .Select(s => (s.CompletedAt!.Value - s.CreatedAt).TotalSeconds)
            .DefaultIfEmpty(0)
            .Average();

        return new UserStatsDto
        {
            TotalPlaylists       = totalPlaylists,
            TotalSessions        = sessions.Count,
            CompletedSessions    = completedSessions,
            FailedSessions       = failedSessions,
            AvgGenerationSeconds = avgSec,
            MoodBreakdown        = moods,
            TopGenres            = genres,
            LikeCount            = feedback.FirstOrDefault(f => f.Type == FeedbackTypes.Like)?.Count ?? 0,
            SkipCount            = feedback.FirstOrDefault(f => f.Type == FeedbackTypes.Skip)?.Count ?? 0,
            Last30Days           = daily,
            ToolUsage            = toolUsage,
            TopTracksByRange     = topTracks,
            TopArtistsByRange    = topArtists,
            MoodTimeline         = moodTimeline,
        };
    }
}
