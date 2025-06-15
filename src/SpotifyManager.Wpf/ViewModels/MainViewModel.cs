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
        try
        {
            // 確認ダイアログ
            var result = System.Windows.MessageBox.Show(
                "選択されたアイテムを削除します。この操作は元に戻せません。よろしいですか？",
                "削除確認",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result != System.Windows.MessageBoxResult.Yes)
                return;

            Console.WriteLine("[MainViewModel] 削除処理開始");

            // 選択された項目を収集
            var playlistsToDelete = new List<PlaylistViewModel>();
            var tracksToDeleteByPlaylist = new Dictionary<string, List<TrackViewModel>>();

            foreach (var playlist in Playlists)
            {
                // プレイリスト全体が選択されている場合
                if (playlist.IsChecked == true)
                {
                    playlistsToDelete.Add(playlist);
                }
                // 楽曲が個別に選択されている場合
                else if (playlist.Tracks.Any(t => t.IsSelected))
                {
                    var selectedTracks = playlist.Tracks.Where(t => t.IsSelected).ToList();
                    tracksToDeleteByPlaylist[playlist.PlaylistInfo.Id] = selectedTracks;
                }
            }

            // プレイリスト削除
            foreach (var playlist in playlistsToDelete)
            {
                Console.WriteLine($"[MainViewModel] プレイリスト削除: {playlist.PlaylistInfo.Name}");
                await _playlistService.DeletePlaylistAsync(playlist.PlaylistInfo.Id);
            }

            // 楽曲削除
            foreach (var kvp in tracksToDeleteByPlaylist)
            {
                var playlistId = kvp.Key;
                var tracks = kvp.Value;
                var trackUris = tracks.Select(t => t.TrackInfo.Uri).ToList();
                
                Console.WriteLine($"[MainViewModel] 楽曲削除: プレイリスト {playlistId}, {tracks.Count}件");
                await _playlistService.DeleteTracksAsync(playlistId, trackUris);
                
                // プレイリストから削除された楽曲を除去してトラック数を更新
                var playlist = Playlists.FirstOrDefault(p => p.PlaylistInfo.Id == playlistId);
                if (playlist != null)
                {
                    // 削除された楽曲をTracksコレクションから除去
                    foreach (var trackToRemove in tracks)
                    {
                        playlist.Tracks.Remove(trackToRemove);
                    }
                    
                    // トラック数を更新
                    playlist.PlaylistInfo.TrackCount = playlist.Tracks.Count;
                    playlist.OnPropertyChanged(nameof(playlist.PlaylistInfo));
                }
            }

            // UIを更新（プレイリスト一覧を再読み込み）
            await LoadPlaylistsAsync();

            Console.WriteLine("[MainViewModel] 削除処理完了");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MainViewModel] 削除処理エラー: {ex.Message}");
            System.Windows.MessageBox.Show(
                $"削除処理中にエラーが発生しました: {ex.Message}",
                "エラー",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }
}