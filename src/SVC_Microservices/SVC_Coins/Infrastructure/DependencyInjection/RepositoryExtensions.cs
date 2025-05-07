using SVC_Coins.Repositories;
using SVC_Coins.Repositories.Interfaces;

namespace SVC_Coins.Infrastructure.DependencyInjection;

/// <summary>
/// Extension methods for configuring repository services.
/// </summary>
public static class RepositoryExtensions
{
    /// <summary>
    /// Adds repository services to the DI container.
    /// </summary>
    /// <param name="services">The IServiceCollection to add services to.</param>
    /// <returns>The updated IServiceCollection.</returns>
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<ICoinsRepository, CoinsRepository>();
        services.AddScoped<ITradingPairsRepository, TradingPairsRepository>();
        services.AddScoped<IExchangesRepository, ExchangesRepository>();

        return services;
    }
}
