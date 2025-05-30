namespace SVC_Scheduler.Infrastructure.Messaging;

/// <summary>
/// Source-generated logging methods for MessagePublisher.
/// </summary>
public static partial class MessagePublisherLogging
{
    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Message published successfully to exchange '{exchange}' with routing key '{routingKey}' for message type '{messageType}'"
    )]
    public static partial void LogMessagePublished(
        this ILogger logger,
        string exchange,
        string routingKey,
        string messageType
    );

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Error,
        Message = "Failed to publish message to exchange '{exchange}' with routing key '{routingKey}'"
    )]
    public static partial void LogMessagePublishFailed(
        this ILogger logger,
        Exception exception,
        string exchange,
        string routingKey
    );
}
