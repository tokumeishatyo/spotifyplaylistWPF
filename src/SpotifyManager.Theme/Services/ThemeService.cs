using SpotifyManager.Core.Interfaces;

namespace SpotifyManager.Theme.Services;

public class ThemeService : IThemeService
{
    private string _currentTheme = "Light";
    public event EventHandler<string>? ThemeChanged;

    public Task<string> GetCurrentThemeAsync()
    {
        return Task.FromResult(_currentTheme);
    }

    public Task SetThemeAsync(string themeName)
    {
        _currentTheme = themeName;
        ThemeChanged?.Invoke(this, themeName);
        return Task.CompletedTask;
    }
}