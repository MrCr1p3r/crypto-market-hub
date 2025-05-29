using SVC_Bridge.MicroserviceClients.SvcCoins;
using SVC_Bridge.MicroserviceClients.SvcExternal;
using SVC_Bridge.MicroserviceClients.SvcKline;

namespace SVC_Bridge.Infrastructure.DependencyInjection;

/// <summary>
/// Extension methods for configuring microservice client implementations.
/// </summary>
public static class MicroserviceClientExtensions
{
    /// <summary>
    /// Adds microservice client implementations to the DI container.
    /// </summary>
    /// <param name="services">The IServiceCollection to add services to.</param>
    /// <returns>The updated IServiceCollection.</returns>
    public static IServiceCollection AddMicroserviceClients(this IServiceCollection services)
    {
        services.AddScoped<ISvcCoinsClient, SvcCoinsClient>();
        services.AddScoped<ISvcExternalClient, SvcExternalClient>();
        services.AddScoped<ISvcKlineClient, SvcKlineClient>();

        return services;
    }
}
