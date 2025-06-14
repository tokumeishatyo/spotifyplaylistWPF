using Microsoft.Extensions.DependencyInjection;
using SpotifyManager.Playlist.Services;
using SpotifyManager.Core.Interfaces;

namespace SpotifyManager.Playlist;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPlaylistServices(this IServiceCollection services)
    {
        services.AddSingleton<IPlaylistService, PlaylistService>();
        return services;
    }
}