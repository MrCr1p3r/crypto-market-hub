using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using SharedLibrary.Constants;
using SharedLibrary.Models.Messaging;

namespace GUI_Crypto.Tests.Integration.Messaging.Helpers;

/// <summary>
/// Helper class for publishing test messages to RabbitMQ during integration tests.
/// </summary>
public class MessagePublisher(IConnectionFactory connectionFactory)
{
    private readonly IConnectionFactory _connectionFactory = connectionFactory;

    /// <summary>
    /// Publishes a successful job completion message with data.
    /// </summary>
    /// <typeparam name="T">Type of data to include in the message.</typeparam>
    /// <param name="queueName">Name of the queue to publish to.</param>
    /// <param name="data">Data to include in the message.</param>
    /// <param name="jobName">Name of the job (optional).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task PublishJobCompletedMessageAsync<T>(
        string queueName,
        T data,
        string? jobName = null
    )
    {
        var message = new JobCompletedMessage
        {
            JobName = jobName ?? GetJobNameFromQueue(queueName),
            JobType = JobConstants.Types.DataSync,
            CompletedAt = DateTime.UtcNow,
            Success = true,
            ErrorMessage = null,
            Data = data,
            Source = "Test",
        };

        await PublishMessageAsync(queueName, message);
    }

    /// <summary>
    /// Publishes a failed job completion message.
    /// </summary>
    /// <param name="queueName">Name of the queue to publish to.</param>
    /// <param name="errorMessage">Error message for the failed job.</param>
    /// <param name="jobName">Name of the job (optional).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task PublishJobFailedMessageAsync(
        string queueName,
        string errorMessage,
        string? jobName = null
    )
    {
        var message = new JobCompletedMessage
        {
            JobName = jobName ?? GetJobNameFromQueue(queueName),
            JobType = JobConstants.Types.DataSync,
            CompletedAt = DateTime.UtcNow,
            Success = false,
            ErrorMessage = errorMessage,
            Data = null,
            Source = "Test",
        };

        await PublishMessageAsync(queueName, message);
    }

    /// <summary>
    /// Publishes a message directly to a queue.
    /// </summary>
    /// <typeparam name="T">Type of message to publish.</typeparam>
    /// <param name="queueName">Name of the queue to publish to.</param>
    /// <param name="message">Message to publish.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task PublishMessageAsync<T>(string queueName, T message)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();

        // Ensure queue exists
        await channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false
        );

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

        await channel.BasicPublishAsync(exchange: string.Empty, routingKey: queueName, body: body);
    }

    /// <summary>
    /// Maps queue names to appropriate job names.
    /// </summary>
    /// <param name="queueName">Name of the queue.</param>
    /// <returns>Corresponding job name.</returns>
    private static string GetJobNameFromQueue(string queueName)
    {
        return queueName switch
        {
            JobConstants.QueueNames.GuiMarketDataUpdated => JobConstants.Names.MarketDataUpdate,
            JobConstants.QueueNames.GuiKlineDataUpdated => JobConstants.Names.KlineDataUpdate,
            JobConstants.QueueNames.GuiTradingPairsUpdated => JobConstants.Names.TradingPairsUpdate,
            JobConstants.QueueNames.GuiCacheWarmupCompleted => JobConstants
                .Names
                .SpotCoinsCacheWarmup,
            _ => "Test Job",
        };
    }
}
