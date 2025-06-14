using Microsoft.Extensions.DependencyInjection;
using SpotifyManager.Theme.Services;
using SpotifyManager.Core.Interfaces;

namespace SpotifyManager.Theme;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddThemeServices(this IServiceCollection services)
    {
        services.AddSingleton<IThemeService, ThemeService>();
        return services;
    }
}