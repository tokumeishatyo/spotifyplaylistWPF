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
    private readonly ISearchService _searchService;

    [ObservableProperty]
    private string _userName = "ユーザー";

    [ObservableProperty]
    private string _currentTheme = "Light";

    [ObservableProperty]
    private bool _isLoadingPlaylists;

    [ObservableProperty]
    private bool _hasSelectedItems;

    // Search related properties
    [ObservableProperty]
    private bool _isSearchPanelExpanded;

    [ObservableProperty]
    private bool _isKeywordSearchMode = true;

    [ObservableProperty]
    private bool _isOmakaseSearchMode;

    [ObservableProperty]
    private string _searchTrackName = string.Empty;

    [ObservableProperty]
    private string _searchArtistName = string.Empty;

    [ObservableProperty]
    private string _searchAlbumName = string.Empty;

    [ObservableProperty]
    private string _selectedMood = string.Empty;

    [ObservableProperty]
    private int _maxResults = 20;

    [ObservableProperty]
    private string _searchButtonText = "検索";

    [ObservableProperty]
    private bool _hasSearchResults;

    [ObservableProperty]
    private string _searchResultsText = string.Empty;

    public ObservableCollection<PlaylistViewModel> Playlists { get; } = new();
    public ObservableCollection<SearchResultViewModel> SearchResults { get; } = new();
    public ObservableCollection<string> AvailableMoods { get; } = new();

    public ICommand LogoutCommand { get; }
    public ICommand ToggleThemeCommand { get; }
    public ICommand LoadPlaylistsCommand { get; }
    public ICommand DeleteSelectedCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand ClearSearchCommand { get; }
    
    public event EventHandler? LogoutRequested;

    public MainViewModel(
        IAuthService authService,
        IPlaylistService playlistService,
        IThemeService themeService,
        ISearchService searchService)
    {
        _authService = authService;
        _playlistService = playlistService;
        _themeService = themeService;
        _searchService = searchService;

        LogoutCommand = new AsyncRelayCommand(LogoutAsync);
        ToggleThemeCommand = new AsyncRelayCommand(ToggleThemeAsync);
        LoadPlaylistsCommand = new AsyncRelayCommand(LoadPlaylistsAsync);
        DeleteSelectedCommand = new AsyncRelayCommand(DeleteSelectedItemsAsync, CanDeleteSelectedItems);
        SearchCommand = new AsyncRelayCommand(SearchAsync, CanSearch);
        ClearSearchCommand = new RelayCommand(ClearSearch);
        
        Playlists.CollectionChanged += OnPlaylistsCollectionChanged;
        
        // プロパティ変更の監視
        PropertyChanged += OnPropertyChanged;
        
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await LoadUserInfoAsync();
        await LoadCurrentThemeAsync();
        await LoadPlaylistsAsync();
        await InitializeSearchAsync();
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

    // Search related methods
    private async Task InitializeSearchAsync()
    {
        try
        {
            Console.WriteLine("[MainViewModel] 検索初期化開始");
            
            // 検索サービスのキャッシュ初期化
            await _searchService.InitializeCacheAsync();
            
            // 気分の選択肢を読み込み
            var moods = _searchService.GetAvailableMoods();
            foreach (var mood in moods)
            {
                AvailableMoods.Add(mood);
            }
            
            Console.WriteLine($"[MainViewModel] 検索初期化完了: {AvailableMoods.Count}種類の気分");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MainViewModel] 検索初期化エラー: {ex.Message}");
        }
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(IsKeywordSearchMode):
                if (IsKeywordSearchMode)
                    IsOmakaseSearchMode = false;
                UpdateSearchCommandCanExecute();
                break;
            case nameof(IsOmakaseSearchMode):
                if (IsOmakaseSearchMode)
                    IsKeywordSearchMode = false;
                UpdateSearchCommandCanExecute();
                break;
            case nameof(SearchTrackName):
            case nameof(SearchArtistName):
            case nameof(SearchAlbumName):
            case nameof(SelectedMood):
                UpdateSearchCommandCanExecute();
                break;
        }
    }

    private void UpdateSearchCommandCanExecute()
    {
        ((AsyncRelayCommand)SearchCommand).NotifyCanExecuteChanged();
    }

    private bool CanSearch()
    {
        if (IsKeywordSearchMode)
        {
            return !string.IsNullOrWhiteSpace(SearchTrackName) ||
                   !string.IsNullOrWhiteSpace(SearchArtistName) ||
                   !string.IsNullOrWhiteSpace(SearchAlbumName);
        }
        else
        {
            return !string.IsNullOrWhiteSpace(SelectedMood);
        }
    }

    private async Task SearchAsync()
    {
        try
        {
            SearchButtonText = "検索中...";
            Console.WriteLine("[MainViewModel] 検索開始");

            var request = new SearchRequest
            {
                Mode = IsKeywordSearchMode ? SearchMode.Keyword : SearchMode.Omakase,
                TrackName = SearchTrackName,
                ArtistName = SearchArtistName,
                AlbumName = SearchAlbumName,
                Mood = SelectedMood,
                MaxResults = MaxResults
            };

            var results = await _searchService.SearchAsync(request);
            
            SearchResults.Clear();
            foreach (var result in results)
            {
                SearchResults.Add(new SearchResultViewModel(result));
            }

            HasSearchResults = SearchResults.Any();
            SearchResultsText = SearchResults.Any() 
                ? $"検索結果: {SearchResults.Count}件"
                : "該当する楽曲が見つかりませんでした。";

            Console.WriteLine($"[MainViewModel] 検索完了: {SearchResults.Count}件");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MainViewModel] 検索エラー: {ex.Message}");
            SearchResults.Clear();
            HasSearchResults = false;
            SearchResultsText = "検索中にエラーが発生しました。";
        }
        finally
        {
            SearchButtonText = "検索";
        }
    }

    private void ClearSearch()
    {
        SearchTrackName = string.Empty;
        SearchArtistName = string.Empty;
        SearchAlbumName = string.Empty;
        SelectedMood = string.Empty;
        SearchResults.Clear();
        HasSearchResults = false;
        SearchResultsText = string.Empty;
        
        Console.WriteLine("[MainViewModel] 検索条件クリア");
    }
}