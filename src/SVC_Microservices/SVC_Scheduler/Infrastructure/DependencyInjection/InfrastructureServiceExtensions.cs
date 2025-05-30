using Coravel;
using RabbitMQ.Client;
using SharedLibrary.Infrastructure;
using SharedLibrary.Messaging;
using SVC_Scheduler.Infrastructure.Messaging;
using SVC_Scheduler.Jobs;
using SVC_Scheduler.SvcBridgeClient;

namespace SVC_Scheduler.Infrastructure.DependencyInjection;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // Add global exception handler and problem details
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        // Add Coravel scheduler
        services.AddScheduler();

        return services;
    }

    public static IServiceCollection AddScheduledJobs(this IServiceCollection services)
    {
        // Register scheduled jobs
        services.AddTransient<MarketDataUpdateJob>();
        services.AddTransient<KlineDataUpdateJob>();
        services.AddTransient<TradingPairsUpdateJob>();

        return services;
    }

    /// <summary>
    /// Adds RabbitMQ publisher services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddRabbitMqPublisher(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddSingleton<IConnectionFactory>(provider =>
        {
            var connectionString =
                configuration.GetConnectionString("rabbitmq")
                ?? RabbitMq.GetDefaultConnectionString();

            return new ConnectionFactory { Uri = new Uri(connectionString) };
        });

        services.AddSingleton<IMessagePublisher, MessagePublisher>();

        return services;
    }

    public static IServiceCollection AddHttpClients(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddSvcBridgeHttpClient(configuration);

        return services;
    }

    public static IServiceCollection AddMicroserviceClients(this IServiceCollection services)
    {
        services.AddScoped<ISvcBridgeClient, SvcBridgeClient.SvcBridgeClient>();

        return services;
    }

    private static IServiceCollection AddSvcBridgeHttpClient(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddHttpClient(
            "SvcBridgeClient",
            client =>
            {
                var baseUrl =
                    configuration["Services:SvcBridgeClient:BaseUrl"] ?? "http://localhost:5109";
                client.BaseAddress = new Uri(baseUrl);
                client.Timeout = TimeSpan.FromMinutes(5);
            }
        );

        return services;
    }
}
