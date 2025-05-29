namespace SVC_Bridge.Infrastructure.DependencyInjection;

/// <summary>
/// Extension methods for configuring Web API core services like controllers and Swagger.
/// </summary>
public static class WebApiExtensions
{
    /// <summary>
    /// Adds core Web API services (Controllers, API Explorer, Swagger) to the DI container.
    /// </summary>
    /// <param name="services">The IServiceCollection to add services to.</param>
    /// <returns>The updated IServiceCollection.</returns>
    public static IServiceCollection AddWebApiServices(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        return services;
    }

    /// <summary>
    /// Configures the Swagger UI middleware, typically only for development environments.
    /// </summary>
    /// <param name="app">The WebApplication instance.</param>
    /// <returns>The updated WebApplication instance.</returns>
    public static WebApplication UseSwaggerPipeline(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        return app;
    }
}
