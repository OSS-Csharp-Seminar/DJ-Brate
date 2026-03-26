using DJBrate.Application.Interfaces;
using DJBrate.Domain.Entities;
using DJBrate.Domain.Interfaces;

namespace DJBrate.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<User?> GetUserByIdAsync(Guid id)
        => await _userRepository.GetByIdAsync(id);

    public async Task<User?> GetUserBySpotifyIdAsync(string spotifyId)
        => await _userRepository.GetBySpotifyIdAsync(spotifyId);

    public async Task<User> CreateOrUpdateUserAsync(User user)
    {
        var existing = await _userRepository.GetBySpotifyIdAsync(user.SpotifyId!);
        if (existing is null)
        {
            await _userRepository.AddAsync(user);
            return user;
        }
        existing.DisplayName = user.DisplayName;
        existing.Email = user.Email;
        existing.AvatarUrl = user.AvatarUrl;
        existing.SpotifyAccessToken = user.SpotifyAccessToken;
        existing.SpotifyRefreshToken = user.SpotifyRefreshToken;
        existing.TokenExpiresAt = user.TokenExpiresAt;
        existing.LastLoginAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(existing);
        return existing;
    }

    public async Task<(bool Success, string? Error)> RegisterAsync(string displayName, string email, string password)
    {
        var existing = await _userRepository.GetByEmailAsync(email);
        if (existing is not null)
            return (false, "Email is already in use.");

        var user = new User
        {
            DisplayName = displayName,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = "user",
            CreatedAt = DateTime.UtcNow
        };
        await _userRepository.AddAsync(user);
        return (true, null);
    }

    public async Task<User?> AuthenticateAsync(string email, string password)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user is null || user.PasswordHash is null)
            return null;
        return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash) ? user : null;
    }
}
