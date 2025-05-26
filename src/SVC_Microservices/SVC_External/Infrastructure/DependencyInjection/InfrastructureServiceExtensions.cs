using System.Threading.RateLimiting;
using Microsoft.Extensions.Http.Resilience;
using SharedLibrary.Infrastructure;
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
                options.Duration = TimeSpan.FromMinutes(5);
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
        var rateLimiter = new SlidingWindowRateLimiter(
            new SlidingWindowRateLimiterOptions
            {
                Window = TimeSpan.FromSeconds(60),
                SegmentsPerWindow = 20,
                PermitLimit = 30,
                QueueLimit = int.MaxValue,
            }
        );
        services.AddSingleton(rateLimiter);

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

                    var apiKey = configuration["COINGECKO_API_KEY"];
                    if (!string.IsNullOrEmpty(apiKey))
                    {
                        client.DefaultRequestHeaders.Add("x-cg-demo-api-key", apiKey);
                    }
                }
            )
            .AddStandardResilienceHandler(options =>
            {
                options.RateLimiter = new HttpRateLimiterStrategyOptions
                {
                    Name = "CoinGeckoClient-RateLimiter",
                    RateLimiter = args =>
                        rateLimiter.AcquireAsync(cancellationToken: args.Context.CancellationToken),
                };
            });

        return services;
    }
}
