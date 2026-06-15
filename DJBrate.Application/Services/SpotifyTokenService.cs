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
    private readonly IUserRepository    _userRepository;
    private readonly IConfiguration     _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public SpotifyTokenService(
        IUserRepository userRepository,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        _userRepository    = userRepository;
        _configuration     = configuration;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<SpotifyTokenResponse> ExchangeCodeForTokensAsync(string code, string redirectUri)
    {
        var http = _httpClientFactory.CreateClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", GetCredentials());

        var response = await http.PostAsync(SpotifyConstants.TokenUrl,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"]   = "authorization_code",
                ["code"]         = code,
                ["redirect_uri"] = redirectUri
            }));

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<SpotifyTokenResponse>()
            ?? throw new InvalidOperationException("Failed to deserialize token response.");

    }

    public async Task<string> EnsureValidTokenAsync(User user)
    {
        if (user.TokenExpiresAt.HasValue &&
            user.TokenExpiresAt > DateTime.UtcNow.AddMinutes(SpotifyConstants.TokenRefreshBufferMinutes))
            return user.SpotifyAccessToken!;

        if (string.IsNullOrEmpty(user.SpotifyRefreshToken))
            throw new InvalidOperationException("No Spotify refresh token available for this user.");

        var http = _httpClientFactory.CreateClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", GetCredentials());

        var response = await http.PostAsync(SpotifyConstants.TokenUrl,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"]    = "refresh_token",
                ["refresh_token"] = user.SpotifyRefreshToken
            }));

        response.EnsureSuccessStatusCode();

        var token = await response.Content.ReadFromJsonAsync<SpotifyTokenResponse>()
            ?? throw new InvalidOperationException("Failed to deserialize token response.");

        user.SpotifyAccessToken = token.AccessToken;
        user.TokenExpiresAt     = DateTime.UtcNow.AddSeconds(token.ExpiresIn);

        if (token.RefreshToken is not null)
            user.SpotifyRefreshToken = token.RefreshToken;

        await _userRepository.UpdateAsync(user);
        return user.SpotifyAccessToken;
    }

    private string GetCredentials()
    {
        var clientId     = _configuration["Spotify:ClientId"]!;
        var clientSecret = _configuration["Spotify:ClientSecret"]!;
        return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
    }
}
