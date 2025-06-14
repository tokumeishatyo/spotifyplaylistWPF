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
        try
        {
            var success = await _authService.LoginAsync();
            if (success)
            {
                LoginSucceeded?.Invoke(this, EventArgs.Empty);
            }
        }
        finally
        {
            IsLoggingIn = false;
        }
    }
}