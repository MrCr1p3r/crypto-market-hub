using SharedLibrary.Infrastructure;

namespace SVC_Coins.Infrastructure.DependencyInjection;

/// <summary>
/// Extension methods for configuring core infrastructure services.
/// </summary>
public static class InfrastructureExtensions
{
    /// <summary>
    /// Adds exception handling and problem details services.
    /// </summary>
    /// <param name="services">The IServiceCollection to add services to.</param>
    /// <returns>The updated IServiceCollection.</returns>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // Register the global exception handler
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        return services;
    }
}
