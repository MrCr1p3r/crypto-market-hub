using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SharedLibrary.Messaging;

namespace GUI_Crypto.Infrastructure.Messaging;

/// <summary>
/// RabbitMQ implementation of the message consumer for GUI_Crypto.
/// </summary>
public class MessageConsumer(IConnectionFactory connectionFactory, ILogger<MessageConsumer> logger)
    : IMessageConsumer,
        IDisposable
{
    private readonly IConnection _connection = connectionFactory
        .CreateConnectionAsync()
        .GetAwaiter()
        .GetResult();

    private readonly ILogger<MessageConsumer> _logger = logger;
    private readonly ConcurrentDictionary<string, IChannel> _channels = new();
    private readonly ConcurrentDictionary<string, AsyncEventingBasicConsumer> _consumers = new();

    /// <inheritdoc />
    public async Task StartConsumingAsync<T>(
        string queueName,
        Func<T, Task> messageHandler,
        CancellationToken cancellationToken = default
    )
        where T : class
    {
        try
        {
            var channel = await _connection.CreateChannelAsync(
                cancellationToken: cancellationToken
            );
            _channels[queueName] = channel;

            // Set QoS to process one message at a time for better load distribution
            await channel.BasicQosAsync(
                prefetchSize: 0,
                prefetchCount: 1,
                global: false,
                cancellationToken
            );

            var consumer = new AsyncEventingBasicConsumer(channel);
            _consumers[queueName] = consumer;

            consumer.ReceivedAsync += async (model, eventArgs) =>
            {
                var body = eventArgs.Body.ToArray();

                try
                {
                    _logger.LogMessageProcessingStarted(queueName);

                    var jsonString = Encoding.UTF8.GetString(body);
                    var message = JsonSerializer.Deserialize<T>(jsonString);

                    if (message == null)
                    {
                        _logger.LogNullMessageReceived(queueName);
                        await channel.BasicNackAsync(
                            eventArgs.DeliveryTag,
                            multiple: false,
                            requeue: false
                        );
                        return;
                    }

                    await messageHandler(message);

                    await channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false);
                    _logger.LogMessageProcessedSuccessfully(queueName);
                }
                catch (Exception ex)
                {
                    // Decide whether to requeue based on exception type
                    var requeue = ShouldRequeueMessage(ex);
                    var messageAction = requeue ? "requeued" : "discarded";

                    _logger.LogMessageProcessingFailed(ex, queueName, messageAction);

                    await channel.BasicNackAsync(
                        eventArgs.DeliveryTag,
                        multiple: false,
                        requeue: requeue
                    );
                }
            };

            await channel.BasicConsumeAsync(
                queue: queueName,
                autoAck: false,
                consumer: consumer,
                cancellationToken: cancellationToken
            );

            _logger.LogConsumingStarted(queueName);
        }
        catch (Exception ex)
        {
            _logger.LogConsumerSetupFailed(ex, queueName);

            // Clean up on failure
            if (_channels.TryRemove(queueName, out var failedChannel))
            {
                await failedChannel.CloseAsync(cancellationToken);
                failedChannel.Dispose();
            }

            _consumers.TryRemove(queueName, out _);

            throw;
        }
    }

    /// <inheritdoc />
    public async Task StopConsumingAsync()
    {
        var tasks = _channels.Select(async kvp =>
        {
            var queueName = kvp.Key;
            var channel = kvp.Value;

            if (_consumers.TryRemove(queueName, out var consumer))
            {
                // Cancel the consumer
                foreach (var consumerTag in consumer.ConsumerTags)
                {
                    await channel.BasicCancelAsync(consumerTag);
                }
            }

            if (!channel.IsClosed)
            {
                await channel.CloseAsync();
            }

            channel.Dispose();

            _logger.LogConsumingStopped(queueName);
        });

        await Task.WhenAll(tasks);

        _channels.Clear();
        _consumers.Clear();
    }

    /// <summary>
    /// Determines whether a message should be requeued based on the exception type.
    /// </summary>
    /// <param name="exception">The exception that occurred during message processing.</param>
    /// <returns>True if the message should be requeued, false otherwise.</returns>
    private static bool ShouldRequeueMessage(Exception exception)
    {
        return exception switch
        {
            // Don't requeue for argument/validation errors
            ArgumentException => false,
            InvalidOperationException => false,

            // Don't requeue for JSON/serialization errors
            JsonException => false,

            // Requeue for transient errors
            TimeoutException => true,
            TaskCanceledException => true,

            // Requeue for other exceptions (could be transient)
            _ => true,
        };
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            StopConsumingAsync().GetAwaiter().GetResult();
            _connection?.CloseAsync().GetAwaiter().GetResult();
            _connection?.Dispose();
        }
    }
}
