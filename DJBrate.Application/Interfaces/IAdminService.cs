using DJBrate.Application.Models.Admin;
using DJBrate.Domain.Entities;

namespace DJBrate.Application.Interfaces;

public interface IAdminService
{
    Task<List<AdminUserRow>> GetAllUsersAsync();
    Task<List<AdminPlaylistRow>> GetRecentPlaylistsAsync(int count = 20);
    Task<AdminAggregateStats> GetAggregateStatsAsync();
    Task<AiModelConfig?> GetActiveConfigAsync();
    Task SaveConfigAsync(Guid configId, string modelName, float? temperature, int? maxTokens, string systemPrompt);
    Task<List<AdminFailedSessionRow>> GetFailedSessionsAsync(int count = 20);
    Task<List<AdminToolCallRow>> GetRecentToolCallsAsync(int count = 30);
    Task SetUserRoleAsync(Guid userId, string role);
    Task DeleteUserAsync(Guid userId);
    Task<List<AdminMoodCount>> GetGlobalMoodBreakdownAsync();
    Task<List<AdminGenreCount>> GetGlobalGenreBreakdownAsync();
}
