namespace SpotifyManager.Core.Interfaces;

public interface IAuthService
{
    Task<bool> IsAuthenticatedAsync();
    Task<bool> LoginAsync();
    Task LogoutAsync();
    Task<(string? UserId, string? DisplayName, string? Email)> GetUserInfoAsync();
    object? GetSpotifyClient(); // SpotifyAPI.Webへの参照を避けるためobject型
}