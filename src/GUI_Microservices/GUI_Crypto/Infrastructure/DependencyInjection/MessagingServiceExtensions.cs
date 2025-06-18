using GUI_Crypto.Infrastructure.Caching;
using GUI_Crypto.Infrastructure.Messaging;
using GUI_Crypto.Services.Messaging;
using RabbitMQ.Client;
using SharedLibrary.Infrastructure;
using SharedLibrary.Messaging;

namespace GUI_Crypto.Infrastructure.DependencyInjection;

/// <summary>
/// Extension methods for configuring messaging services.
/// </summary>
public static class MessagingServiceExtensions
{
    /// <summary>
    /// Adds all messaging-related services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMessagingServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        // Add RabbitMQ services
        services.AddRabbitMqConsumer(configuration);

        // Add SignalR
        services.AddSignalR();

        // Add message handlers
        services.AddMessageHandlers();

        // Add background service
        services.AddHostedService<MessageConsumerBackgroundService>();

        return services;
    }

    /// <summary>
    /// Adds RabbitMQ consumer services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    private static IServiceCollection AddRabbitMqConsumer(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        // Register RabbitMQ connection factory
        services.AddSingleton<IConnectionFactory>(provider =>
        {
            var connectionString =
                configuration.GetConnectionString("rabbitmq")
                ?? RabbitMq.GetDefaultConnectionString();

            return new ConnectionFactory { Uri = new Uri(connectionString) };
        });

        // Register message consumer
        services.AddSingleton<IMessageConsumer, MessageConsumer>();

        return services;
    }

    /// <summary>
    /// Adds message handlers to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    private static IServiceCollection AddMessageHandlers(this IServiceCollection services)
    {
        services.AddScoped<MarketDataMessageHandler>();
        services.AddScoped<KlineDataMessageHandler>();
        services.AddScoped<CacheWarmupMessageHandler>();

        // Add cache warmup state service as singleton to maintain state across requests
        services.AddSingleton<ICacheWarmupStateService, CacheWarmupStateService>();

        return services;
    }

    /// <summary>
    /// Sets up RabbitMQ infrastructure on application startup.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The web application for chaining.</returns>
    public static async Task<WebApplication> SetupRabbitMqInfrastructureAsync(
        this WebApplication app
    )
    {
        using var scope = app.Services.CreateScope();
        var connectionFactory = scope.ServiceProvider.GetRequiredService<IConnectionFactory>();
        await RabbitMq.SetupInfrastructureAsync(connectionFactory);
        return app;
    }
}
