using DJBrate.Application.Models.Ai;
using DJBrate.Application.Models.Spotify;
using DJBrate.Domain.Entities;

namespace DJBrate.Application.Interfaces;

// Definira AI generiranje i uredivanje Spotify playlista.
public interface IAiMoodService
{
    Task<AiMoodResult> GeneratePlaylistAsync(MoodSession session, User user, AiModelConfig config);
    Task<string> RefinePlaylistAsync(MoodSession editSession, Playlist playlist, User user, string userMessage, AiModelConfig config);
}

// Sadrzi AI naziv, mood, audio parametre i pronadene Spotify pjesme.
public class AiMoodResult
{
    public string PlaylistName { get; set; } = null!;
    public string PlaylistDescription { get; set; } = null!;
    public string DetectedMood { get; set; } = null!;
    public string? AiReasoning { get; set; }
    public AudioFeatureTargets AudioFeatures { get; set; } = new();
    public List<SpotifyTrack> RecommendedTracks { get; set; } = [];
}
