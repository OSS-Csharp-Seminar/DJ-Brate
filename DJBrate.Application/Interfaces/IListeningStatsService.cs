using DJBrate.Application.Models.Stats;

namespace DJBrate.Application.Interfaces;

// Definira izracun korisnickih statistika za Statistics stranicu.
public interface IListeningStatsService
{
    Task<UserStatsDto> GetUserStatsAsync(Guid userId);
}
