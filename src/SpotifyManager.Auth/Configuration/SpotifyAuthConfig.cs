namespace SpotifyManager.Auth.Configuration;

public static class SpotifyAuthConfig
{
    // Client IDは難読化して保存（実際のアプリではここに正しいClient IDを設定）
    private static readonly string[] ClientIdParts = new[]
    {
        "c92f0b846a9f40bf",
        "95b487d2cbed045e"
    };

    public static string ClientId => string.Concat(ClientIdParts);
    
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