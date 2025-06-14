namespace SpotifyManager.Auth.Configuration;

public static class SpotifyAuthConfig
{
    // Client IDは難読化して保存
    private static readonly string[] ClientIdParts = new[]
    {
        "YOUR_CLIENT_",
        "ID_HERE"
    };

    public static string ClientId => string.Concat(ClientIdParts);
    
    public const string RedirectUri = "http://localhost:5000/callback";
    
    public static readonly string[] Scopes = new[]
    {
        "playlist-read-private",
        "playlist-read-collaborative",
        "playlist-modify-public",
        "playlist-modify-private"
    };
    
    public const string CredentialTargetName = "SpotifyManager_RefreshToken";
}