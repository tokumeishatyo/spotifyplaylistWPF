using CommunityToolkit.Mvvm.ComponentModel;

namespace SpotifyManager.Wpf.ViewModels;

public partial class CreatePlaylistDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private string _playlistName = string.Empty;

    [ObservableProperty]
    private string _trackCountText;

    [ObservableProperty]
    private bool _canCreate;

    public CreatePlaylistDialogViewModel(int trackCount)
    {
        TrackCountText = $"{trackCount}件の楽曲を追加します";
        UpdateCanCreate();
    }

    partial void OnPlaylistNameChanged(string value)
    {
        UpdateCanCreate();
    }

    private void UpdateCanCreate()
    {
        CanCreate = !string.IsNullOrWhiteSpace(PlaylistName);
    }
}