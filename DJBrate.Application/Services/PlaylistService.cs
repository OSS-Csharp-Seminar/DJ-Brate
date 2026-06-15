using System.Security.Cryptography;
using DJBrate.Application.Interfaces;
using DJBrate.Domain.Entities;
using DJBrate.Domain.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace DJBrate.Application.Services;

public class PlaylistService : IPlaylistService
{
    private const int CoverMaxDimension = 600;
    private const int CoverJpegQuality = 85;
    private const int SpotifyCoverMaxBase64Bytes = 256 * 1024;
    private const int ShareTokenByteLength = 6;

    private readonly IPlaylistRepository _playlistRepository;
    private readonly IUserRepository _userRepository;
    private readonly ISpotifyTokenService _tokenService;
    private readonly ISpotifyApiClient _spotifyClient;
    private readonly IAiConversationMessageRepository _conversationRepository;
    private readonly IMoodSessionRepository _moodSessionRepository;

    public PlaylistService(
        IPlaylistRepository playlistRepository,
        IUserRepository userRepository,
        ISpotifyTokenService tokenService,
        ISpotifyApiClient spotifyClient,
        IAiConversationMessageRepository conversationRepository,
        IMoodSessionRepository moodSessionRepository)
    {
        _playlistRepository     = playlistRepository;
        _userRepository         = userRepository;
        _tokenService           = tokenService;
        _spotifyClient          = spotifyClient;
        _conversationRepository = conversationRepository;
        _moodSessionRepository  = moodSessionRepository;
    }

    public async Task<Playlist?> GetPlaylistByIdAsync(Guid id)
        => await _playlistRepository.GetByIdWithTracksAsync(id);

    public async Task<IEnumerable<Playlist>> GetPlaylistsByUserIdAsync(Guid userId)
        => await _playlistRepository.GetByUserIdAsync(userId);

    public async Task<Playlist> CreatePlaylistAsync(Playlist playlist)
    {
        await _playlistRepository.AddAsync(playlist);
        return playlist;
    }

    public async Task<bool> UpdateCoverImageAsync(Guid playlistId, Guid userId, string imageUrl)
    {
        var playlist = await _playlistRepository.GetByIdAsync(playlistId);
        if (playlist is null || playlist.UserId != userId) return false;
        playlist.ImageUrl = imageUrl;
        await _playlistRepository.UpdateAsync(playlist);
        return true;
    }

    public async Task SyncCoverToSpotifyAsync(Guid playlistId, Guid userId, byte[] imageBytes)
    {
        var playlist = await _playlistRepository.GetByIdAsync(playlistId);
        if (playlist is null || playlist.UserId != userId) return;
        if (string.IsNullOrEmpty(playlist.SpotifyPlaylistId)) return;

        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null) return;

        var jpegBase64 = await Task.Run(() => ReencodeToJpegBase64(imageBytes));
        if (jpegBase64 is null) return;

        var token = await _tokenService.EnsureValidTokenAsync(user);
        await _spotifyClient.UploadPlaylistCoverAsync(token, playlist.SpotifyPlaylistId, jpegBase64);
    }

    public async Task<User?> GetUserAsync(Guid userId)
        => await _userRepository.GetByIdAsync(userId);

    public async Task<List<AiConversationMessage>> GetConversationAsync(Guid playlistId, Guid userId)
    {
        var playlist = await _playlistRepository.GetByIdAsync(playlistId);
        if (playlist is null || playlist.UserId != userId) return [];
        return await _conversationRepository.GetByPlaylistIdAsync(playlistId);
    }

    public async Task<string?> EnableSharingAsync(Guid playlistId, Guid userId)
    {
        var playlist = await _playlistRepository.GetByIdAsync(playlistId);
        if (playlist is null || playlist.UserId != userId) return null;

        if (string.IsNullOrEmpty(playlist.ShareToken))
            playlist.ShareToken = GenerateShareToken();
        playlist.IsShared = true;

        await _playlistRepository.UpdateAsync(playlist);
        return playlist.ShareToken;
    }

    public async Task<bool> DisableSharingAsync(Guid playlistId, Guid userId)
    {
        var playlist = await _playlistRepository.GetByIdAsync(playlistId);
        if (playlist is null || playlist.UserId != userId) return false;

        playlist.IsShared = false;
        await _playlistRepository.UpdateAsync(playlist);
        return true;
    }

    public async Task<Playlist?> GetByShareTokenAsync(string token)
        => await _playlistRepository.GetByShareTokenAsync(token);

    public async Task<List<AiConversationMessage>> GetSharedConversationAsync(string token)
    {
        var playlist = await _playlistRepository.GetByShareTokenAsync(token);
        if (playlist is null) return [];
        return await _conversationRepository.GetByPlaylistIdAsync(playlist.Id);
    }

    public async Task<MoodSession?> GetOriginalSessionAsync(Guid playlistId, Guid userId)
    {
        var playlist = await _playlistRepository.GetByIdAsync(playlistId);
        if (playlist is null || playlist.UserId != userId) return null;
        return await _moodSessionRepository.GetByIdAsync(playlist.SessionId);
    }

    private static string GenerateShareToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(ShareTokenByteLength))
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');

    private static string? ReencodeToJpegBase64(byte[] bytes)
    {
        using var image = Image.Load(bytes);
        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Mode = ResizeMode.Max,
            Size = new Size(CoverMaxDimension, CoverMaxDimension)
        }));

        using var ms = new MemoryStream();
        image.SaveAsJpeg(ms, new JpegEncoder { Quality = CoverJpegQuality });
        var base64 = Convert.ToBase64String(ms.ToArray());
        return base64.Length > SpotifyCoverMaxBase64Bytes ? null : base64;
    }
}
