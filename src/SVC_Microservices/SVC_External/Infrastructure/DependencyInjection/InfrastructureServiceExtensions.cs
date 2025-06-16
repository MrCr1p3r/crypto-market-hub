using SharedLibrary.Infrastructure;
using SVC_External.ExternalClients.MarketDataProviders.CoinGecko;
using SVC_External.Infrastructure.CoinGecko;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;

namespace SVC_External.Infrastructure.DependencyInjection;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        return services;
    }

    public static IServiceCollection AddHttpClients(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddBinanceHttpClient(configuration);
        services.AddBybitHttpClient(configuration);
        services.AddMexcHttpClient(configuration);
        services.AddCoinGeckoHttpClient(configuration);

        return services;
    }

    public static IServiceCollection AddCachingServices(this IServiceCollection services)
    {
        services
            .AddFusionCache()
            .WithDefaultEntryOptions(options =>
            {
                options.Duration = TimeSpan.FromHours(1);
                // Start eager refresh after ≈ 40 minutes (66% of 1 hour)
                options.EagerRefreshThreshold = 0.66f;
            })
            .WithSerializer(new FusionCacheSystemTextJsonSerializer())
            .WithRegisteredDistributedCache()
            .AsHybridCache();

        return services;
    }

    private static IServiceCollection AddBinanceHttpClient(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddHttpClient(
            "BinanceClient",
            client =>
            {
                var baseUrl =
                    configuration["Services:BinanceClient:BaseUrl"] ?? "https://api.binance.com";
                client.BaseAddress = new Uri(baseUrl);
            }
        );

        return services;
    }

    private static IServiceCollection AddBybitHttpClient(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddHttpClient(
            "BybitClient",
            client =>
            {
                var baseUrl =
                    configuration["Services:BybitClient:BaseUrl"] ?? "https://api.bybit.com";
                client.BaseAddress = new Uri(baseUrl);
            }
        );

        return services;
    }

    private static IServiceCollection AddMexcHttpClient(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddHttpClient(
            "MexcClient",
            client =>
            {
                var baseUrl =
                    configuration["Services:MexcClient:BaseUrl"] ?? "https://api.mexc.com";
                client.BaseAddress = new Uri(baseUrl);
            }
        );

        return services;
    }

    private static IServiceCollection AddCoinGeckoHttpClient(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddSingleton<
            ICoinGeckoAuthenticationStateService,
            CoinGeckoAuthenticationStateService
        >();

        services.AddTransient<CoinGeckoAdaptiveHandler>();

        services
            .AddHttpClient(
                "CoinGeckoClient",
                client =>
                {
                    var baseUrl =
                        configuration["Services:CoinGeckoClient:BaseUrl"]
                        ?? "https://api.coingecko.com";
                    client.BaseAddress = new Uri(baseUrl);
                    client.DefaultRequestHeaders.Add("accept", "application/json");
                    client.DefaultRequestHeaders.Add("User-Agent", "SVC_External");
                }
            )
            .AddHttpMessageHandler<CoinGeckoAdaptiveHandler>();

        return services;
    }
}
