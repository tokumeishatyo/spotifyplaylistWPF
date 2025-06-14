using SpotifyManager.Core.Interfaces;

namespace SpotifyManager.Playlist.Services;

public class PlaylistService : IPlaylistService
{
    public Task<IEnumerable<object>> GetPlaylistsAsync()
    {
        // TODO: Implement playlist retrieval
        return Task.FromResult<IEnumerable<object>>(Enumerable.Empty<object>());
    }

    public Task<IEnumerable<object>> GetTracksAsync(string playlistId)
    {
        // TODO: Implement track retrieval
        return Task.FromResult<IEnumerable<object>>(Enumerable.Empty<object>());
    }

    public Task DeletePlaylistAsync(string playlistId)
    {
        // TODO: Implement playlist deletion
        return Task.CompletedTask;
    }

    public Task DeleteTracksAsync(string playlistId, IEnumerable<string> trackUris)
    {
        // TODO: Implement track deletion
        return Task.CompletedTask;
    }
}