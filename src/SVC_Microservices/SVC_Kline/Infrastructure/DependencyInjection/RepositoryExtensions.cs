using SVC_Kline.Repositories;

namespace SVC_Kline.Infrastructure.DependencyInjection;

/// <summary>
/// Extension methods for configuring repository services.
/// </summary>
public static class RepositoryExtensions
{
    /// <summary>
    /// Adds repository services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IKlineDataRepository, KlineDataRepository>();

        return services;
    }
}
