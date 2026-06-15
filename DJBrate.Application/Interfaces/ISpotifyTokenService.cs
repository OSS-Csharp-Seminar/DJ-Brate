using DJBrate.Application.Models.Spotify;
using DJBrate.Domain.Entities;

namespace DJBrate.Application.Interfaces;

public interface ISpotifyTokenService
{
    Task<SpotifyTokenResponse> ExchangeCodeForTokensAsync(string code, string redirectUri);
    Task<string> EnsureValidTokenAsync(User user);
}
