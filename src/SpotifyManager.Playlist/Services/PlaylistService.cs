using SpotifyAPI.Web;
using SpotifyManager.Core.Interfaces;
using SpotifyManager.Core.Models;
using System.Diagnostics;

namespace SpotifyManager.Playlist.Services;

public class PlaylistService : IPlaylistService
{
    private readonly IAuthService _authService;

    public PlaylistService(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<IEnumerable<PlaylistInfo>> GetPlaylistsAsync()
    {
        try
        {
            Console.WriteLine("[Playlist] プレイリスト取得開始");
            var spotify = GetSpotifyClient();
            if (spotify == null)
            {
                Console.WriteLine("[Playlist] SpotifyClientが取得できませんでした");
                return Enumerable.Empty<PlaylistInfo>();
            }

            var playlists = new List<PlaylistInfo>();
            var currentUser = await spotify.UserProfile.Current();
            
            Console.WriteLine($"[Playlist] ユーザー: {currentUser.DisplayName}");

            // ページングでプレイリストを取得（パフォーマンス考慮）
            var request = new PlaylistGetUsersRequest { Limit = 50 };
            var playlistsPage = await spotify.Playlists.GetUsers(currentUser.Id, request);

            await foreach (var playlist in spotify.Paginate(playlistsPage))
            {
                var playlistInfo = new PlaylistInfo
                {
                    Id = playlist.Id!,
                    Name = playlist.Name!,
                    Description = playlist.Description,
                    ImageUrl = playlist.Images?.FirstOrDefault()?.Url,
                    TrackCount = playlist.Tracks?.Total ?? 0,
                    IsOwner = playlist.Owner?.Id == currentUser.Id,
                    CanEdit = playlist.Owner?.Id == currentUser.Id || (playlist.Collaborative ?? false),
                    IsPublic = playlist.Public ?? false
                };

                playlists.Add(playlistInfo);
                Console.WriteLine($"[Playlist] 取得: {playlistInfo.Name} ({playlistInfo.TrackCount} tracks)");
            }

            Console.WriteLine($"[Playlist] プレイリスト取得完了: {playlists.Count}件");
            return playlists;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Playlist] エラー: {ex.Message}");
            Debug.WriteLine($"プレイリスト取得エラー: {ex}");
            return Enumerable.Empty<PlaylistInfo>();
        }
    }

