using DJBrate.Domain.Entities;

namespace DJBrate.Application.Interfaces;

public interface IUserService
{
    Task<User?> GetUserByIdAsync(Guid id);
    Task<User?> GetUserBySpotifyIdAsync(string spotifyId);
    Task<User> CreateOrUpdateUserAsync(User user);
    Task<(bool Success, string? Error)> RegisterAsync(string displayName, string email, string password);
    Task<User?> AuthenticateAsync(string email, string password);
}
