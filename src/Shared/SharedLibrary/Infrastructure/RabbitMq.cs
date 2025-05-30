using RabbitMQ.Client;
using SharedLibrary.Constants;

namespace SharedLibrary.Infrastructure;

/// <summary>
/// Shared RabbitMQ infrastructure utilities for setting up exchanges, queues, and connections.
/// </summary>
public static class RabbitMq
{
    /// <summary>
    /// Sets up the required exchanges, queues, and bindings for the crypto system.
    /// This should be called during application startup to ensure infrastructure exists.
    /// </summary>
    /// <param name="connectionFactory">The connection factory to use.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task SetupInfrastructureAsync(IConnectionFactory connectionFactory)
    {
        using var connection = await connectionFactory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();

        // Declare the main exchange
        await channel.ExchangeDeclareAsync(
            exchange: JobConstants.Exchanges.CryptoScheduler,
            type: ExchangeType.Topic,
            durable: true
        );

        // Declare queues for GUI_Crypto using constants
        foreach (var queueName in JobConstants.QueueNames.GetAllGuiQueues())
        {
            await channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false
            );
        }

        // Bind queues to exchange using constants
        foreach (var binding in JobConstants.QueueNames.GetGuiQueueBindings())
        {
            await channel.QueueBindAsync(
                queue: binding.Key,
                exchange: JobConstants.Exchanges.CryptoScheduler,
                routingKey: binding.Value
            );
        }
    }

    /// <summary>
    /// Gets the default RabbitMQ connection string.
    /// </summary>
    /// <returns>The default connection string.</returns>
    public static string GetDefaultConnectionString()
    {
        return "amqp://guest:guest@localhost:5672/";
    }
}
