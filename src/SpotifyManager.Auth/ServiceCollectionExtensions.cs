using Microsoft.Extensions.DependencyInjection;
using SpotifyManager.Auth.Services;
using SpotifyManager.Core.Interfaces;

namespace SpotifyManager.Auth;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuthServices(this IServiceCollection services)
    {
        services.AddSingleton<IAuthService, AuthService>();
        return services;
    }
}