using CommunityToolkit.Mvvm.ComponentModel;
using SpotifyManager.Core.Interfaces;
using SpotifyManager.Core.Models;
using System;

namespace SpotifyManager.Wpf.ViewModels;

public partial class SearchResultViewModel : ObservableObject
{
    public SearchResult SearchResult { get; }
    public TrackInfo TrackInfo => SearchResult.TrackInfo;

    [ObservableProperty]
    private bool _isSelected;

    public event EventHandler? SelectionChanged;

    public SearchResultViewModel(SearchResult searchResult)
    {
        SearchResult = searchResult;
    }

    public string ArtistsText => string.Join(", ", SearchResult.TrackInfo.Artists);
    
    public string AlbumText => SearchResult.TrackInfo.AlbumName ?? string.Empty;
    
    public string PlaylistName => SearchResult.PlaylistName;
    
    public string PlaylistDisplayText => 
        SearchResult.PlaylistName == "Spotify検索結果" 
            ? "Spotify検索" 
            : $"プレイリスト: {SearchResult.PlaylistName}";

    partial void OnIsSelectedChanged(bool value)
    {
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }
}