using SVC_Coins.Services;
using SVC_Coins.Services.Validators;
using SVC_Coins.Services.Validators.Interfaces;

namespace SVC_Coins.Infrastructure.DependencyInjection;

/// <summary>
/// Extension methods for configuring application services and domain validators.
/// </summary>
public static class ServiceExtensions
{
    /// <summary>
    /// Adds application services and domain validators to the DI container.
    /// </summary>
    /// <param name="services">The IServiceCollection to add services to.</param>
    /// <returns>The updated IServiceCollection.</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Domain Validators
        services.AddScoped<ICoinsValidator, CoinsValidator>();
        services.AddScoped<ITradingPairsValidator, TradingPairsValidator>();

        // Application Services
        services.AddScoped<ICoinsService, CoinsService>();

        return services;
    }
}
