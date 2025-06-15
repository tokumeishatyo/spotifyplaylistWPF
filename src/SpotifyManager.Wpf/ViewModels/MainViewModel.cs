using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SpotifyManager.Core.Interfaces;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
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

    [ObservableProperty]
    private bool _hasSelectedItems;

    public ObservableCollection<PlaylistViewModel> Playlists { get; } = new();

    public ICommand LogoutCommand { get; }
    public ICommand ToggleThemeCommand { get; }
    public ICommand LoadPlaylistsCommand { get; }
    public ICommand DeleteSelectedCommand { get; }
    
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
        DeleteSelectedCommand = new AsyncRelayCommand(DeleteSelectedItemsAsync, CanDeleteSelectedItems);
        
        Playlists.CollectionChanged += OnPlaylistsCollectionChanged;
        
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
                var playlistViewModel = new PlaylistViewModel(playlist, _playlistService);
                playlistViewModel.PropertyChanged += OnPlaylistPropertyChanged;
                playlistViewModel.SelectionChanged += OnPlaylistSelectionChanged;
                Playlists.Add(playlistViewModel);
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

    private void OnPlaylistsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (PlaylistViewModel playlist in e.NewItems)
            {
                playlist.PropertyChanged += OnPlaylistPropertyChanged;
                playlist.SelectionChanged += OnPlaylistSelectionChanged;
            }
        }
        
        if (e.OldItems != null)
        {
            foreach (PlaylistViewModel playlist in e.OldItems)
            {
                playlist.PropertyChanged -= OnPlaylistPropertyChanged;
                playlist.SelectionChanged -= OnPlaylistSelectionChanged;
            }
        }
    }

    private void OnPlaylistPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PlaylistViewModel.IsChecked))
        {
            UpdateHasSelectedItems();
        }
    }

    private void OnPlaylistSelectionChanged(object? sender, EventArgs e)
    {
        UpdateHasSelectedItems();
    }

    private void UpdateHasSelectedItems()
    {
        HasSelectedItems = Playlists.Any(p => p.IsChecked == true || p.Tracks.Any(t => t.IsSelected));
        ((AsyncRelayCommand)DeleteSelectedCommand).NotifyCanExecuteChanged();
    }

    private bool CanDeleteSelectedItems()
    {
        return HasSelectedItems;
    }

    private async Task DeleteSelectedItemsAsync()
    {
        // TODO: 実装は次のPBIで行う
        Console.WriteLine("[MainViewModel] 削除処理（未実装）");
        await Task.CompletedTask;
    }
}