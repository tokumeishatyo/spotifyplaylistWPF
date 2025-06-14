using SpotifyManager.Wpf.ViewModels;
using System.Windows.Controls;

namespace SpotifyManager.Wpf.Views;

public partial class MainView : UserControl
{
    public MainView(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}