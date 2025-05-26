using SVC_External.ExternalClients.Exchanges;
using SVC_External.ExternalClients.Exchanges.Binance;
using SVC_External.ExternalClients.Exchanges.Bybit;
using SVC_External.ExternalClients.Exchanges.Mexc;
using SVC_External.ExternalClients.MarketDataProviders.CoinGecko;

namespace SVC_External.Infrastructure.DependencyInjection;

public static class ExternalClientServiceExtensions
{
    public static IServiceCollection AddExternalClients(this IServiceCollection services)
    {
        services.AddExchangeClients();
        services.AddMarketDataProviderClients();

        return services;
    }

    private static IServiceCollection AddExchangeClients(this IServiceCollection services)
    {
        services.AddScoped<IExchangesClient, BinanceClient>();
        services.AddScoped<IExchangesClient, BybitClient>();
        services.AddScoped<IExchangesClient, MexcClient>();

        return services;
    }

    private static IServiceCollection AddMarketDataProviderClients(this IServiceCollection services)
    {
        services.AddScoped<ICoinGeckoClient, CoinGeckoClient>();

        return services;
    }
}
