using DJBrate.Application.Models.Stats;

namespace DJBrate.Application.Interfaces;

public interface IListeningStatsService
{
    Task<UserStatsDto> GetUserStatsAsync(Guid userId);
}
