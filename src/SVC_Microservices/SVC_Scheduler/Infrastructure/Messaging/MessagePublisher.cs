using System.Text.Json;
using RabbitMQ.Client;
using SharedLibrary.Constants;
using SharedLibrary.Messaging;
using SharedLibrary.Models.Messaging;

namespace SVC_Scheduler.Infrastructure.Messaging;

/// <summary>
/// RabbitMQ implementation of the message publisher for SVC_Scheduler.
/// </summary>
public class MessagePublisher : IMessagePublisher, IDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly ILogger<MessagePublisher> _logger;

    public MessagePublisher(IConnectionFactory connectionFactory, ILogger<MessagePublisher> logger)
    {
        _logger = logger;
        _connection = connectionFactory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public async Task PublishAsync(
        string routingKey,
        JobCompletedMessage message,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var messageBody = JsonSerializer.SerializeToUtf8Bytes(message);
            var properties = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
            };

            await _channel.BasicPublishAsync(
                exchange: JobConstants.Exchanges.CryptoScheduler,
                routingKey: routingKey,
                mandatory: true,
                basicProperties: properties,
                body: messageBody,
                cancellationToken: cancellationToken
            );

            _logger.LogMessagePublished(
                JobConstants.Exchanges.CryptoScheduler,
                routingKey,
                nameof(JobCompletedMessage)
            );
        }
        catch (Exception ex)
        {
            _logger.LogMessagePublishFailed(ex, JobConstants.Exchanges.CryptoScheduler, routingKey);
            throw;
        }
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
            _channel?.CloseAsync().GetAwaiter().GetResult();
            _connection?.CloseAsync().GetAwaiter().GetResult();
            _channel?.Dispose();
            _connection?.Dispose();
        }
    }
}
