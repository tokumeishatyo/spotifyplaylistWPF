using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SpotifyManager.Auth;
using SpotifyManager.Core.Interfaces;
using SpotifyManager.Playlist;
using SpotifyManager.Theme;
using SpotifyManager.Wpf.ViewModels;
using SpotifyManager.Wpf.Views;
using System.Windows;

namespace SpotifyManager.Wpf;

public partial class App : Application
{
    private readonly IHost _host;

    public App()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Register services from DLLs
                services.AddAuthServices();
                services.AddPlaylistServices();
                services.AddThemeServices();

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
        // デバッグ用にコンソールウィンドウを表示
        #if DEBUG
        AllocConsole();
        Console.WriteLine("=== Spotify Manager デバッグコンソール ===");
        Console.WriteLine("認証処理のデバッグ情報がここに表示されます");  
        Console.WriteLine("現在時刻: " + DateTime.Now);
        Console.WriteLine("コンソールウィンドウが正常に表示されています");
        #endif

        await _host.StartAsync();

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        base.OnStartup(e);
    }

    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern bool AllocConsole();

    protected override async void OnExit(ExitEventArgs e)
    {
        using (_host)
        {
            await _host.StopAsync();
        }
        
        base.OnExit(e);
    }
}