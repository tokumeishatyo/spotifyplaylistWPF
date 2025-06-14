using SpotifyManager.Core.Models;

namespace SpotifyManager.Core.Interfaces;

public interface IPlaylistService
{
    Task<IEnumerable<PlaylistInfo>> GetPlaylistsAsync();
    Task<IEnumerable<TrackInfo>> GetTracksAsync(string playlistId);
    Task DeletePlaylistAsync(string playlistId);
    Task DeleteTracksAsync(string playlistId, IEnumerable<string> trackUris);
}