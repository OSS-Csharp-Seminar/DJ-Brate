using DJBrate.Domain.Entities;

namespace DJBrate.Domain.Interfaces;

// Definira posebne upite za korisnike.
public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetBySpotifyIdAsync(string spotifyId);
}
