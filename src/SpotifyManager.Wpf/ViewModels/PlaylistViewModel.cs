using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SpotifyManager.Core.Interfaces;
using SpotifyManager.Core.Models;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Input;

namespace SpotifyManager.Wpf.ViewModels;

public partial class PlaylistViewModel : ObservableObject
{
    private readonly IPlaylistService _playlistService;
    
    [ObservableProperty]
    private PlaylistInfo _playlistInfo;
    
    [ObservableProperty]
    private bool _isExpanded;
    
    [ObservableProperty]
    private bool _isLoading;
    
    [ObservableProperty]
    private bool _tracksLoaded;

    private bool? _isChecked = false;
    private bool? _pendingCheckState = null;

    public bool? IsChecked
    {
        get => _isChecked;
        set
        {
            if (_isChecked != value)
            {
                _isChecked = value;
                OnPropertyChanged();
                
                // ユーザーが直接プレイリストのチェックボックスをクリックした場合
                if (!_isUpdatingFromChildren && value.HasValue)
                {
                    if (TracksLoaded)
                    {
                        SetAllTracksChecked(value.Value);
                    }
                    else
                    {
                        // 楽曲が未読み込みの場合、状態を保持
                        _pendingCheckState = value;
                    }
                }
                
                OnSelectionChanged();
            }
        }
    }

    private bool _isUpdatingFromChildren = false;

    public ObservableCollection<TrackViewModel> Tracks { get; } = new();
    
    public ICommand LoadTracksCommand { get; }
    public ICommand ToggleExpandCommand { get; }
    public ICommand ToggleCheckCommand { get; }
    
    private TrackViewModel? _lastSelectedTrack;

    public PlaylistViewModel(PlaylistInfo playlistInfo, IPlaylistService playlistService)
    {
        _playlistInfo = playlistInfo;
        _playlistService = playlistService;
        
        LoadTracksCommand = new AsyncRelayCommand(LoadTracksAsync);
        ToggleExpandCommand = new RelayCommand(ToggleExpand);
        ToggleCheckCommand = new RelayCommand(ToggleCheck);
        
        Tracks.CollectionChanged += OnTracksCollectionChanged;
    }

    private void ToggleExpand()
    {
        IsExpanded = !IsExpanded;
        
        // パフォーマンス考慮：展開時にのみ楽曲を読み込み
        if (IsExpanded && !TracksLoaded)
        {
            LoadTracksCommand.Execute(null);
        }
    }

    private async Task LoadTracksAsync()
    {
        if (TracksLoaded || IsLoading)
            return;

        try
        {
            IsLoading = true;
            Console.WriteLine($"[PlaylistViewModel] 楽曲読み込み開始: {PlaylistInfo.Name}");
            
            var tracks = await _playlistService.GetTracksAsync(PlaylistInfo.Id);
            
            Tracks.Clear();
            foreach (var track in tracks)
            {
                var trackViewModel = new TrackViewModel(track);
                trackViewModel.PropertyChanged += OnTrackPropertyChanged;
                Tracks.Add(trackViewModel);
            }
            
            TracksLoaded = true;
            Console.WriteLine($"[PlaylistViewModel] 楽曲読み込み完了: {Tracks.Count}件");
            
            // 保留中のチェック状態があれば適用
            if (_pendingCheckState.HasValue)
            {
                SetAllTracksChecked(_pendingCheckState.Value);
                _pendingCheckState = null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PlaylistViewModel] 楽曲読み込みエラー: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OnTracksCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (TrackViewModel track in e.NewItems)
            {
                track.PropertyChanged += OnTrackPropertyChanged;
            }
        }
        
        if (e.OldItems != null)
        {
            foreach (TrackViewModel track in e.OldItems)
            {
                track.PropertyChanged -= OnTrackPropertyChanged;
            }
        }
    }

    private void OnTrackPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TrackViewModel.IsSelected))
        {
            UpdateIsCheckedFromChildren();
            OnSelectionChanged();
        }
    }

    private void UpdateIsCheckedFromChildren()
    {
        _isUpdatingFromChildren = true;
        
        var checkedCount = Tracks.Count(t => t.IsSelected);
        
        if (checkedCount == 0)
        {
            IsChecked = false;
        }
        else
        {
            // 楽曲が個別に選択された場合は常に中間状態を維持
            // プレイリストのON状態は、ユーザーが直接プレイリストをクリックした場合のみ
            IsChecked = null; // 中間状態
        }
        
        _isUpdatingFromChildren = false;
    }

    private void SetAllTracksChecked(bool isChecked)
    {
        foreach (var track in Tracks)
        {
            track.IsSelected = isChecked;
        }
    }

    public event EventHandler? SelectionChanged;

    protected virtual void OnSelectionChanged()
    {
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    public new void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
    }

    public void HandleTrackClick(TrackViewModel clickedTrack, bool isShiftPressed)
    {
        if (isShiftPressed && _lastSelectedTrack != null && _lastSelectedTrack != clickedTrack)
        {
            // 範囲選択
            var startIndex = Tracks.IndexOf(_lastSelectedTrack);
            var endIndex = Tracks.IndexOf(clickedTrack);
            
            if (startIndex >= 0 && endIndex >= 0)
            {
                var minIndex = Math.Min(startIndex, endIndex);
                var maxIndex = Math.Max(startIndex, endIndex);
                var targetState = _lastSelectedTrack.IsSelected;
                
                for (int i = minIndex; i <= maxIndex; i++)
                {
                    Tracks[i].IsSelected = targetState;
                }
            }
        }
        else
        {
            // 通常のクリック
            clickedTrack.IsSelected = !clickedTrack.IsSelected;
            _lastSelectedTrack = clickedTrack;
        }
    }

    private void ToggleCheck()
    {
        // ユーザーのクリック操作時は2状態のみ（中間状態をスキップ）
        if (IsChecked == true)
        {
            IsChecked = false;
        }
        else
        {
            IsChecked = true;
        }
    }
}