using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using DJBrate.Application.Interfaces;
using DJBrate.Domain.Entities;
using DJBrate.Domain.Interfaces;
using Microsoft.Extensions.Configuration;

namespace DJBrate.Application.Services;

public class SpotifyTokenService : ISpotifyTokenService
{
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;

    public SpotifyTokenService(IUserRepository userRepository, IConfiguration configuration)
    {
        _userRepository = userRepository;
        _configuration = configuration;
    }

    public async Task<string> EnsureValidTokenAsync(User user)
    {
        if (user.TokenExpiresAt.HasValue && user.TokenExpiresAt > DateTime.UtcNow.AddMinutes(5))
            return user.SpotifyAccessToken!;

        if (string.IsNullOrEmpty(user.SpotifyRefreshToken))
            throw new InvalidOperationException("No Spotify refresh token available for this user.");

        var clientId = _configuration["Spotify:ClientId"]!;
        var clientSecret = _configuration["Spotify:ClientSecret"]!;
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        var response = await httpClient.PostAsync("https://accounts.spotify.com/api/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"]    = "refresh_token",
                ["refresh_token"] = user.SpotifyRefreshToken
            }));

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        user.SpotifyAccessToken = json.GetProperty("access_token").GetString()!;
        user.TokenExpiresAt = DateTime.UtcNow.AddSeconds(json.GetProperty("expires_in").GetInt32());

        if (json.TryGetProperty("refresh_token", out var newRefresh) && newRefresh.ValueKind != JsonValueKind.Null)
            user.SpotifyRefreshToken = newRefresh.GetString();

        await _userRepository.UpdateAsync(user);
        return user.SpotifyAccessToken;
    }
}
