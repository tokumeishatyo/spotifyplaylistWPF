namespace SpotifyManager.Core.Models;

public class TrackInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Uri { get; set; } = string.Empty;
    public string PlaylistId { get; set; } = string.Empty;
    public IList<string> Artists { get; set; } = new List<string>();
    public string? AlbumName { get; set; }
    public string? AlbumImageUrl { get; set; }
    public int DurationMs { get; set; }
    public bool IsLocal { get; set; }
    
    public string ArtistsString => string.Join(", ", Artists);
    public string DisplayName => $"{Name} - {ArtistsString}";
}