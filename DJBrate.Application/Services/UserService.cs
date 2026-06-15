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

    // Dohvaca korisnika prema lokalnom ID-u.
    public async Task<User?> GetUserByIdAsync(Guid id)
        => await _userRepository.GetByIdAsync(id);

    public async Task<User?> GetUserBySpotifyIdAsync(string spotifyId) //prosljedjuje se SpotifyId u IUserRepository
        => await _userRepository.GetBySpotifyIdAsync(spotifyId);

    public async Task<User> CreateOrUpdateUserAsync(User user)
    {
        var existing = await _userRepository.GetBySpotifyIdAsync(user.SpotifyId!);
        if (existing is null)
        {
            user.LastLoginAt = DateTime.UtcNow;
            await _userRepository.AddAsync(user);
            return user;
        }
        existing.DisplayName        = user.DisplayName;
        existing.Email              = user.Email;
        existing.AvatarUrl          = user.AvatarUrl;
        existing.SpotifyAccessToken  = user.SpotifyAccessToken;
        existing.SpotifyRefreshToken = user.SpotifyRefreshToken;
        existing.TokenExpiresAt     = user.TokenExpiresAt;
        existing.LastLoginAt        = DateTime.UtcNow;
        await _userRepository.UpdateAsync(existing);
        return existing;
    } // ako korisnik ne postoji, postavlja se LastLoginAt na trenutno vrijeme i poziva se AddAsync da se doda u bazu. 
    // Ako korisnik postoji, azuriraju se njegovi podaci i tokeni, te se poziva UpdateAsync da se spremi u bazu. U oba slucaja vraca se User objekt.
}
