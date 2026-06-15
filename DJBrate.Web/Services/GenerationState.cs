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
            energyLevel, danceability, playlistNameOverride, playlistDescriptionOverride); //poziva se GenerateAsync metoda iz MoodSessionService klase koja generira playlistu na temelju unesenih parametara, 
                                                                                          // a rezultat se sprema u task varijablu 

        await ((Task)task).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        IsGenerating = false;
        if (task.IsCompletedSuccessfully)
            Result = task.Result;
        else
            ErrorMessage = task.Exception?.InnerException?.Message ?? "Generation failed. Please try again.";

        Notify();
    }
    // ako je IsGenerating true prekida se novi poziv, ako je false onda se postavlja IsGenerating na true i resetira se 
    // Result i ErrorMessage na null, te se poziva Notify() da obavijesti pretplatnike o promjeni stanja
    //Notify() metoda poziva OnChange događaj kojiprikazuje korisniku "generating..." stanje ( StateHasChanged() )

    public void Reset()
    {
        if (IsGenerating) return;
        Result = null;
        ErrorMessage = null;
        Notify();
    }

    private void Notify() => OnChange?.Invoke();
}
