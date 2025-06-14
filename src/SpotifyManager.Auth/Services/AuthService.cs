using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyManager.Core.Interfaces;
using System.Diagnostics;
using System.Text.Json;

namespace SpotifyManager.Auth.Services;

public class AuthService : IAuthService
{
    private readonly CredentialService _credentialService;
    private SpotifyClient? _spotify;
    private EmbedIOAuthServer? _server;
    private string? _verifier;

    public AuthService()
    {
        _credentialService = new CredentialService();
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        try
        {
            var refreshToken = await _credentialService.GetCredentialAsync(Configuration.SpotifyAuthConfig.CredentialTargetName);
            if (string.IsNullOrEmpty(refreshToken))
                return false;

            // リフレッシュトークンでアクセストークンを取得
            try
            {
                var tokenResponse = await new OAuthClient().RequestToken(
                    new AuthorizationCodeRefreshRequest(Configuration.SpotifyAuthConfig.ClientId, "", refreshToken)
                );

                _spotify = new SpotifyClient(tokenResponse.AccessToken);
                
                // ユーザー情報を更新
                var user = await _spotify.UserProfile.Current();
                var userInfo = new { user.Id, user.DisplayName, user.Email };
                await _credentialService.SaveCredentialAsync(
                    Configuration.SpotifyAuthConfig.UserInfoTargetName, 
                    JsonSerializer.Serialize(userInfo)
                );
                
                return true;
            }
            catch
            {
                // トークンが無効な場合は削除
                await _credentialService.DeleteCredentialAsync(Configuration.SpotifyAuthConfig.CredentialTargetName);
                await _credentialService.DeleteCredentialAsync(Configuration.SpotifyAuthConfig.UserInfoTargetName);
                return false;
            }
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
            Debug.WriteLine("認証プロセス開始");
            
            var tcs = new TaskCompletionSource<bool>();
            
            // EmbedIOAuthServerを使用
            _server = new EmbedIOAuthServer(new Uri(Configuration.SpotifyAuthConfig.RedirectUri), 5000);
            
            // PKCEを使用する場合
            (_verifier, var challenge) = PKCEUtil.GenerateCodes();
            
            await _server.Start();
            Debug.WriteLine($"認証サーバー開始: {_server.BaseUri}");

            _server.AuthorizationCodeReceived += async (sender, response) =>
            {
                Debug.WriteLine($"認証コード受信: {response.Code}");
                await _server.Stop();
                
                try
                {
                    var tokenResponse = await new OAuthClient().RequestToken(
                        new PKCETokenRequest(Configuration.SpotifyAuthConfig.ClientId, response.Code, _server.BaseUri, _verifier)
                    );

                    Debug.WriteLine("トークン取得成功");

                    // リフレッシュトークンを保存
                    await _credentialService.SaveCredentialAsync(
                        Configuration.SpotifyAuthConfig.CredentialTargetName, 
                        tokenResponse.RefreshToken
                    );

                    // SpotifyClientを初期化
                    _spotify = new SpotifyClient(tokenResponse.AccessToken);
                    
                    // ユーザー情報を取得して保存
                    var user = await _spotify.UserProfile.Current();
                    Debug.WriteLine($"ユーザー情報取得: {user.DisplayName}");
                    
                    var userInfo = new { user.Id, user.DisplayName, user.Email };
                    await _credentialService.SaveCredentialAsync(
                        Configuration.SpotifyAuthConfig.UserInfoTargetName, 
                        JsonSerializer.Serialize(userInfo)
                    );
                    
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"認証エラー詳細: {ex}");
                    tcs.SetResult(false);
                }
            };

            _server.ErrorReceived += async (sender, error, errorUri) =>
            {
                Debug.WriteLine($"認証エラー: {error}");
                Debug.WriteLine($"エラーURI: {errorUri}");
                await _server.Stop();
                tcs.TrySetResult(false);
            };
            
            var request = new LoginRequest(_server.BaseUri, Configuration.SpotifyAuthConfig.ClientId, LoginRequest.ResponseType.Code)
            {
                CodeChallengeMethod = "S256",
                CodeChallenge = challenge,
                Scope = Configuration.SpotifyAuthConfig.Scopes
            };

            var uri = request.ToUri();
            Debug.WriteLine($"認証URL: {uri}");
            
            BrowserUtil.Open(uri);

            return await tcs.Task;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ログインエラー: {ex}");
            return false;
        }
        finally
        {
            if (_server != null)
            {
                _server.Dispose();
                _server = null;
            }
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            // 保存されているトークンとユーザー情報を削除
            await _credentialService.DeleteCredentialAsync(Configuration.SpotifyAuthConfig.CredentialTargetName);
            await _credentialService.DeleteCredentialAsync(Configuration.SpotifyAuthConfig.UserInfoTargetName);
            
            // SpotifyClientをクリア
            _spotify = null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ログアウトエラー: {ex.Message}");
        }
    }

    public async Task<(string? UserId, string? DisplayName, string? Email)> GetUserInfoAsync()
    {
        try
        {
            var userInfoJson = await _credentialService.GetCredentialAsync(Configuration.SpotifyAuthConfig.UserInfoTargetName);
            if (string.IsNullOrEmpty(userInfoJson))
                return (null, null, null);

            var userInfo = JsonSerializer.Deserialize<JsonElement>(userInfoJson);
            
            return (
                userInfo.TryGetProperty("Id", out var id) ? id.GetString() : null,
                userInfo.TryGetProperty("DisplayName", out var displayName) ? displayName.GetString() : null,
                userInfo.TryGetProperty("Email", out var email) ? email.GetString() : null
            );
        }
        catch
        {
            return (null, null, null);
        }
    }

    public SpotifyClient? GetSpotifyClient()
    {
        return _spotify;
    }
}