using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SpotifyManager.Core.Interfaces;
using System.Windows.Input;

namespace SpotifyManager.Wpf.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthService _authService;

    [ObservableProperty]
    private bool _isLoggingIn;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    public ICommand LoginCommand { get; }
    
    public event EventHandler? LoginSucceeded;

    public LoginViewModel(IAuthService authService)
    {
        _authService = authService;
        LoginCommand = new AsyncRelayCommand(LoginAsync);
    }

    private async Task LoginAsync()
    {
        Console.WriteLine("★★★★★ ログインボタンがクリックされました ★★★★★");
        IsLoggingIn = true;
        HasError = false;
        ErrorMessage = string.Empty;
        
        try
        {
            Console.WriteLine("★★★ LoginAsync開始 ★★★");
            System.Diagnostics.Debug.WriteLine("ログイン開始");
            Console.WriteLine("[LOGIN] ログイン開始");
            
            Console.WriteLine("★★★ AuthService.LoginAsync()呼び出し前 ★★★");
            var success = await _authService.LoginAsync();
            Console.WriteLine("★★★ AuthService.LoginAsync()呼び出し後 ★★★");
            
            System.Diagnostics.Debug.WriteLine($"ログイン結果: {success}");
            Console.WriteLine($"[LOGIN] ログイン結果: {success}");
            
            if (success)
            {
                Console.WriteLine("★★★ ログイン成功 - イベント発火 ★★★");
                LoginSucceeded?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Console.WriteLine("★★★ ログイン失敗 ★★★");
                HasError = true;
                ErrorMessage = "ログインに失敗しました。デバッグコンソールでエラー詳細を確認してください。";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ログイン例外: {ex}");
            HasError = true;
            ErrorMessage = $"ログインエラー: {ex.Message}";
        }
        finally
        {
            IsLoggingIn = false;
        }
    }
}