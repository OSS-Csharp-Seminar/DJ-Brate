using DJBrate.Application.Interfaces;
using DJBrate.Domain.Entities;

namespace DJBrate.Web.Services;

public class RefinementState
{
    private readonly IMoodSessionService _service;

    public RefinementState(IMoodSessionService service)
    {
        _service = service;
    }

    public event Action? OnChange;

    public bool IsRefining { get; private set; }
    public string? LastReply { get; private set; }
    public string? ErrorMessage { get; private set; }

    public async Task RefineAsync(User user, Playlist playlist, string userMessage)
    {
        if (IsRefining) return;

        IsRefining = true;
        LastReply = null;
        ErrorMessage = null;
        Notify();

        var task = _service.RefineAsync(user, playlist, userMessage);
        await ((Task)task).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        IsRefining = false;
        if (task.IsCompletedSuccessfully)
            LastReply = task.Result;
        else
            ErrorMessage = task.Exception?.InnerException?.Message ?? "Refinement failed. Please try again.";

        Notify();
    }
    // sprjecava 2 istovremena editanja, postavlja IsRefining na true, resetira LastReply i ErrorMessage, poziva Notify da se obavijeste pretplatnici,
    // poziva _service.RefineAsync i ceka da se zavrsi, nakon toga postavlja IsRefining na false, ako je uspjesno postavlja LastReply na rezultat, ako nije postavlja ErrorMessage na poruku greske, i onda poziva Notify da se obavijeste pretplatnici

    private void Notify() => OnChange?.Invoke();
}
