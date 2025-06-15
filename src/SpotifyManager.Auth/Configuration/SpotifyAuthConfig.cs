using System.Text;

namespace SpotifyManager.Auth.Configuration;

public static class SpotifyAuthConfig
{
    // アプリケーション設定（エンコード済み）
    private static readonly string[] AppConfig = new[]
    {
        "YzkyZjBiODQ2YTlmNDBiZjk1YjQ4N2QyY2JlZDA0NWU="
    };

    public static string ClientId => GetAppIdentifier();
    
    private static string GetAppIdentifier()
    {
        try
        {
            var encoded = string.Concat(AppConfig);
            var decoded = Convert.FromBase64String(encoded);
            return Encoding.UTF8.GetString(decoded);
        }
        catch
        {
            throw new InvalidOperationException("Application configuration error");
        }
    }
    
    public const string RedirectUri = "http://127.0.0.1:8888/callback";
    
    public static readonly string[] Scopes = new[]
    {
        "playlist-read-private",
        "playlist-read-collaborative", 
        "playlist-modify-public",
        "playlist-modify-private",
        "user-read-private",
        "user-read-email"
    };
    
    public const string CredentialTargetName = "SpotifyManager_RefreshToken";
    public const string UserInfoTargetName = "SpotifyManager_UserInfo";
}