namespace SVC_Bridge.Infrastructure.DependencyInjection;

/// <summary>
/// Extension methods for configuring HTTP clients for microservice communication.
/// </summary>
public static class HttpClientExtensions
{
    /// <summary>
    /// Adds HTTP clients for microservice communication.
    /// </summary>
    /// <param name="services">The IServiceCollection to add services to.</param>
    /// <param name="configuration">The configuration instance.</param>
    /// <returns>The updated IServiceCollection.</returns>
    public static IServiceCollection AddHttpClients(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        // SVC Coins Client
        services.AddHttpClient(
            "SvcCoinsClient",
            client =>
            {
                var baseAddress =
                    configuration["Services:SvcCoinsClient:BaseUrl"] ?? "http://localhost:5001";
                client.BaseAddress = new Uri(baseAddress);
            }
        );

        // SVC External Client
        services.AddHttpClient(
            "SvcExternalClient",
            client =>
            {
                var baseAddress =
                    configuration["Services:SvcExternalClient:BaseUrl"] ?? "http://localhost:5003";
                client.BaseAddress = new Uri(baseAddress);
            }
        );

        // SVC Kline Client
        services.AddHttpClient(
            "SvcKlineClient",
            client =>
            {
                var baseAddress =
                    configuration["Services:SvcKlineClient:BaseUrl"] ?? "http://localhost:5002";
                client.BaseAddress = new Uri(baseAddress);
            }
        );

        return services;
    }
}
