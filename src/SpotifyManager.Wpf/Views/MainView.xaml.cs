using SpotifyManager.Wpf.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SpotifyManager.Wpf.Views;

public partial class MainView : UserControl
{
    public MainView(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void OnTrackCheckBoxPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is CheckBox checkBox && checkBox.DataContext is TrackViewModel track)
        {
            // 親PlaylistViewModelを探す
            DependencyObject parent = checkBox;
            PlaylistViewModel? playlist = null;
            
            while (parent != null && playlist == null)
            {
                parent = System.Windows.Media.VisualTreeHelper.GetParent(parent);
                if (parent is FrameworkElement fe && fe.DataContext is PlaylistViewModel pvm)
                {
                    playlist = pvm;
                }
            }
            
            if (playlist != null)
            {
                e.Handled = true; // デフォルトのチェックボックス動作を防ぐ
                playlist.HandleTrackClick(track, Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift));
            }
        }
    }

    private void OnPlaylistCheckBoxPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is CheckBox checkBox && checkBox.DataContext is PlaylistViewModel playlist)
        {
            e.Handled = true; // デフォルトのチェックボックス動作を防ぐ
            playlist.ToggleCheckCommand.Execute(null);
        }
    }

    private void OnSearchResultCheckBoxPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.OnSearchResultCheckBoxPreviewMouseLeftButtonDown(sender, e);
        }
    }

}