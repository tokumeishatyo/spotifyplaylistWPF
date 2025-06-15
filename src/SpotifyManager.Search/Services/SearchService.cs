using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpotifyAPI.Web;
using SpotifyManager.Core.Interfaces;
using SpotifyManager.Core.Models;

namespace SpotifyManager.Search.Services;

public class SearchService : Core.Interfaces.ISearchService
{
    private readonly ISpotifyApi _spotifyApi;
    private readonly IPlaylistService _playlistService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SearchService> _logger;
    
    private List<SearchResult> _cachedTracks = new();
    private Dictionary<string, List<string>> _moodMappings = new();

    public SearchService(
        ISpotifyApi spotifyApi,
        IPlaylistService playlistService,
        IConfiguration configuration,
        ILogger<SearchService> logger)
    {
        _spotifyApi = spotifyApi;
        _playlistService = playlistService;
        _configuration = configuration;
        _logger = logger;
        
        LoadMoodMappings();
    }

    public async Task InitializeCacheAsync()
    {
        try
        {
            _logger.LogInformation("[SearchService] キャッシュ初期化開始");
            
            var playlists = await _playlistService.GetPlaylistsAsync();
            _cachedTracks.Clear();
            
            foreach (var playlist in playlists)
            {
                var tracks = await _playlistService.GetTracksAsync(playlist.Id);
                foreach (var track in tracks)
                {
                    _cachedTracks.Add(new SearchResult
                    {
                        TrackInfo = track,
                        PlaylistName = playlist.Name,
                        PlaylistId = playlist.Id
                    });
                }
            }
            
            _logger.LogInformation($"[SearchService] キャッシュ初期化完了: {_cachedTracks.Count}件");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SearchService] キャッシュ初期化エラー");
        }
    }

    public async Task<IEnumerable<SearchResult>> SearchAsync(SearchRequest request)
    {
        try
        {
            _logger.LogInformation($"[SearchService] 検索開始: Mode={request.Mode}, MaxResults={request.MaxResults}");
            
            if (request.Mode == SearchMode.Keyword)
            {
                return await SearchByKeywordAsync(request);
            }
            else
            {
                return await SearchByMoodAsync(request);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SearchService] 検索エラー");
            return Enumerable.Empty<SearchResult>();
        }
    }

    public IEnumerable<string> GetAvailableMoods()
    {
        return _moodMappings.Keys;
    }

    private async Task<IEnumerable<SearchResult>> SearchByKeywordAsync(SearchRequest request)
    {
        var results = _cachedTracks.AsEnumerable();
        
        if (!string.IsNullOrWhiteSpace(request.TrackName))
        {
            results = results.Where(r => r.TrackInfo.Name.Contains(request.TrackName, StringComparison.OrdinalIgnoreCase));
        }
        
        if (!string.IsNullOrWhiteSpace(request.ArtistName))
        {
            results = results.Where(r => r.TrackInfo.Artists.Any(a => a.Contains(request.ArtistName, StringComparison.OrdinalIgnoreCase)));
        }
        
        if (!string.IsNullOrWhiteSpace(request.AlbumName))
        {
            results = results.Where(r => r.TrackInfo.AlbumName.Contains(request.AlbumName, StringComparison.OrdinalIgnoreCase));
        }
        
        return results.Take(request.MaxResults);
    }

    private async Task<IEnumerable<SearchResult>> SearchByMoodAsync(SearchRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Mood) || !_moodMappings.ContainsKey(request.Mood))
        {
            return Enumerable.Empty<Core.Interfaces.SearchResult>();
        }
        
        var keywords = _moodMappings[request.Mood];
        var results = new List<SearchResult>();
        
        foreach (var keyword in keywords)
        {
            var matches = _cachedTracks.Where(r => 
                r.TrackInfo.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                r.TrackInfo.AlbumName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
            );
            
            results.AddRange(matches);
        }
        
        // 重複除去
        return results.DistinctBy(r => r.TrackInfo.Id).Take(request.MaxResults);
    }

    private void LoadMoodMappings()
    {
        try
        {
            var moodSection = _configuration.GetSection("MoodMappings");
            if (moodSection.Exists())
            {
                foreach (var mood in moodSection.GetChildren())
                {
                    var keywords = mood.GetChildren().Select(k => k.Value ?? string.Empty).ToList();
                    _moodMappings[mood.Key] = keywords;
                }
            }
            else
            {
                // デフォルトの気分マッピング
                SetDefaultMoodMappings();
            }
            
            _logger.LogInformation($"[SearchService] 気分マッピング読み込み完了: {_moodMappings.Count}種類");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SearchService] 気分マッピング読み込みエラー");
            SetDefaultMoodMappings();
        }
    }

    private void SetDefaultMoodMappings()
    {
        _moodMappings = new Dictionary<string, List<string>>
        {
            ["アップテンポ"] = new() { "upbeat", "dance", "pop", "party", "happy" },
            ["リラックス"] = new() { "chill", "acoustic", "lo-fi", "relax", "ambient", "instrumental" },
            ["集中したい時"] = new() { "focus", "study", "instrumental", "classical", "ambient" },
            ["パーティー"] = new() { "party", "dance", "edm", "remix", "pop" },
            ["切ない気分"] = new() { "sad", "ballad", "mellow", "rainy day" }
        };
    }
}