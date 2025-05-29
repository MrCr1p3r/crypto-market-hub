using SVC_Bridge.Services;
using SVC_Bridge.Services.Interfaces;

namespace SVC_Bridge.Infrastructure.DependencyInjection;

/// <summary>
/// Extension methods for configuring application services.
/// </summary>
public static class ServiceExtensions
{
    /// <summary>
    /// Adds application service implementations to the DI container.
    /// </summary>
    /// <param name="services">The IServiceCollection to add services to.</param>
    /// <returns>The updated IServiceCollection.</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ICoinsService, CoinsService>();
        services.AddScoped<IKlineDataService, KlineDataService>();
        services.AddScoped<ITradingPairsService, TradingPairsService>();

        return services;
    }
}
