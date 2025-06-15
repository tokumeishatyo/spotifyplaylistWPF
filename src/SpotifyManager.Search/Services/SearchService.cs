using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpotifyManager.Core.Interfaces;

namespace SpotifyManager.Search.Services;

public class SearchService : ISearchService
{
    private readonly IPlaylistService _playlistService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SearchService> _logger;
    
    private List<SearchResult> _cachedTracks = new();
    private Dictionary<string, List<string>> _moodMappings = new();

    public SearchService(
        IPlaylistService playlistService,
        IConfiguration configuration,
        ILogger<SearchService> logger)
    {
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

    public async Task<IEnumerable<SearchResult>> SearchAsync(Core.Interfaces.SearchRequest request)
    {
        try
        {
            _logger.LogInformation($"[SearchService] 検索開始: Mode={request.Mode}, MaxResults={request.MaxResults}");
            
            if (request.Mode == Core.Interfaces.SearchMode.Keyword)
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

    private async Task<IEnumerable<SearchResult>> SearchByKeywordAsync(Core.Interfaces.SearchRequest request)
    {
        var results = new List<SearchResult>();
        
        // まずプレイリストのキャッシュから検索
        var cachedResults = _cachedTracks.AsEnumerable();
        
        if (!string.IsNullOrWhiteSpace(request.TrackName))
        {
            cachedResults = cachedResults.Where(r => r.TrackInfo.Name.Contains(request.TrackName, StringComparison.OrdinalIgnoreCase));
        }
        
        if (!string.IsNullOrWhiteSpace(request.ArtistName))
        {
            cachedResults = cachedResults.Where(r => r.TrackInfo.Artists.Any(a => a.Contains(request.ArtistName, StringComparison.OrdinalIgnoreCase)));
        }
        
        if (!string.IsNullOrWhiteSpace(request.AlbumName))
        {
            cachedResults = cachedResults.Where(r => r.TrackInfo.AlbumName.Contains(request.AlbumName, StringComparison.OrdinalIgnoreCase));
        }
        
        results.AddRange(cachedResults);
        
        // Spotify Web APIからも検索（プレイリストにない曲も含む）
        try
        {
            var query = BuildSearchQuery(request);
            if (!string.IsNullOrWhiteSpace(query))
            {
                var spotifyTracks = await _playlistService.SearchTracksAsync(query, request.MaxResults);
                
                foreach (var track in spotifyTracks)
                {
                    // 既にプレイリストに含まれている曲は除外（重複防止）
                    if (!results.Any(r => r.TrackInfo.Id == track.Id))
                    {
                        results.Add(new SearchResult
                        {
                            TrackInfo = track,
                            PlaylistName = "Spotify検索結果",
                            PlaylistId = string.Empty
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SearchService] Spotify検索エラー");
        }
        
        return results.Take(request.MaxResults);
    }
    
    private string BuildSearchQuery(Core.Interfaces.SearchRequest request)
    {
        var queryParts = new List<string>();
        
        if (!string.IsNullOrWhiteSpace(request.TrackName))
        {
            queryParts.Add($"track:\"{request.TrackName}\"");
        }
        
        if (!string.IsNullOrWhiteSpace(request.ArtistName))
        {
            queryParts.Add($"artist:\"{request.ArtistName}\"");
        }
        
        if (!string.IsNullOrWhiteSpace(request.AlbumName))
        {
            queryParts.Add($"album:\"{request.AlbumName}\"");
        }
        
        return string.Join(" ", queryParts);
    }

    private async Task<IEnumerable<SearchResult>> SearchByMoodAsync(Core.Interfaces.SearchRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Mood) || !_moodMappings.ContainsKey(request.Mood))
        {
            return Enumerable.Empty<SearchResult>();
        }
        
        var keywords = _moodMappings[request.Mood];
        var results = new List<SearchResult>();
        
        try
        {
            // おまかせ検索はSpotify Web APIから直接検索（プレイリストにない楽曲を含む）
            foreach (var keyword in keywords)
            {
                _logger.LogInformation($"[SearchService] おまかせ検索: {keyword}");
                
                var spotifyTracks = await _playlistService.SearchTracksAsync(keyword, request.MaxResults / keywords.Count + 1);
                
                foreach (var track in spotifyTracks)
                {
                    // 重複チェック
                    if (!results.Any(r => r.TrackInfo.Id == track.Id))
                    {
                        results.Add(new SearchResult
                        {
                            TrackInfo = track,
                            PlaylistName = "Spotify検索結果",
                            PlaylistId = string.Empty
                        });
                    }
                }
            }
            
            // ランダムに並び替えて多様性を持たせる
            var random = new Random();
            results = results.OrderBy(x => random.Next()).ToList();
            
            _logger.LogInformation($"[SearchService] おまかせ検索完了: {results.Count}件");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SearchService] おまかせ検索エラー");
        }
        
        return results.Take(request.MaxResults);
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