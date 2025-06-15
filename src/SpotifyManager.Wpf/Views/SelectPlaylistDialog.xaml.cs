using System.Windows;
using SpotifyManager.Wpf.ViewModels;

namespace SpotifyManager.Wpf.Views;

public partial class SelectPlaylistDialog : Window
{
    public SelectPlaylistDialogViewModel ViewModel { get; }
    
    public IEnumerable<PlaylistViewModel> SelectedPlaylists => ViewModel.SelectedPlaylists;

    public SelectPlaylistDialog(IEnumerable<PlaylistViewModel> allPlaylists, int trackCount)
    {
        InitializeComponent();
        ViewModel = new SelectPlaylistDialogViewModel(allPlaylists, trackCount);
        DataContext = ViewModel;
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void OnAddClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.HasSelectedPlaylists)
        {
            DialogResult = true;
            Close();
        }
    }
}