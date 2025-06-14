namespace SpotifyManager.Core.Models;

public class PlaylistInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int TrackCount { get; set; }
    public bool IsOwner { get; set; }
    public bool CanEdit { get; set; }
    public bool IsPublic { get; set; }
}