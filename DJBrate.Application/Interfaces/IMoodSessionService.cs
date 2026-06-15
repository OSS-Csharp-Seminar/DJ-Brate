using DJBrate.Domain.Entities;

namespace DJBrate.Application.Interfaces;

// Definira stvaranje sesije, generiranje i AI uredivanje playliste.
public interface IMoodSessionService
{
    Task<MoodSession?> GetSessionByIdAsync(Guid id);
    Task<IEnumerable<MoodSession>> GetSessionsByUserIdAsync(Guid userId);
    Task<MoodSession> CreateSessionAsync(MoodSession session);

    Task<PlaylistGenerationResult> GenerateAsync(
        User user,
        string? promptText,
        string? selectedMood,
        string[]? selectedGenres,
        float? energyLevel,
        float? danceability,
        string? playlistNameOverride,
        string? playlistDescriptionOverride);

    Task<string> RefineAsync(User user, Playlist playlist, string userMessage);
}

// Prenosi spremljenu playlistu i AI objasnjenja prema Web sloju.
public class PlaylistGenerationResult
{
    public Playlist Playlist { get; set; } = null!;
    public AiMoodResult Insights { get; set; } = null!;
}
