namespace SpotifyManager.Core.Interfaces;

public interface IAuthService
{
    Task<bool> IsAuthenticatedAsync();
    Task<bool> LoginAsync();
    Task LogoutAsync();
}