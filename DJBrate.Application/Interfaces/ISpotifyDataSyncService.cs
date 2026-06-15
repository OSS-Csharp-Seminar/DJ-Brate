namespace DJBrate.Application.Interfaces;

// Definira sinkronizaciju korisnikovih top Spotify podataka.
public interface ISpotifyDataSyncService
{
    Task SyncUserTopDataAsync(Guid userId);
}
