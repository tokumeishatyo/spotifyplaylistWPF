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

    [ObservableProperty]
    private bool _hasSelectedSearchResults;

    [ObservableProperty]
    private bool _hasSelectedPlaylistItems;

    // Search related properties
    [ObservableProperty]
    private bool _isSearchPanelExpanded = true;

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

    [ObservableProperty]
    private string _selectedSearchResultsText = string.Empty;

    public ObservableCollection<PlaylistViewModel> Playlists { get; } = new();
    public ObservableCollection<SearchResultViewModel> SearchResults { get; } = new();
    public ObservableCollection<string> AvailableMoods { get; } = new();

    public ICommand LogoutCommand { get; }
    public ICommand ToggleThemeCommand { get; }
    public ICommand LoadPlaylistsCommand { get; }
    public ICommand DeleteSelectedCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand ClearSearchCommand { get; }
    public ICommand SelectAllSearchResultsCommand { get; }
    public ICommand ClearAllSearchResultsCommand { get; }
    public ICommand AddToNewPlaylistCommand { get; }
    public ICommand AddToExistingPlaylistCommand { get; }

    private int _lastSelectedSearchIndex = -1;
    
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
        SelectAllSearchResultsCommand = new RelayCommand(SelectAllSearchResults);
        ClearAllSearchResultsCommand = new RelayCommand(ClearAllSearchResults);
        AddToNewPlaylistCommand = new RelayCommand(AddToNewPlaylist, CanAddToPlaylist);
        AddToExistingPlaylistCommand = new RelayCommand(AddToExistingPlaylist, CanAddToPlaylist);
        
        Playlists.CollectionChanged += OnPlaylistsCollectionChanged;
        
        // プロパティ変更の監視
        PropertyChanged += OnPropertyChanged;
        
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        // 基本情報の読み込み
        await LoadUserInfoAsync();
        await LoadCurrentThemeAsync();
        
        // 気分データの初期化（プレイリスト読み込みと並行して実行）
        InitializeMoodsFromConfig();
        
        // プレイリスト読み込みと検索キャッシュ初期化を並行実行
        var playlistTask = LoadPlaylistsAsync();
        var searchCacheTask = InitializeSearchCacheAsync();
        
        await Task.WhenAll(playlistTask, searchCacheTask);
    }
    
    private void InitializeMoodsFromConfig()
    {
        try
        {
            Console.WriteLine("[MainViewModel] 気分データ初期化開始");
            
            // 気分の選択肢を設定ファイルから読み込み
            var moods = _searchService.GetAvailableMoods();
            AvailableMoods.Clear();
            foreach (var mood in moods)
            {
                AvailableMoods.Add(mood);
                Console.WriteLine($"[MainViewModel] 気分追加: {mood}");
            }
            
            Console.WriteLine($"[MainViewModel] 気分データ初期化完了: {AvailableMoods.Count}種類");
            
            // UIの強制更新
            OnPropertyChanged(nameof(AvailableMoods));
            OnPropertyChanged(nameof(IsKeywordSearchMode));
            OnPropertyChanged(nameof(IsOmakaseSearchMode));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MainViewModel] 気分データ初期化エラー: {ex.Message}");
        }
    }
    
    private async Task InitializeSearchCacheAsync()
    {
        try
        {
            Console.WriteLine("[MainViewModel] 検索キャッシュ初期化開始");
            await _searchService.InitializeCacheAsync();
            Console.WriteLine("[MainViewModel] 検索キャッシュ初期化完了");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MainViewModel] 検索キャッシュ初期化エラー: {ex.Message}");
        }
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
        // プレイリストアイテムの選択状態
        HasSelectedPlaylistItems = Playlists.Any(p => p.IsChecked == true || p.Tracks.Any(t => t.IsSelected));
        
        // 検索結果の選択状態
        HasSelectedSearchResults = SearchResults.Any(r => r.IsSelected);
        
        // 全体の選択状態
        HasSelectedItems = HasSelectedPlaylistItems || HasSelectedSearchResults;
        
        ((AsyncRelayCommand)DeleteSelectedCommand).NotifyCanExecuteChanged();
        ((RelayCommand)AddToNewPlaylistCommand).NotifyCanExecuteChanged();
        ((RelayCommand)AddToExistingPlaylistCommand).NotifyCanExecuteChanged();
    }

    private bool CanDeleteSelectedItems()
    {
        // 検索結果のみが選択されている場合はDeleteボタンを無効にする
        return HasSelectedPlaylistItems;
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

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(IsKeywordSearchMode):
                if (IsKeywordSearchMode)
                {
                    IsOmakaseSearchMode = false;
                    Console.WriteLine("[MainViewModel] キーワード検索モードに切替");
                }
                UpdateSearchCommandCanExecute();
                break;
            case nameof(IsOmakaseSearchMode):
                if (IsOmakaseSearchMode)
                {
                    IsKeywordSearchMode = false;
                    Console.WriteLine("[MainViewModel] おまかせ検索モードに切替");
                }
                UpdateSearchCommandCanExecute();
                break;
            case nameof(SearchTrackName):
            case nameof(SearchArtistName):
            case nameof(SearchAlbumName):
            case nameof(SelectedMood):
                // Clear search results when search conditions change
                foreach (var result in SearchResults)
                {
                    result.SelectionChanged -= OnSearchResultSelectionChanged;
                }
                SearchResults.Clear();
                HasSearchResults = false;
                SearchResultsText = string.Empty;
                SelectedSearchResultsText = string.Empty;
                _lastSelectedSearchIndex = -1;
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
                var searchResultViewModel = new SearchResultViewModel(result);
                searchResultViewModel.SelectionChanged += OnSearchResultSelectionChanged;
                SearchResults.Add(searchResultViewModel);
                Console.WriteLine($"[MainViewModel] 検索結果追加: {result.TrackInfo.Name} - IsSelected: {searchResultViewModel.IsSelected}");
            }

            HasSearchResults = SearchResults.Any();
            SearchResultsText = SearchResults.Any() 
                ? $"検索結果: {SearchResults.Count}件"
                : "該当する楽曲が見つかりませんでした。";
            SelectedSearchResultsText = string.Empty;

            Console.WriteLine($"[MainViewModel] 検索完了: {SearchResults.Count}件");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MainViewModel] 検索エラー: {ex.Message}");
            // Clear existing search results and unsubscribe from events
            foreach (var result in SearchResults)
            {
                result.SelectionChanged -= OnSearchResultSelectionChanged;
            }
            SearchResults.Clear();
            HasSearchResults = false;
            SearchResultsText = "検索中にエラーが発生しました。";
            SelectedSearchResultsText = string.Empty;
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
        // Clear existing search results and unsubscribe from events
        foreach (var result in SearchResults)
        {
            result.SelectionChanged -= OnSearchResultSelectionChanged;
        }
        SearchResults.Clear();
        HasSearchResults = false;
        SearchResultsText = string.Empty;
        SelectedSearchResultsText = string.Empty;
        
        Console.WriteLine("[MainViewModel] 検索条件クリア");
    }

    // Search result selection methods
    
    private void SelectAllSearchResults()
    {
        foreach (var result in SearchResults)
        {
            result.IsSelected = true;
        }
        UpdateSelectedSearchResultsText();
        Console.WriteLine("[MainViewModel] 検索結果を全選択");
    }
    
    private void ClearAllSearchResults()
    {
        foreach (var result in SearchResults)
        {
            result.IsSelected = false;
        }
        UpdateSelectedSearchResultsText();
        Console.WriteLine("[MainViewModel] 検索結果の選択を全解除");
    }
    
    private void UpdateSelectedSearchResultsText()
    {
        var selectedCount = SearchResults.Count(r => r.IsSelected);
        SelectedSearchResultsText = selectedCount > 0 ? $"({selectedCount}件選択)" : string.Empty;
    }

    private void OnSearchResultSelectionChanged(object? sender, EventArgs e)
    {
        UpdateSelectedSearchResultsText();
        UpdateHasSelectedItems();
    }

    public void OnSearchResultCheckBoxPreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is not System.Windows.Controls.CheckBox checkBox ||
            checkBox.DataContext is not SearchResultViewModel currentItem)
            return;

        var currentIndex = SearchResults.IndexOf(currentItem);
        if (currentIndex == -1) return;

        // Shift+クリックの場合は範囲選択
        if (System.Windows.Input.Keyboard.Modifiers.HasFlag(System.Windows.Input.ModifierKeys.Shift) && 
            _lastSelectedSearchIndex >= 0 && _lastSelectedSearchIndex < SearchResults.Count)
        {
            var startIndex = Math.Min(_lastSelectedSearchIndex, currentIndex);
            var endIndex = Math.Max(_lastSelectedSearchIndex, currentIndex);
            
            // 範囲内のアイテムを選択状態にする
            for (int i = startIndex; i <= endIndex; i++)
            {
                SearchResults[i].IsSelected = true;
            }
            
            UpdateSelectedSearchResultsText();
            
            // マウスイベントをキャンセルして、デフォルトのチェックボックス動作を防ぐ
            e.Handled = true;
        }
        else
        {
            // 通常のクリックの場合は最後の選択インデックスを更新
            _lastSelectedSearchIndex = currentIndex;
        }
    }

    // Playlist addition methods
    
    private bool CanAddToPlaylist()
    {
        return HasSelectedItems;
    }

    private async void AddToNewPlaylist()
    {
        try
        {
            var selectedTracks = new List<SpotifyManager.Core.Models.TrackInfo>();
            
            // 検索結果から選択された楽曲を追加
            selectedTracks.AddRange(SearchResults.Where(r => r.IsSelected).Select(r => r.TrackInfo));
            
            // プレイリストから選択された楽曲を追加
            foreach (var playlist in Playlists)
            {
                selectedTracks.AddRange(playlist.Tracks.Where(t => t.IsSelected).Select(t => t.TrackInfo));
            }
            
            if (selectedTracks.Count == 0)
            {
                System.Windows.MessageBox.Show(
                    "楽曲が選択されていません。",
                    "情報",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
                return;
            }
            
            Console.WriteLine($"[MainViewModel] 新規プレイリストに{selectedTracks.Count}件の楽曲を追加");
            
            // ダイアログを表示
            var dialog = new SpotifyManager.Wpf.Views.CreatePlaylistDialog(selectedTracks.Count);
            var result = dialog.ShowDialog();
            
            if (result != true || string.IsNullOrWhiteSpace(dialog.PlaylistName))
            {
                Console.WriteLine("[MainViewModel] プレイリスト作成がキャンセルされました");
                return;
            }
            
            // プレイリストを作成
            var newPlaylist = await _playlistService.CreatePlaylistAsync(dialog.PlaylistName);
            
            // 楽曲を追加
            if (selectedTracks.Any())
            {
                var trackUris = selectedTracks.Select(t => t.Uri).ToList();
                await _playlistService.AddTracksToPlaylistAsync(newPlaylist.Id, trackUris);
                
                // TrackCountを更新
                newPlaylist.TrackCount = selectedTracks.Count;
                
                // 最初の楽曲のアルバム画像をプレイリスト画像として設定
                var firstTrackWithImage = selectedTracks.FirstOrDefault(t => !string.IsNullOrEmpty(t.AlbumImageUrl));
                if (firstTrackWithImage != null)
                {
                    newPlaylist.ImageUrl = firstTrackWithImage.AlbumImageUrl;
                }
            }
            
            // プレイリストViewModelを作成してリストに追加
            var playlistViewModel = new PlaylistViewModel(newPlaylist, _playlistService);
            playlistViewModel.PropertyChanged += OnPlaylistPropertyChanged;
            playlistViewModel.SelectionChanged += OnPlaylistSelectionChanged;
            Playlists.Add(playlistViewModel);
            
            // 選択をクリア
            ClearAllSelections();
            
            System.Windows.MessageBox.Show(
                $"プレイリスト「{newPlaylist.Name}」を作成し、{selectedTracks.Count}件の楽曲を追加しました。",
                "完了",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
                
            Console.WriteLine($"[MainViewModel] 新規プレイリスト作成完了: {newPlaylist.Name}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MainViewModel] 新規プレイリスト追加エラー: {ex.Message}");
            System.Windows.MessageBox.Show(
                $"新規プレイリスト作成中にエラーが発生しました: {ex.Message}",
                "エラー",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private async void AddToExistingPlaylist()
    {
        try
        {
            var selectedTracks = new List<SpotifyManager.Core.Models.TrackInfo>();
            
            // 検索結果から選択された楽曲を追加
            selectedTracks.AddRange(SearchResults.Where(r => r.IsSelected).Select(r => r.TrackInfo));
            
            // プレイリストから選択された楽曲を追加
            foreach (var playlist in Playlists)
            {
                selectedTracks.AddRange(playlist.Tracks.Where(t => t.IsSelected).Select(t => t.TrackInfo));
            }
            
            if (selectedTracks.Count == 0)
            {
                System.Windows.MessageBox.Show(
                    "楽曲が選択されていません。",
                    "情報",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
                return;
            }
            
            Console.WriteLine($"[MainViewModel] 既存プレイリストに{selectedTracks.Count}件の楽曲を追加");
            
            // プレイリスト選択ダイアログを表示
            var dialog = new SpotifyManager.Wpf.Views.SelectPlaylistDialog(Playlists, selectedTracks.Count);
            var result = dialog.ShowDialog();
            
            if (result != true || !dialog.SelectedPlaylists.Any())
            {
                Console.WriteLine("[MainViewModel] プレイリスト選択がキャンセルされました");
                return;
            }
            
            var selectedPlaylists = dialog.SelectedPlaylists.ToList();
            var trackUris = selectedTracks.Select(t => t.Uri).ToList();
            var successCount = 0;
            var errorMessages = new List<string>();
            
            // 選択された各プレイリストに楽曲を追加
            foreach (var playlist in selectedPlaylists)
            {
                try
                {
                    await _playlistService.AddTracksToPlaylistAsync(playlist.PlaylistInfo.Id, trackUris);
                    
                    // トラック数を更新
                    playlist.PlaylistInfo.TrackCount += selectedTracks.Count;
                    playlist.OnPropertyChanged(nameof(playlist.PlaylistInfo));
                    
                    successCount++;
                    Console.WriteLine($"[MainViewModel] プレイリスト「{playlist.PlaylistInfo.Name}」に{selectedTracks.Count}件を追加完了");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MainViewModel] プレイリスト「{playlist.PlaylistInfo.Name}」への追加エラー: {ex.Message}");
                    errorMessages.Add($"「{playlist.PlaylistInfo.Name}」: {ex.Message}");
                }
            }
            
            // 選択をクリア
            ClearAllSelections();
            
            // 結果を表示
            if (successCount == selectedPlaylists.Count)
            {
                System.Windows.MessageBox.Show(
                    $"{selectedTracks.Count}件の楽曲を{successCount}個のプレイリストに追加しました。",
                    "完了",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
            else
            {
                var message = $"{selectedTracks.Count}件の楽曲を{successCount}/{selectedPlaylists.Count}個のプレイリストに追加しました。";
                if (errorMessages.Any())
                {
                    message += $"\n\nエラーが発生したプレイリスト:\n{string.Join("\n", errorMessages)}";
                }
                
                System.Windows.MessageBox.Show(
                    message,
                    "部分的に完了",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
            }
            
            Console.WriteLine($"[MainViewModel] 既存プレイリスト追加完了: {successCount}/{selectedPlaylists.Count}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MainViewModel] 既存プレイリスト追加エラー: {ex.Message}");
            System.Windows.MessageBox.Show(
                $"既存プレイリスト追加中にエラーが発生しました: {ex.Message}",
                "エラー",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private void ClearAllSelections()
    {
        // 検索結果の選択をクリア
        foreach (var result in SearchResults)
        {
            result.IsSelected = false;
        }
        
        // プレイリストの選択をクリア
        foreach (var playlist in Playlists)
        {
            playlist.IsChecked = false;
            foreach (var track in playlist.Tracks)
            {
                track.IsSelected = false;
            }
        }
        
        Console.WriteLine("[MainViewModel] すべての選択をクリアしました");
    }
}