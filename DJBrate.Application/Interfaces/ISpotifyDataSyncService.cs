namespace DJBrate.Application.Interfaces;

public interface ISpotifyDataSyncService
{
    Task SyncUserTopDataAsync(Guid userId);
}
