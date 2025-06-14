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
        IsLoggingIn = true;
        HasError = false;
        ErrorMessage = string.Empty;
        
        try
        {
            System.Diagnostics.Debug.WriteLine("ログイン開始");
            var success = await _authService.LoginAsync();
            System.Diagnostics.Debug.WriteLine($"ログイン結果: {success}");
            
            if (success)
            {
                LoginSucceeded?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                HasError = true;
                ErrorMessage = "ログインに失敗しました。コンソールのデバッグメッセージを確認してください。";
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