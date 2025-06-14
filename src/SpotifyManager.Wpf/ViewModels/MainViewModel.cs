using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SpotifyManager.Core.Interfaces;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SpotifyManager.Wpf.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly IPlaylistService _playlistService;
    private readonly IThemeService _themeService;

    [ObservableProperty]
    private string _userName = "ユーザー";

    [ObservableProperty]
    private string _currentTheme = "Light";

    [ObservableProperty]
    private bool _isLoadingPlaylists;

    public ObservableCollection<PlaylistViewModel> Playlists { get; } = new();

    public ICommand LogoutCommand { get; }
    public ICommand ToggleThemeCommand { get; }
    public ICommand LoadPlaylistsCommand { get; }
    
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
        LoadPlaylistsCommand = new AsyncRelayCommand(LoadPlaylistsAsync);
        
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await LoadUserInfoAsync();
        await LoadCurrentThemeAsync();
        await LoadPlaylistsAsync();
    }

    private async Task LoadUserInfoAsync()
    {
        try
        {
            var (userId, displayName, email) = await _authService.GetUserInfoAsync();
            UserName = displayName ?? email ?? userId ?? "ユーザー";
        }
        catch
        {
            UserName = "ユーザー";
        }
    }

    private async Task LoadCurrentThemeAsync()
    {
        try
        {
            CurrentTheme = await _themeService.GetCurrentThemeAsync();
        }
        catch
        {
            CurrentTheme = "Light";
        }
    }

    private async Task LogoutAsync()
    {
        await _authService.LogoutAsync();
        LogoutRequested?.Invoke(this, EventArgs.Empty);
    }

    private async Task ToggleThemeAsync()
    {
        var newTheme = CurrentTheme == "Light" ? "Dark" : "Light";
        await _themeService.SetThemeAsync(newTheme);
        CurrentTheme = newTheme;
    }

    private async Task LoadPlaylistsAsync()
    {
        if (IsLoadingPlaylists)
            return;

        try
        {
            IsLoadingPlaylists = true;
            Console.WriteLine("[MainViewModel] プレイリスト読み込み開始");
            
            var playlists = await _playlistService.GetPlaylistsAsync();
            
            Playlists.Clear();
            foreach (var playlist in playlists)
            {
                Playlists.Add(new PlaylistViewModel(playlist, _playlistService));
            }
            
            Console.WriteLine($"[MainViewModel] プレイリスト読み込み完了: {Playlists.Count}件");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MainViewModel] プレイリスト読み込みエラー: {ex.Message}");
        }
        finally
        {
            IsLoadingPlaylists = false;
        }
    }
}