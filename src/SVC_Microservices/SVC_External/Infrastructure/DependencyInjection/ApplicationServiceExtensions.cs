using SVC_External.Services.Exchanges;
using SVC_External.Services.Exchanges.Interfaces;
using SVC_External.Services.MarketDataProviders;
using SVC_External.Services.MarketDataProviders.Interfaces;

namespace SVC_External.Infrastructure.DependencyInjection;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddExchangeServices();
        services.AddMarketDataProviderServices();

        return services;
    }

    private static IServiceCollection AddExchangeServices(this IServiceCollection services)
    {
        services.AddScoped<ICoinsService, CoinsService>();
        services.AddScoped<IKlineDataService, KlineDataService>();

        return services;
    }

    private static IServiceCollection AddMarketDataProviderServices(
        this IServiceCollection services
    )
    {
        services.AddScoped<ICoinGeckoService, CoinGeckoService>();

        return services;
    }
}