    public async Task<IEnumerable<TrackInfo>> GetTracksAsync(string playlistId)
    {
        try
        {
            Console.WriteLine($"[Playlist] 楽曲取得開始: {playlistId}");
            var spotify = GetSpotifyClient();
            if (spotify == null)
            {
                Console.WriteLine("[Playlist] SpotifyClientが取得できませんでした");
                return Enumerable.Empty<TrackInfo>();
            }

            var tracks = new List<TrackInfo>();

            // ページングで楽曲を取得（パフォーマンス考慮）
            var request = new PlaylistGetItemsRequest { Limit = 100 };
            var tracksPage = await spotify.Playlists.GetItems(playlistId, request);

            await foreach (var item in spotify.Paginate(tracksPage))
            {
                if (item.Track is FullTrack track)
                {
                    var trackInfo = new TrackInfo
                    {
                        Id = track.Id,
                        Name = track.Name,
                        Uri = track.Uri,
                        PlaylistId = playlistId,
                        Artists = track.Artists.Select(a => a.Name).ToList(),
                        AlbumName = track.Album?.Name,
                        AlbumImageUrl = track.Album?.Images?.FirstOrDefault()?.Url,
                        DurationMs = track.DurationMs,
                        IsLocal = false
                    };

                    tracks.Add(trackInfo);
                }
                else if (item.Track is FullEpisode episode)
                {
                    // ポッドキャストエピソードの場合
                    var trackInfo = new TrackInfo
                    {
                        Id = episode.Id,
                        Name = episode.Name,
                        Uri = episode.Uri,
                        PlaylistId = playlistId,
                        Artists = new List<string> { episode.Show?.Name ?? "Podcast" },
                        AlbumName = episode.Show?.Name,
                        AlbumImageUrl = episode.Images?.FirstOrDefault()?.Url,
                        DurationMs = episode.DurationMs,
                        IsLocal = false
                    };

                    tracks.Add(trackInfo);
                }
            }

            Console.WriteLine($"[Playlist] 楽曲取得完了: {tracks.Count}件");
            return tracks;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Playlist] 楽曲取得エラー: {ex.Message}");
            Debug.WriteLine($"楽曲取得エラー: {ex}");
            return Enumerable.Empty<TrackInfo>();
        }
    }

    public async Task DeletePlaylistAsync(string playlistId)
    {
        try
        {
            Console.WriteLine($"[Playlist] プレイリスト削除開始: {playlistId}");
            var spotify = GetSpotifyClient();
            if (spotify == null)
            {
                Console.WriteLine("[Playlist] SpotifyClientが取得できませんでした");
                return;
            }

            await spotify.Follow.UnfollowPlaylist(playlistId);
            Console.WriteLine($"[Playlist] プレイリスト削除完了: {playlistId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Playlist] プレイリスト削除エラー: {ex.Message}");
            Debug.WriteLine($"プレイリスト削除エラー: {ex}");
            throw;
        }
    }

    public async Task DeleteTracksAsync(string playlistId, IEnumerable<string> trackUris)
    {
        try
        {
            Console.WriteLine($"[Playlist] 楽曲削除開始: {playlistId}, {trackUris.Count()}件");
            var spotify = GetSpotifyClient();
            if (spotify == null)
            {
                Console.WriteLine("[Playlist] SpotifyClientが取得できませんでした");
                return;
            }

            var request = new PlaylistRemoveItemsRequest
            {
                Tracks = trackUris.Select(uri => new PlaylistRemoveItemsRequest.Item { Uri = uri }).ToList()
            };

            await spotify.Playlists.RemoveItems(playlistId, request);
            Console.WriteLine($"[Playlist] 楽曲削除完了: {trackUris.Count()}件");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Playlist] 楽曲削除エラー: {ex.Message}");
            Debug.WriteLine($"楽曲削除エラー: {ex}");
            throw;
        }
    }

    public async Task<IEnumerable<TrackInfo>> SearchTracksAsync(string query, int limit = 20)
    {
        try
        {
            Console.WriteLine($"[Playlist] 楽曲検索開始: {query}");
            var spotify = GetSpotifyClient();
            if (spotify == null)
            {
                Console.WriteLine("[Playlist] SpotifyClientが取得できませんでした");
                return Enumerable.Empty<TrackInfo>();
            }

            var searchRequest = new SpotifyAPI.Web.SearchRequest(SpotifyAPI.Web.SearchRequest.Types.Track, query)
            {
                Limit = limit
            };

            var searchResult = await spotify.Search.Item(searchRequest);
            var tracks = new List<TrackInfo>();

            if (searchResult.Tracks?.Items != null)
            {
                foreach (var track in searchResult.Tracks.Items)
                {
                    var trackInfo = new TrackInfo
                    {
                        Id = track.Id,
                        Name = track.Name,
                        Uri = track.Uri,
                        PlaylistId = string.Empty, // 検索結果なのでプレイリストIDはなし
                        Artists = track.Artists.Select(a => a.Name).ToList(),
                        AlbumName = track.Album?.Name,
                        AlbumImageUrl = track.Album?.Images?.FirstOrDefault()?.Url,
                        DurationMs = track.DurationMs,
                        IsLocal = false
                    };

                    tracks.Add(trackInfo);
                }
            }

            Console.WriteLine($"[Playlist] 楽曲検索完了: {tracks.Count}件");
            return tracks;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Playlist] 楽曲検索エラー: {ex.Message}");
            Debug.WriteLine($"楽曲検索エラー: {ex}");
            return Enumerable.Empty<TrackInfo>();
        }
    }

    private SpotifyClient? GetSpotifyClient()
    {
        return _authService.GetSpotifyClient() as SpotifyClient;
    }
}