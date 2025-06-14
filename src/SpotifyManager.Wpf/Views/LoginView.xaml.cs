using SpotifyManager.Wpf.ViewModels;
using System.Windows.Controls;

namespace SpotifyManager.Wpf.Views;

public partial class LoginView : UserControl
{
    public LoginView(LoginViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}