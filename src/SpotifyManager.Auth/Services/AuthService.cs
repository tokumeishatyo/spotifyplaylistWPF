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

    private void LogDebug(string message)
    {
        Debug.WriteLine(message);
        Console.WriteLine($"[DEBUG] {message}");
    }

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
        Console.WriteLine("💫💫💫 AuthService.LoginAsync() メソッドが呼ばれました 💫💫💫");
        try
        {
            LogDebug("認証プロセス開始");
            LogDebug($"リダイレクトURI: {Configuration.SpotifyAuthConfig.RedirectUri}");
            
            var tcs = new TaskCompletionSource<bool>();
            
            // EmbedIOAuthServerを使用
            LogDebug("EmbedIOAuthServerを作成中...");
            _server = new EmbedIOAuthServer(new Uri("http://127.0.0.1:5000/"), 5000);
            LogDebug("EmbedIOAuthServer作成完了");
            
            // イベントハンドラーをサーバー開始前に登録
            LogDebug("認証コードイベントハンドラー登録中...");
            _server.AuthorizationCodeReceived += async (sender, response) =>
            {
                LogDebug($"★★★ 認証コード受信: {response.Code} ★★★");
                LogDebug($"受信したState: {response.State}");
                LogDebug("認証サーバー停止中...");
                await _server.Stop();
                LogDebug("認証サーバー停止完了");
                
                try
                {
                    var tokenResponse = await new OAuthClient().RequestToken(
                        new PKCETokenRequest(Configuration.SpotifyAuthConfig.ClientId, response.Code, _server.BaseUri, _verifier!)
                    );

                    LogDebug("トークン取得成功");

                    // リフレッシュトークンを保存
                    await _credentialService.SaveCredentialAsync(
                        Configuration.SpotifyAuthConfig.CredentialTargetName, 
                        tokenResponse.RefreshToken
                    );

                    // SpotifyClientを初期化
                    _spotify = new SpotifyClient(tokenResponse.AccessToken);
                    
                    // ユーザー情報を取得して保存
                    var user = await _spotify.UserProfile.Current();
                    LogDebug($"ユーザー情報取得: {user.DisplayName}");
                    
                    var userInfo = new { user.Id, user.DisplayName, user.Email };
                    await _credentialService.SaveCredentialAsync(
                        Configuration.SpotifyAuthConfig.UserInfoTargetName, 
                        JsonSerializer.Serialize(userInfo)
                    );
                    
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    LogDebug($"認証エラー詳細: {ex}");
                    tcs.SetResult(false);
                }
            };

            LogDebug("エラーイベントハンドラー登録中...");
            _server.ErrorReceived += async (sender, error, errorUri) =>
            {
                LogDebug($"★★★ 認証エラー: {error} ★★★");
                LogDebug($"★★★ エラーURI: {errorUri} ★★★");
                await _server.Stop();
                tcs.TrySetResult(false);
            };
            LogDebug("イベントハンドラー登録完了");
            
            // PKCEを使用する場合
            LogDebug("PKCE コード生成中...");
            (_verifier, var challenge) = PKCEUtil.GenerateCodes();
            LogDebug($"PKCE コード生成完了: verifier長={_verifier?.Length}, challenge長={challenge?.Length}");
            
            LogDebug("認証サーバー開始中...");
            await _server.Start();
            LogDebug($"認証サーバー開始完了: {_server.BaseUri}");
            LogDebug($"サーバーポート: {_server.Port}");
            LogDebug($"サーバー実際のURI: {_server.BaseUri}");
            
            // HTTPリクエスト受信の監視を追加
            LogDebug("HTTPリクエスト監視を開始しています...");
            
            // EmbedIOAuthServerが自動的に作成するコールバックURLを使用
            var callbackUri = new Uri(_server.BaseUri, "callback");
            LogDebug($"実際のコールバックURI: {callbackUri}");
            
            var request = new LoginRequest(callbackUri, Configuration.SpotifyAuthConfig.ClientId, LoginRequest.ResponseType.Code)
            {
                CodeChallengeMethod = "S256",
                CodeChallenge = challenge,
                Scope = Configuration.SpotifyAuthConfig.Scopes
            };

            var uri = request.ToUri();
            LogDebug($"認証URL: {uri}");
            
            LogDebug("ブラウザを開いています...");
            BrowserUtil.Open(uri);
            LogDebug("ブラウザを開きました");

            LogDebug("コールバック待機中...");
            
            // 60秒のタイムアウトを追加
            var timeoutTask = Task.Delay(60000);
            var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);
            
            if (completedTask == timeoutTask)
            {
                LogDebug("★★★ コールバックタイムアウト (60秒) ★★★");
                return false;
            }
            
            var result = await tcs.Task;
            LogDebug($"コールバック完了: {result}");
            return result;
        }
        catch (Exception ex)
        {
            LogDebug($"ログインエラー: {ex}");
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
            LogDebug($"ログアウトエラー: {ex.Message}");
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