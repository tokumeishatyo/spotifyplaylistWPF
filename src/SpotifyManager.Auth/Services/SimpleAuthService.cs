using SpotifyManager.Core.Interfaces;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Web;
using System.Security.Cryptography;

namespace SpotifyManager.Auth.Services;

public class SimpleAuthService : IAuthService
{
    private readonly CredentialService _credentialService;
    private HttpListener? _listener;
    private string? _accessToken;
    private string? _codeVerifier;
    private string? _codeChallenge;

    public SimpleAuthService()
    {
        _credentialService = new CredentialService();
    }

    private void GeneratePKCECodes()
    {
        // PKCE code verifier生成 (43-128文字のランダム文字列)
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        _codeVerifier = Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        // PKCE code challenge生成 (SHA256ハッシュ)
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(_codeVerifier));
        _codeChallenge = Convert.ToBase64String(hash)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        Console.WriteLine($"[PKCE] Code verifier長: {_codeVerifier.Length}");
        Console.WriteLine($"[PKCE] Code challenge長: {_codeChallenge.Length}");
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
        Console.WriteLine("💫💫💫 SimpleAuthService.LoginAsync() メソッドが呼ばれました 💫💫💫");
        try
        {
            Console.WriteLine("[SimpleAuth] 認証プロセス開始");
            Debug.WriteLine("認証プロセス開始");
            
            // 既存のリスナーがあれば停止
            Console.WriteLine("[SimpleAuth] 既存のリスナー確認中...");
            if (_listener != null)
            {
                try
                {
                    Console.WriteLine("[SimpleAuth] 既存のリスナーを停止中...");
                    _listener.Stop();
                    _listener.Close();
                }
                catch { }
                _listener = null;
            }
            
            // HTTPリスナーを作成
            Console.WriteLine("[SimpleAuth] HTTPリスナーを作成中...");
            _listener = new HttpListener();
            var listenerPrefix = Configuration.SpotifyAuthConfig.RedirectUri;
            if (!listenerPrefix.EndsWith("/"))
            {
                listenerPrefix += "/";
            }
            
            Console.WriteLine($"[SimpleAuth] リスナープレフィックス: {listenerPrefix}");
            Debug.WriteLine($"リスナープレフィックス: {listenerPrefix}");
            
            try
            {
                Console.WriteLine("[SimpleAuth] リスナープレフィックス追加中...");
                _listener.Prefixes.Add(listenerPrefix);
                Console.WriteLine("[SimpleAuth] HTTPリスナー開始中...");
                _listener.Start();
                Console.WriteLine("[SimpleAuth] HTTPリスナー開始成功");
            }
            catch (HttpListenerException ex)
            {
                Console.WriteLine($"[SimpleAuth] ★★★ HttpListenerエラー: {ex.Message} ★★★");
                Console.WriteLine("[SimpleAuth] 管理者権限で実行するか、以下のコマンドを管理者として実行してください：");
                Console.WriteLine($"[SimpleAuth] netsh http add urlacl url={listenerPrefix} user=Everyone");
                Debug.WriteLine($"HttpListenerエラー: {ex.Message}");
                Debug.WriteLine("管理者権限で実行するか、以下のコマンドを管理者として実行してください：");
                Debug.WriteLine($"netsh http add urlacl url={listenerPrefix} user=Everyone");
                return false; // 例外を投げる代わりにfalseを返す
            }
            
            Console.WriteLine($"[SimpleAuth] HTTPリスナー開始完了: {Configuration.SpotifyAuthConfig.RedirectUri}");
            Debug.WriteLine($"HTTPリスナー開始: {Configuration.SpotifyAuthConfig.RedirectUri}");

            // PKCEコードを生成
            Console.WriteLine("[SimpleAuth] PKCEコード生成中...");
            GeneratePKCECodes();

            // 認証URLを作成（PKCEパラメータ付き）
            Console.WriteLine("[SimpleAuth] 認証URL生成中...");
            var authUrl = $"https://accounts.spotify.com/authorize" +
                $"?client_id={Configuration.SpotifyAuthConfig.ClientId}" +
                $"&response_type=code" +
                $"&redirect_uri={HttpUtility.UrlEncode(Configuration.SpotifyAuthConfig.RedirectUri)}" +
                $"&scope={HttpUtility.UrlEncode(string.Join(" ", Configuration.SpotifyAuthConfig.Scopes))}" +
                $"&code_challenge_method=S256" +
                $"&code_challenge={_codeChallenge}";

            Console.WriteLine($"[SimpleAuth] 認証URL: {authUrl}");
            Debug.WriteLine($"認証URL: {authUrl}");

            // ブラウザで認証ページを開く
            Console.WriteLine("[SimpleAuth] ブラウザを開いています...");
            Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });
            Console.WriteLine("[SimpleAuth] ブラウザを開きました");

            // コールバックを待つ
            Console.WriteLine("[SimpleAuth] コールバック待機中...");
            var context = await _listener.GetContextAsync();
            Console.WriteLine("[SimpleAuth] ★★★ コールバック受信! ★★★");
            var request = context.Request;
            var response = context.Response;
            Console.WriteLine($"[SimpleAuth] 受信URL: {request.Url}");

            // 認証コードを取得
            var code = HttpUtility.ParseQueryString(request.Url?.Query ?? "")?["code"];
            Console.WriteLine($"[SimpleAuth] 抽出されたコード: {code}");
            
            if (!string.IsNullOrEmpty(code))
            {
                Console.WriteLine($"[SimpleAuth] ★★★ 認証コード受信: {code} ★★★");
                Debug.WriteLine($"認証コード受信: {code}");
                
                // ブラウザに成功メッセージを返す（UTF-8 charset指定付き）
                string responseString = @"<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <title>認証完了</title>
    <style>
        body { font-family: Arial, sans-serif; text-align: center; padding: 50px; }
        .success { color: #28a745; font-size: 24px; }
        .message { color: #333; font-size: 16px; margin-top: 20px; }
    </style>
</head>
<body>
    <div class=""success"">✅ 認証が完了しました</div>
    <div class=""message"">このウィンドウを閉じてアプリに戻ってください。</div>
</body>
</html>";
                
                // Content-Typeヘッダーにcharsetを指定
                response.ContentType = "text/html; charset=UTF-8";
                byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.OutputStream.Close();

                // トークンを取得
                Console.WriteLine("[SimpleAuth] トークン取得開始...");
                var success = await GetTokenAsync(code);
                Console.WriteLine($"[SimpleAuth] トークン取得結果: {success}");
                
                StopListener();
                Console.WriteLine($"[SimpleAuth] ログイン最終結果: {success}");
                return success;
            }
            else
            {
                Console.WriteLine("[SimpleAuth] ★★★ 認証コード取得失敗 ★★★");
                Debug.WriteLine("認証コードが取得できませんでした");
                
                // エラーメッセージを返す（UTF-8 charset指定付き）
                string responseString = @"<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <title>認証エラー</title>
    <style>
        body { font-family: Arial, sans-serif; text-align: center; padding: 50px; }
        .error { color: #dc3545; font-size: 24px; }
        .message { color: #333; font-size: 16px; margin-top: 20px; }
    </style>
</head>
<body>
    <div class=""error"">❌ 認証に失敗しました</div>
    <div class=""message"">このウィンドウを閉じて、もう一度お試しください。</div>
</body>
</html>";
                
                response.ContentType = "text/html; charset=UTF-8";
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
            Console.WriteLine($"[SimpleAuth] ★★★ 一般的なログインエラー: {ex} ★★★");
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
        Console.WriteLine($"[GetToken] トークン取得開始 - コード長: {code.Length}");
        try
        {
            Console.WriteLine("[GetToken] HttpClientを作成中...");
            using var client = new HttpClient();
            
            Console.WriteLine("[GetToken] PKCEトークンリクエストを準備中...");
            var tokenRequest = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("redirect_uri", Configuration.SpotifyAuthConfig.RedirectUri),
                new KeyValuePair<string, string>("client_id", Configuration.SpotifyAuthConfig.ClientId),
                new KeyValuePair<string, string>("code_verifier", _codeVerifier!) // PKCEパラメータ
            });

            Console.WriteLine($"[GetToken] リダイレクトURI: {Configuration.SpotifyAuthConfig.RedirectUri}");
            Console.WriteLine($"[GetToken] クライアントID: {Configuration.SpotifyAuthConfig.ClientId}");
            
            Console.WriteLine("[GetToken] Spotify APIにPOSTリクエスト送信中...");
            var response = await client.PostAsync("https://accounts.spotify.com/api/token", tokenRequest);
            var content = await response.Content.ReadAsStringAsync();
            
            Console.WriteLine($"[GetToken] レスポンスステータス: {response.StatusCode}");
            Console.WriteLine($"[GetToken] レスポンス内容: {content}");
            Debug.WriteLine($"トークンレスポンス: {response.StatusCode}");
            
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("[GetToken] ★★★ トークン取得成功! ★★★");
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
                    Console.WriteLine("[GetToken] ユーザー情報取得開始...");
                    await GetUserInfoAsync(_accessToken!);
                    Console.WriteLine("[GetToken] ユーザー情報取得完了");
                    
                    return true;
                }
            }
            
            Console.WriteLine($"[GetToken] ★★★ トークン取得失敗 ★★★");
            Console.WriteLine($"[GetToken] エラー内容: {content}");
            Debug.WriteLine($"トークン取得エラー: {content}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GetToken] ★★★ 例外発生: {ex.Message} ★★★");
            Console.WriteLine($"[GetToken] 例外詳細: {ex}");
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