namespace DJBrate.Application.Models.Admin;

public record AdminUserRow(Guid Id, string DisplayName, string Email, string Role, bool HasSpotify, DateTime? LastLoginAt, int PlaylistCount);
public record AdminPlaylistRow(Guid Id, string Name, string UserDisplayName, int TrackCount, DateTime CreatedAt, bool IsShared);
public record AdminAggregateStats(int TotalUsers, int TotalPlaylists, int TotalSessions, int LikeCount, int SkipCount);
public record AdminFailedSessionRow(Guid Id, string UserDisplayName, string? PromptText, string SessionType, DateTime CreatedAt, double? DurationSeconds);
public record AdminToolCallRow(string ToolName, string UserDisplayName, bool Success, int? DurationMs, DateTime CalledAt);
public record AdminMoodCount(string Mood, int Count);
public record AdminGenreCount(string Genre, int Count);
