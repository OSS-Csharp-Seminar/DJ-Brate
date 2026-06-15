using DJBrate.Application.Interfaces;
using DJBrate.Domain.Entities;

namespace DJBrate.Web.Services;

public class GenerationState
{
    private readonly IMoodSessionService _service;

    public GenerationState(IMoodSessionService service)
    {
        _service = service;
    }

    public event Action? OnChange;

    public bool IsGenerating { get; private set; }
    public PlaylistGenerationResult? Result { get; private set; }
    public string? ErrorMessage { get; private set; }

    public async Task GenerateAsync(
        User user,
        string? promptText,
        string? selectedMood,
        string[]? selectedGenres,
        float? energyLevel,
        float? danceability,
        string? playlistNameOverride,
        string? playlistDescriptionOverride)
    {
        if (IsGenerating) return;

        IsGenerating = true;
        Result = null;
        ErrorMessage = null;
        Notify();

        var task = _service.GenerateAsync(
            user, promptText, selectedMood, selectedGenres,
            energyLevel, danceability, playlistNameOverride, playlistDescriptionOverride);

        await ((Task)task).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        IsGenerating = false;
        if (task.IsCompletedSuccessfully)
            Result = task.Result;
        else
            ErrorMessage = task.Exception?.InnerException?.Message ?? "Generation failed. Please try again.";

        Notify();
    }

    public void Reset()
    {
        if (IsGenerating) return;
        Result = null;
        ErrorMessage = null;
        Notify();
    }

    private void Notify() => OnChange?.Invoke();
}
