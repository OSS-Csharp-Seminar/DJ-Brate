using DJBrate.Domain.Entities;

namespace DJBrate.Application.Interfaces;

public interface ISpotifyTokenService
{
    Task<string> EnsureValidTokenAsync(User user);
}
