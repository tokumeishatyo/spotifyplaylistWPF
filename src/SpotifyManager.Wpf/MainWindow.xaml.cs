using Microsoft.Extensions.DependencyInjection;
using SpotifyManager.Core.Interfaces;
using SpotifyManager.Wpf.Views;
using System.Windows;

namespace SpotifyManager.Wpf;

public partial class MainWindow : Window
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IAuthService _authService;

    public MainWindow(IServiceProvider serviceProvider, IAuthService authService)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
        _authService = authService;
        
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await CheckAndNavigateAsync();
    }

    private async Task CheckAndNavigateAsync()
    {
        var isAuthenticated = await _authService.IsAuthenticatedAsync();
        
        if (isAuthenticated)
        {
            ShowMainView();
        }
        else
        {
            ShowLoginView();
        }
    }

    private void ShowLoginView()
    {
        var loginView = _serviceProvider.GetRequiredService<LoginView>();
        
        // LoginViewModelのイベントを購読
        if (loginView.DataContext is ViewModels.LoginViewModel loginViewModel)
        {
            loginViewModel.LoginSucceeded += OnLoginSucceeded;
        }
        
        MainContent.Content = loginView;
    }

    private void ShowMainView()
    {
        var mainView = _serviceProvider.GetRequiredService<MainView>();
        
        // MainViewModelのイベントを購読
        if (mainView.DataContext is ViewModels.MainViewModel mainViewModel)
        {
            mainViewModel.LogoutRequested += OnLogoutRequested;
        }
        
        MainContent.Content = mainView;
    }

    private void OnLoginSucceeded(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() => ShowMainView());
    }

    private void OnLogoutRequested(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() => ShowLoginView());
    }
}