using Microsoft.Extensions.DependencyInjection;
using SpotifyManager.Core.Interfaces;
using SpotifyManager.Search.Services;

namespace SpotifyManager.Search;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSearchServices(this IServiceCollection services)
    {
        services.AddTransient<ISearchService, SearchService>();
        return services;
    }
}