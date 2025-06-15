namespace SpotifyManager.Core.Interfaces;

public interface IThemeService
{
    Task<string> GetCurrentThemeAsync();
    Task SetThemeAsync(string themeName);
    void ApplyInitialTheme();
    event EventHandler<string>? ThemeChanged;
}