namespace SpotifyManager.Core.Interfaces;

public interface IThemeService
{
    Task<string> GetCurrentThemeAsync();
    Task SetThemeAsync(string themeName);
    event EventHandler<string>? ThemeChanged;
}