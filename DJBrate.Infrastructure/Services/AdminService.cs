using DJBrate.Application.Interfaces;
using DJBrate.Application.Models.Admin;
using DJBrate.Application.Models.Spotify;
using DJBrate.Domain.Entities;
using DJBrate.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DJBrate.Infrastructure.Services;

public class AdminService : IAdminService
{
    private const int RecentPlaylistsDefault = 20;
    private const int RecentFailedDefault    = 20;
    private const int RecentToolCallsDefault = 30;
    private const int MaxMoods               = 10;
    private const int MaxGenres              = 15;

    private readonly AppDbContext _db;

    public AdminService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<AdminUserRow>> GetAllUsersAsync()
    {
        var users = await _db.Users
            .Select(u => new { u.Id, u.DisplayName, u.Email, u.Role, u.SpotifyId, u.LastLoginAt })
            .ToListAsync();

        var counts = await _db.Playlists
            .GroupBy(p => p.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToListAsync();

        var countMap = counts.ToDictionary(x => x.UserId, x => x.Count);

        return users
            .Select(u => new AdminUserRow(
                u.Id,
                u.DisplayName,
                u.Email,
                u.Role,
                !string.IsNullOrEmpty(u.SpotifyId),
                u.LastLoginAt,
                countMap.GetValueOrDefault(u.Id)))
            .OrderByDescending(u => u.LastLoginAt)
            .ToList();
    }

    public async Task<List<AdminPlaylistRow>> GetRecentPlaylistsAsync(int count = RecentPlaylistsDefault)
    {
        var rows = await _db.Playlists
            .Join(_db.Users,
                p => p.UserId,
                u => u.Id,
                (p, u) => new { p.Id, p.Name, p.TrackCount, p.CreatedAt, p.IsShared, UserDisplayName = u.DisplayName })
            .OrderByDescending(x => x.CreatedAt)
            .Take(count)
            .ToListAsync();

        return rows
            .Select(r => new AdminPlaylistRow(r.Id, r.Name, r.UserDisplayName, r.TrackCount, r.CreatedAt, r.IsShared))
            .ToList();
    }

    public async Task<AdminAggregateStats> GetAggregateStatsAsync()
    {
        var totalUsers     = await _db.Users.CountAsync();
        var totalPlaylists = await _db.Playlists.CountAsync();
        var totalSessions  = await _db.MoodSessions.CountAsync();

        var feedbackRows = await _db.TrackFeedbacks
            .GroupBy(f => f.FeedbackType)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToListAsync();

        return new AdminAggregateStats(
            totalUsers,
            totalPlaylists,
            totalSessions,
            feedbackRows.FirstOrDefault(f => f.Type == FeedbackTypes.Like)?.Count ?? 0,
            feedbackRows.FirstOrDefault(f => f.Type == FeedbackTypes.Skip)?.Count ?? 0);
    }

    public async Task<AiModelConfig?> GetActiveConfigAsync()
        => await _db.AiModelConfigs.FirstOrDefaultAsync(c => c.IsActive);

    public async Task SaveConfigAsync(Guid configId, string modelName, float? temperature, int? maxTokens, string systemPrompt)
    {
        var config = await _db.AiModelConfigs.FindAsync(configId);
        if (config is null) return;
        config.ModelName    = modelName;
        config.Temperature  = temperature;
        config.MaxTokens    = maxTokens;
        config.SystemPrompt = systemPrompt;
        await _db.SaveChangesAsync();
    }

    public async Task<List<AdminFailedSessionRow>> GetFailedSessionsAsync(int count = RecentFailedDefault)
    {
        var rows = await _db.MoodSessions
            .Where(s => s.Status == MoodSessionStatuses.Failed)
            .Join(_db.Users,
                s => s.UserId,
                u => u.Id,
                (s, u) => new { s.Id, s.PromptText, s.SessionType, s.CreatedAt, s.CompletedAt, UserDisplayName = u.DisplayName })
            .OrderByDescending(x => x.CreatedAt)
            .Take(count)
            .ToListAsync();

        return rows
            .Select(r => new AdminFailedSessionRow(
                r.Id,
                r.UserDisplayName,
                r.PromptText,
                r.SessionType,
                r.CreatedAt,
                r.CompletedAt.HasValue ? (r.CompletedAt.Value - r.CreatedAt).TotalSeconds : null))
            .ToList();
    }

    public async Task<List<AdminToolCallRow>> GetRecentToolCallsAsync(int count = RecentToolCallsDefault)
    {
        var rows = await _db.McpToolCalls
            .Join(_db.MoodSessions,
                t => t.SessionId,
                s => s.Id,
                (t, s) => new { t.ToolName, t.Success, t.DurationMs, t.CalledAt, s.UserId })
            .Join(_db.Users,
                x => x.UserId,
                u => u.Id,
                (x, u) => new { x.ToolName, x.Success, x.DurationMs, x.CalledAt, UserDisplayName = u.DisplayName })
            .OrderByDescending(x => x.CalledAt)
            .Take(count)
            .ToListAsync();

        return rows
            .Select(r => new AdminToolCallRow(r.ToolName, r.UserDisplayName, r.Success, r.DurationMs, r.CalledAt))
            .ToList();
    }

    public async Task SetUserRoleAsync(Guid userId, string role)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null) return;
        user.Role = role;
        await _db.SaveChangesAsync();
    }

    public async Task DeleteUserAsync(Guid userId)
    {
        var sessionIds = await _db.MoodSessions
            .Where(s => s.UserId == userId)
            .Select(s => s.Id)
            .ToListAsync();

        var playlistIds = await _db.Playlists
            .Where(p => p.UserId == userId)
            .Select(p => p.Id)
            .ToListAsync();

        await _db.TrackFeedbacks
            .Where(f => f.UserId == userId)
            .ExecuteDeleteAsync();

        if (sessionIds.Count > 0)
        {
            await _db.McpToolCalls
                .Where(t => sessionIds.Contains(t.SessionId))
                .ExecuteDeleteAsync();
            await _db.AiConversationMessages
                .Where(m => sessionIds.Contains(m.SessionId))
                .ExecuteDeleteAsync();
            await _db.AiMoodMappings
                .Where(m => sessionIds.Contains(m.SessionId))
                .ExecuteDeleteAsync();
        }

        if (playlistIds.Count > 0)
        {
            await _db.MoodSessions
                .Where(s => s.RefinesPlaylistId.HasValue && playlistIds.Contains(s.RefinesPlaylistId.Value))
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.RefinesPlaylistId, (Guid?)null));

            await _db.PlaylistTracks
                .Where(t => playlistIds.Contains(t.PlaylistId))
                .ExecuteDeleteAsync();

            await _db.Playlists
                .Where(p => p.UserId == userId)
                .ExecuteDeleteAsync();
        }

        await _db.MoodSessions.Where(s => s.UserId == userId).ExecuteDeleteAsync();
        await _db.UserTopTracks.Where(t => t.UserId == userId).ExecuteDeleteAsync();
        await _db.UserTopArtists.Where(a => a.UserId == userId).ExecuteDeleteAsync();
        await _db.ListeningStats.Where(s => s.UserId == userId).ExecuteDeleteAsync();
        await _db.Users.Where(u => u.Id == userId).ExecuteDeleteAsync();
    }

    public async Task<List<AdminMoodCount>> GetGlobalMoodBreakdownAsync()
    {
        var rows = await _db.AiMoodMappings
            .Where(m => m.DetectedMood != null)
            .GroupBy(m => m.DetectedMood!)
            .Select(g => new { Mood = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(MaxMoods)
            .ToListAsync();

        return rows.Select(r => new AdminMoodCount(r.Mood, r.Count)).ToList();
    }

    public async Task<List<AdminGenreCount>> GetGlobalGenreBreakdownAsync()
    {
        var allGenres = await _db.MoodSessions
            .Where(s => s.SelectedGenres != null)
            .Select(s => s.SelectedGenres!)
            .ToListAsync();

        return allGenres
            .SelectMany(g => g)
            .GroupBy(g => g)
            .Select(g => new AdminGenreCount(g.Key, g.Count()))
            .OrderByDescending(x => x.Count)
            .Take(MaxGenres)
            .ToList();
    }
}
