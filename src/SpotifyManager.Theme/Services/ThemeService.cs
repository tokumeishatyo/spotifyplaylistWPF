using SpotifyManager.Core.Interfaces;
using System.IO;
using System.Windows;

namespace SpotifyManager.Theme.Services;

public class ThemeService : IThemeService
{
    private const string SettingsFileName = "theme.settings";
    private static readonly string SettingsFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SpotifyManager",
        SettingsFileName);

    private string _currentTheme = "Light";
    public event EventHandler<string>? ThemeChanged;

    public ThemeService()
    {
        // 設定フォルダが存在しない場合は作成
        var settingsDir = Path.GetDirectoryName(SettingsFilePath);
        if (!string.IsNullOrEmpty(settingsDir) && !Directory.Exists(settingsDir))
        {
            Directory.CreateDirectory(settingsDir);
        }

        // 保存されたテーマ設定を読み込み
        LoadThemeFromSettings();
    }

    public Task<string> GetCurrentThemeAsync()
    {
        return Task.FromResult(_currentTheme);
    }

    public async Task SetThemeAsync(string themeName)
    {
        if (_currentTheme == themeName)
            return;

        _currentTheme = themeName;
        
        // テーマリソースを適用
        ApplyThemeResources(themeName);
        
        // 設定を永続化
        await SaveThemeToSettingsAsync(themeName);
        
        // イベント通知
        ThemeChanged?.Invoke(this, themeName);
        
        Console.WriteLine($"[ThemeService] テーマ切替: {themeName}");
    }

    private void LoadThemeFromSettings()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                var theme = File.ReadAllText(SettingsFilePath).Trim();
                if (!string.IsNullOrEmpty(theme) && (theme == "Light" || theme == "Dark"))
                {
                    _currentTheme = theme;
                    Console.WriteLine($"[ThemeService] 保存されたテーマを読み込み: {_currentTheme}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ThemeService] テーマ設定読み込みエラー: {ex.Message}");
            _currentTheme = "Light"; // デフォルトにフォールバック
        }
    }

    private async Task SaveThemeToSettingsAsync(string themeName)
    {
        try
        {
            await File.WriteAllTextAsync(SettingsFilePath, themeName);
            Console.WriteLine($"[ThemeService] テーマ設定保存: {themeName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ThemeService] テーマ設定保存エラー: {ex.Message}");
        }
    }

    private void ApplyThemeResources(string themeName)
    {
        try
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var app = Application.Current;
                if (app?.Resources == null) return;

                // 既存のテーマリソースをクリア
                var resourcesToRemove = app.Resources.MergedDictionaries
                    .Where(rd => rd.Source?.OriginalString?.Contains("Themes/") == true)
                    .ToList();

                foreach (var resource in resourcesToRemove)
                {
                    app.Resources.MergedDictionaries.Remove(resource);
                }

                // 新しいテーマリソースを追加
                var themeUri = new Uri($"pack://application:,,,/SpotifyManager.Theme;component/Themes/{themeName}Theme.xaml");
                var themeResource = new ResourceDictionary { Source = themeUri };
                app.Resources.MergedDictionaries.Add(themeResource);

                Console.WriteLine($"[ThemeService] テーマリソース適用: {themeName}");
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ThemeService] テーマリソース適用エラー: {ex.Message}");
        }
    }

    public void ApplyInitialTheme()
    {
        ApplyThemeResources(_currentTheme);
    }
}