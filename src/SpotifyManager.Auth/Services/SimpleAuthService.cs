using SpotifyManager.Core.Interfaces;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Web;

namespace SpotifyManager.Auth.Services;

public class SimpleAuthService : IAuthService
{
    private readonly CredentialService _credentialService;
    private HttpListener? _listener;
    private string? _accessToken;

    public SimpleAuthService()
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
            Debug.WriteLine("認証プロセス開始");
            
            // 既存のリスナーがあれば停止
            if (_listener != null)
            {
                try
                {
                    _listener.Stop();
                    _listener.Close();
                }
                catch { }
                _listener = null;
            }
            
            // HTTPリスナーを作成
            _listener = new HttpListener();
            var listenerPrefix = Configuration.SpotifyAuthConfig.RedirectUri;
            if (!listenerPrefix.EndsWith("/"))
            {
                listenerPrefix += "/";
            }
            
            Debug.WriteLine($"リスナープレフィックス: {listenerPrefix}");
            
            try
            {
                _listener.Prefixes.Add(listenerPrefix);
                _listener.Start();
            }
            catch (HttpListenerException ex)
            {
                Debug.WriteLine($"HttpListenerエラー: {ex.Message}");
                Debug.WriteLine("管理者権限で実行するか、以下のコマンドを管理者として実行してください：");
                Debug.WriteLine($"netsh http add urlacl url={listenerPrefix} user=Everyone");
                throw new InvalidOperationException($"HTTPリスナーを開始できませんでした。管理者権限が必要な可能性があります。\n\nエラー: {ex.Message}", ex);
            }
            
            Debug.WriteLine($"HTTPリスナー開始: {Configuration.SpotifyAuthConfig.RedirectUri}");

            // 認証URLを作成
            var authUrl = $"https://accounts.spotify.com/authorize" +
                $"?client_id={Configuration.SpotifyAuthConfig.ClientId}" +
                $"&response_type=code" +
                $"&redirect_uri={HttpUtility.UrlEncode(Configuration.SpotifyAuthConfig.RedirectUri)}" +
                $"&scope={HttpUtility.UrlEncode(string.Join(" ", Configuration.SpotifyAuthConfig.Scopes))}";

            Debug.WriteLine($"認証URL: {authUrl}");

            // ブラウザで認証ページを開く
            Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });

            // コールバックを待つ
            var context = await _listener.GetContextAsync();
            var request = context.Request;
            var response = context.Response;

            // 認証コードを取得
            var code = HttpUtility.ParseQueryString(request.Url?.Query ?? "")?["code"];
            
            if (!string.IsNullOrEmpty(code))
            {
                Debug.WriteLine($"認証コード受信: {code}");
                
                // ブラウザに成功メッセージを返す
                string responseString = "<html><body>認証が完了しました。このウィンドウを閉じてアプリに戻ってください。</body></html>";
                byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.OutputStream.Close();

                // トークンを取得
                var success = await GetTokenAsync(code);
                
                StopListener();
                return success;
            }
            else
            {
                Debug.WriteLine("認証コードが取得できませんでした");
                
                // エラーメッセージを返す
                string responseString = "<html><body>認証に失敗しました。</body></html>";
                byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.OutputStream.Close();
                
                StopListener();
                return false;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ログインエラー: {ex}");
            StopListener();
            return false;
        }
    }

    private void StopListener()
    {
        if (_listener != null)
        {
            try
            {
                _listener.Stop();
                _listener.Close();
            }
            catch { }
            _listener = null;
        }
    }

    private async Task<bool> GetTokenAsync(string code)
    {
        try
        {
            using var client = new HttpClient();
            
            var tokenRequest = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("redirect_uri", Configuration.SpotifyAuthConfig.RedirectUri),
                new KeyValuePair<string, string>("client_id", Configuration.SpotifyAuthConfig.ClientId)
            });

            var response = await client.PostAsync("https://accounts.spotify.com/api/token", tokenRequest);
            var content = await response.Content.ReadAsStringAsync();
            
            Debug.WriteLine($"トークンレスポンス: {response.StatusCode}");
            
            if (response.IsSuccessStatusCode)
            {
                var tokenData = JsonSerializer.Deserialize<JsonElement>(content);
                
                _accessToken = tokenData.GetProperty("access_token").GetString();
                var refreshToken = tokenData.GetProperty("refresh_token").GetString();
                
                if (!string.IsNullOrEmpty(refreshToken))
                {
                    // リフレッシュトークンを保存
                    await _credentialService.SaveCredentialAsync(
                        Configuration.SpotifyAuthConfig.CredentialTargetName,
                        refreshToken
                    );
                    
                    // ユーザー情報を取得
                    await GetUserInfoAsync(_accessToken!);
                    
                    return true;
                }
            }
            
            Debug.WriteLine($"トークン取得エラー: {content}");
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"トークン取得エラー: {ex}");
            return false;
        }
    }

    private async Task GetUserInfoAsync(string accessToken)
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            
            var response = await client.GetAsync("https://api.spotify.com/v1/me");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var userData = JsonSerializer.Deserialize<JsonElement>(content);
                
                var userInfo = new
                {
                    Id = userData.GetProperty("id").GetString(),
                    DisplayName = userData.GetProperty("display_name").GetString(),
                    Email = userData.GetProperty("email").GetString()
                };
                
                await _credentialService.SaveCredentialAsync(
                    Configuration.SpotifyAuthConfig.UserInfoTargetName,
                    JsonSerializer.Serialize(userInfo)
                );
                
                Debug.WriteLine($"ユーザー情報保存: {userInfo.DisplayName}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ユーザー情報取得エラー: {ex}");
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            await _credentialService.DeleteCredentialAsync(Configuration.SpotifyAuthConfig.CredentialTargetName);
            await _credentialService.DeleteCredentialAsync(Configuration.SpotifyAuthConfig.UserInfoTargetName);
            _accessToken = null;
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
}