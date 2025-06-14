using SpotifyManager.Core.Interfaces;

namespace SpotifyManager.Auth.Services;

public class AuthService : IAuthService
{
    private readonly CredentialService _credentialService;

    public AuthService()
    {
        _credentialService = new CredentialService();
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        try
        {
            var refreshToken = await _credentialService.GetCredentialAsync(Configuration.SpotifyAuthConfig.CredentialTargetName);
            return !string.IsNullOrEmpty(refreshToken);
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> LoginAsync()
    {
        try
        {
            // TODO: 実際のSpotify OAuth実装は後で行う
            // 今はテスト用にダミートークンを保存
            var dummyToken = "dummy_refresh_token_" + DateTime.Now.Ticks;
            await _credentialService.SaveCredentialAsync(
                Configuration.SpotifyAuthConfig.CredentialTargetName, 
                dummyToken
            );
            
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            await _credentialService.DeleteCredentialAsync(Configuration.SpotifyAuthConfig.CredentialTargetName);
        }
        catch
        {
            // エラーは無視
        }
    }
}