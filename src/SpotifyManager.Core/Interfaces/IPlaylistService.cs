using SpotifyManager.Core.Models;

namespace SpotifyManager.Core.Interfaces;

public interface IPlaylistService
{
    Task<IEnumerable<PlaylistInfo>> GetPlaylistsAsync();
    Task<IEnumerable<TrackInfo>> GetTracksAsync(string playlistId);
    Task DeletePlaylistAsync(string playlistId);
    Task DeleteTracksAsync(string playlistId, IEnumerable<string> trackUris);
    Task<IEnumerable<TrackInfo>> SearchTracksAsync(string query, int limit = 20);
    Task<PlaylistInfo> CreatePlaylistAsync(string name, string? description = null);
    Task AddTracksToPlaylistAsync(string playlistId, IEnumerable<string> trackUris);
}