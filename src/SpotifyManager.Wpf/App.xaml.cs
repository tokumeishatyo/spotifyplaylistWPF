using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SpotifyManager.Auth;
using SpotifyManager.Core.Interfaces;
using SpotifyManager.Playlist;
using SpotifyManager.Theme;
using SpotifyManager.Search;
using SpotifyManager.Wpf.ViewModels;
using SpotifyManager.Wpf.Views;
using System.Windows;

namespace SpotifyManager.Wpf;

public partial class App : Application
{
    private readonly IHost _host;

    public App()
    {
        // Global exception handling for better debugging
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Register services from DLLs
                services.AddAuthServices();
                services.AddPlaylistServices();
                services.AddThemeServices();
                services.AddSearchServices();

                // Register ViewModels
                services.AddTransient<LoginViewModel>();
                services.AddTransient<MainViewModel>();

                // Register Views
                services.AddTransient<LoginView>();
                services.AddTransient<MainView>();
                services.AddSingleton<MainWindow>();
            })
            .Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await _host.StartAsync();

        // 初期テーマを適用
        var themeService = _host.Services.GetRequiredService<IThemeService>();
        themeService.ApplyInitialTheme();

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        base.OnStartup(e);
    }


    protected override async void OnExit(ExitEventArgs e)
    {
        using (_host)
        {
            await _host.StopAsync();
        }
        
        base.OnExit(e);
    }

    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(
            $"An unhandled exception occurred:\n\n{e.Exception.Message}\n\nStack Trace:\n{e.Exception.StackTrace}",
            "Application Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;
        MessageBox.Show(
            $"A fatal unhandled exception occurred:\n\n{exception?.Message}\n\nStack Trace:\n{exception?.StackTrace}",
            "Fatal Application Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
}