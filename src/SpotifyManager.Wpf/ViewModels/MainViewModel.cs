using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SpotifyManager.Core.Interfaces;
using System.Windows.Input;

namespace SpotifyManager.Wpf.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly IPlaylistService _playlistService;
    private readonly IThemeService _themeService;

    [ObservableProperty]
    private string _userName = "ユーザー";

    public ICommand LogoutCommand { get; }
    public ICommand ToggleThemeCommand { get; }
    
    public event EventHandler? LogoutRequested;

    public MainViewModel(
        IAuthService authService,
        IPlaylistService playlistService,
        IThemeService themeService)
    {
        _authService = authService;
        _playlistService = playlistService;
        _themeService = themeService;

        LogoutCommand = new AsyncRelayCommand(LogoutAsync);
        ToggleThemeCommand = new AsyncRelayCommand(ToggleThemeAsync);
    }

    private async Task LogoutAsync()
    {
        await _authService.LogoutAsync();
        LogoutRequested?.Invoke(this, EventArgs.Empty);
    }

    private async Task ToggleThemeAsync()
    {
        var currentTheme = await _themeService.GetCurrentThemeAsync();
        var newTheme = currentTheme == "Light" ? "Dark" : "Light";
        await _themeService.SetThemeAsync(newTheme);
    }
}