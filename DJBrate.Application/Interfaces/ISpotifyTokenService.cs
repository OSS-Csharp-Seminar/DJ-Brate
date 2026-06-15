using DJBrate.Application.Models.Spotify;
using DJBrate.Domain.Entities;

namespace DJBrate.Application.Interfaces;

// Definira zamjenu OAuth codea i obnavljanje Spotify tokena.
public interface ISpotifyTokenService
{
    Task<SpotifyTokenResponse> ExchangeCodeForTokensAsync(string code, string redirectUri); //stvarna implementacija SpotifyTokenService
    Task<string> EnsureValidTokenAsync(User user);
}
