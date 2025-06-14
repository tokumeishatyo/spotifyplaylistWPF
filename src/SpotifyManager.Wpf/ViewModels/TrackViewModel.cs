using CommunityToolkit.Mvvm.ComponentModel;
using SpotifyManager.Core.Models;

namespace SpotifyManager.Wpf.ViewModels;

public partial class TrackViewModel : ObservableObject
{
    [ObservableProperty]
    private TrackInfo _trackInfo;
    
    [ObservableProperty]
    private bool _isSelected;

    public TrackViewModel(TrackInfo trackInfo)
    {
        _trackInfo = trackInfo;
    }
    
    public string DisplayName => TrackInfo.DisplayName;
    public string Artists => TrackInfo.ArtistsString;
    public string AlbumName => TrackInfo.AlbumName ?? "";
    public TimeSpan Duration => TimeSpan.FromMilliseconds(TrackInfo.DurationMs);
    public string DurationString => Duration.ToString(@"mm\:ss");
}