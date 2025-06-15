using SpotifyManager.Wpf.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace SpotifyManager.Wpf.Views;

public partial class CreatePlaylistDialog : Window
{
    public CreatePlaylistDialogViewModel ViewModel { get; }

    public CreatePlaylistDialog(int trackCount)
    {
        InitializeComponent();
        ViewModel = new CreatePlaylistDialogViewModel(trackCount);
        DataContext = ViewModel;
        
        // テキストボックスにフォーカスを設定
        Loaded += (s, e) => PlaylistNameTextBox.Focus();
    }

    private void OnCreateClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.CanCreate)
        {
            DialogResult = true;
            Close();
        }
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void OnPlaylistNameKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && ViewModel.CanCreate)
        {
            OnCreateClick(sender, new RoutedEventArgs());
        }
    }

    public string PlaylistName => ViewModel.PlaylistName;
}