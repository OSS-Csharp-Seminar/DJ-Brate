using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using DJBrate.Application.Interfaces;
using DJBrate.Application.Models.Spotify;
using DJBrate.Domain.Entities;
using DJBrate.Domain.Interfaces;
using Microsoft.Extensions.Configuration;

namespace DJBrate.Application.Services;

public class SpotifyTokenService : ISpotifyTokenService
{
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration  _configuration;

    public SpotifyTokenService(IUserRepository userRepository, IConfiguration configuration)
    {
        _userRepository = userRepository;
        _configuration  = configuration;
    }

    public async Task<string> EnsureValidTokenAsync(User user)
    {
        if (user.TokenExpiresAt.HasValue &&
            user.TokenExpiresAt > DateTime.UtcNow.AddMinutes(SpotifyConstants.TokenRefreshBufferMinutes))
            return user.SpotifyAccessToken!;

        if (string.IsNullOrEmpty(user.SpotifyRefreshToken))
            throw new InvalidOperationException("No Spotify refresh token available for this user.");

        var clientId     = _configuration["Spotify:ClientId"]!;
        var clientSecret = _configuration["Spotify:ClientSecret"]!;
        var credentials  = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

        using var http = new HttpClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        var response = await http.PostAsync(SpotifyConstants.TokenUrl,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"]    = "refresh_token",
                ["refresh_token"] = user.SpotifyRefreshToken
            }));

        response.EnsureSuccessStatusCode();

        var token = await response.Content.ReadFromJsonAsync<SpotifyTokenResponse>();
        user.SpotifyAccessToken = token!.AccessToken;
        user.TokenExpiresAt     = DateTime.UtcNow.AddSeconds(token.ExpiresIn);

        if (token.RefreshToken is not null)
            user.SpotifyRefreshToken = token.RefreshToken;

        await _userRepository.UpdateAsync(user);
        return user.SpotifyAccessToken;
    }
}
