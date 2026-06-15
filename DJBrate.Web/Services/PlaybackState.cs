using DJBrate.Domain.Entities;

namespace DJBrate.Web.Services;

public class PlaybackState
{
    public event Action? OnChange;

    public IReadOnlyList<PlaylistTrack> Tracks { get; private set; } = Array.Empty<PlaylistTrack>();
    public string? PlayingTrackId { get; private set; }
    public Guid? SourcePlaylistId { get; private set; }
    public bool IsPaused { get; private set; } = true;
    public double PositionMs { get; private set; }
    public double DurationMs { get; private set; }

    public PlaylistTrack? CurrentTrack =>
        PlayingTrackId is null ? null : Tracks.FirstOrDefault(t => t.SpotifyTrackId == PlayingTrackId);

    public int PlayingIndex =>
        PlayingTrackId is null ? -1 : OrderedTracks().FindIndex(t => t.SpotifyTrackId == PlayingTrackId);

    public bool CanPlayPrevious => PlayingIndex > 0;
    public bool CanPlayNext
    {
        get
        {
            var idx = PlayingIndex;
            return idx >= 0 && idx + 1 < Tracks.Count;
        }
    }

    public Func<string, Task>? PlayInvoker { get; set; }
    public Func<Task>? TogglePlayInvoker { get; set; }
    public Func<double, Task>? SeekInvoker { get; set; }

    public async Task PlayAsync(IEnumerable<PlaylistTrack> tracks, string trackId, Guid? sourcePlaylistId)
    {
        var list = tracks.ToList();
        Tracks = list;
        SourcePlaylistId = sourcePlaylistId;

        if (PlayingTrackId == trackId)
        {
            if (TogglePlayInvoker is not null) await TogglePlayInvoker();
        }
        else
        {
            PlayingTrackId = trackId;
            PositionMs = 0;
            if (PlayInvoker is not null) await PlayInvoker(trackId);
        }
        Notify();
    }

    public async Task TogglePlayPauseAsync()
    {
        if (PlayingTrackId is null || TogglePlayInvoker is null) return;
        await TogglePlayInvoker();
    }

    public async Task PlayNextAsync()
    {
        var ordered = OrderedTracks();
        var idx = ordered.FindIndex(t => t.SpotifyTrackId == PlayingTrackId);
        if (idx < 0 || idx + 1 >= ordered.Count) return;
        PlayingTrackId = ordered[idx + 1].SpotifyTrackId;
        PositionMs = 0;
        if (PlayInvoker is not null) await PlayInvoker(PlayingTrackId);
        Notify();
    }

    public async Task PlayPreviousAsync()
    {
        var ordered = OrderedTracks();
        var idx = ordered.FindIndex(t => t.SpotifyTrackId == PlayingTrackId);
        if (idx <= 0) return;
        PlayingTrackId = ordered[idx - 1].SpotifyTrackId;
        PositionMs = 0;
        if (PlayInvoker is not null) await PlayInvoker(PlayingTrackId);
        Notify();
    }

    public async Task SeekAsync(double ms)
    {
        PositionMs = ms;
        if (SeekInvoker is not null) await SeekInvoker(ms);
        Notify();
    }

    public void UpdatePlayback(double position, double duration, bool paused)
    {
        PositionMs = position;
        DurationMs = duration;
        IsPaused = paused;
        Notify();
    }

    public async Task HandleTrackEndedAsync()
    {
        var ordered = OrderedTracks();
        var idx = ordered.FindIndex(t => t.SpotifyTrackId == PlayingTrackId);
        if (idx < 0) return;

        if (idx + 1 < ordered.Count)
        {
            PlayingTrackId = ordered[idx + 1].SpotifyTrackId;
            PositionMs = 0;
            if (PlayInvoker is not null) await PlayInvoker(PlayingTrackId);
            Notify();
        }
        else
        {
            Stop();
        }
    }

    public void Stop()
    {
        PlayingTrackId = null;
        PositionMs = 0;
        DurationMs = 0;
        IsPaused = true;
        Notify();
    }

    private List<PlaylistTrack> OrderedTracks() => Tracks.OrderBy(t => t.Position).ToList();

    private void Notify() => OnChange?.Invoke();
}
