using SpotifyManager.Core.Models;

namespace SpotifyManager.Core.Interfaces;

public interface ISearchService
{
    Task<IEnumerable<SearchResult>> SearchAsync(SearchRequest request);
    Task InitializeCacheAsync();
    IEnumerable<string> GetAvailableMoods();
}

public class SearchResult
{
    public TrackInfo TrackInfo { get; set; } = null!;
    public string PlaylistName { get; set; } = string.Empty;
    public string PlaylistId { get; set; } = string.Empty;
}

public class SearchRequest
{
    public SearchMode Mode { get; set; }
    public string? TrackName { get; set; }
    public string? ArtistName { get; set; }
    public string? AlbumName { get; set; }
    public string? Mood { get; set; }
    public int MaxResults { get; set; } = 20;
}

public enum SearchMode
{
    Keyword,
    Omakase
}