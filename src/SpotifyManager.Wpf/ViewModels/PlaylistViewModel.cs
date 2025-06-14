using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SpotifyManager.Core.Interfaces;
using SpotifyManager.Core.Models;
using System.Collections.ObjectModel;
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

    public ObservableCollection<TrackViewModel> Tracks { get; } = new();
    
    public ICommand LoadTracksCommand { get; }
    public ICommand ToggleExpandCommand { get; }

    public PlaylistViewModel(PlaylistInfo playlistInfo, IPlaylistService playlistService)
    {
        _playlistInfo = playlistInfo;
        _playlistService = playlistService;
        
        LoadTracksCommand = new AsyncRelayCommand(LoadTracksAsync);
        ToggleExpandCommand = new RelayCommand(ToggleExpand);
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
                Tracks.Add(new TrackViewModel(track));
            }
            
            TracksLoaded = true;
            Console.WriteLine($"[PlaylistViewModel] 楽曲読み込み完了: {Tracks.Count}件");
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
}