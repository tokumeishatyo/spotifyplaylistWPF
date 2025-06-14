namespace SpotifyManager.Core.Interfaces;

public interface IPlaylistService
{
    Task<IEnumerable<object>> GetPlaylistsAsync();
    Task<IEnumerable<object>> GetTracksAsync(string playlistId);
    Task DeletePlaylistAsync(string playlistId);
    Task DeleteTracksAsync(string playlistId, IEnumerable<string> trackUris);
}