using DJBrate.Domain.Entities;

namespace DJBrate.Application.Interfaces;

public interface IUserService
{
    Task<User?> GetUserByIdAsync(Guid id);
    Task<User?> GetUserBySpotifyIdAsync(string spotifyId);
    Task<User> CreateOrUpdateUserAsync(User user);
}
