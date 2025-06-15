using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace SpotifyManager.Wpf.ViewModels;

public partial class SelectPlaylistDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private string _trackCountText = string.Empty;
    
    [ObservableProperty]
    private bool _hasSelectedPlaylists;

    public ObservableCollection<SelectablePlaylistViewModel> EditablePlaylists { get; } = new();
    
    public IEnumerable<PlaylistViewModel> SelectedPlaylists => 
        EditablePlaylists.Where(p => p.IsSelected).Select(p => p.PlaylistViewModel);

    public SelectPlaylistDialogViewModel(IEnumerable<PlaylistViewModel> allPlaylists, int trackCount)
    {
        TrackCountText = $"{trackCount}件の楽曲を追加します";
        
        // 編集可能なプレイリストのみをフィルタして追加
        var editablePlaylists = allPlaylists.Where(p => p.PlaylistInfo.CanEdit);
        
        foreach (var playlist in editablePlaylists)
        {
            var selectablePlaylist = new SelectablePlaylistViewModel(playlist);
            selectablePlaylist.PropertyChanged += OnPlaylistSelectionChanged;
            EditablePlaylists.Add(selectablePlaylist);
        }
        
        UpdateHasSelectedPlaylists();
    }
    
    private void OnPlaylistSelectionChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectablePlaylistViewModel.IsSelected))
        {
            UpdateHasSelectedPlaylists();
        }
    }
    
    private void UpdateHasSelectedPlaylists()
    {
        HasSelectedPlaylists = EditablePlaylists.Any(p => p.IsSelected);
    }
}

public partial class SelectablePlaylistViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isSelected;
    
    public PlaylistViewModel PlaylistViewModel { get; }
    public SpotifyManager.Core.Models.PlaylistInfo PlaylistInfo => PlaylistViewModel.PlaylistInfo;
    
    public SelectablePlaylistViewModel(PlaylistViewModel playlistViewModel)
    {
        PlaylistViewModel = playlistViewModel;
    }
}