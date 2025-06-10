namespace GUI_Crypto.Infrastructure.Messaging;

/// <summary>
/// Source-generated logging methods for MessageConsumer.
/// </summary>
public static partial class MessageConsumerLogging
{
    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Information,
        Message = "Started consuming messages from queue '{queueName}'"
    )]
    public static partial void LogConsumingStarted(this ILogger logger, string queueName);

    [LoggerMessage(
        EventId = 2002,
        Level = LogLevel.Information,
        Message = "Stopped consuming messages from queue '{queueName}'"
    )]
    public static partial void LogConsumingStopped(this ILogger logger, string queueName);

    [LoggerMessage(
        EventId = 2003,
        Level = LogLevel.Debug,
        Message = "Processing message from queue '{queueName}'"
    )]
    public static partial void LogMessageProcessingStarted(this ILogger logger, string queueName);

    [LoggerMessage(
        EventId = 2004,
        Level = LogLevel.Debug,
        Message = "Successfully processed message from queue '{queueName}'"
    )]
    public static partial void LogMessageProcessedSuccessfully(
        this ILogger logger,
        string queueName
    );

    [LoggerMessage(
        EventId = 2006,
        Level = LogLevel.Error,
        Message = "Error processing message from queue '{queueName}'. Message will be {messageAction}"
    )]
    public static partial void LogMessageProcessingFailed(
        this ILogger logger,
        Exception exception,
        string queueName,
        string messageAction
    );

    [LoggerMessage(
        EventId = 2007,
        Level = LogLevel.Warning,
        Message = "Received null message from queue '{queueName}'. Message will be discarded"
    )]
    public static partial void LogNullMessageReceived(this ILogger logger, string queueName);

    [LoggerMessage(
        EventId = 2008,
        Level = LogLevel.Error,
        Message = "Failed to setup consumer for queue '{queueName}'"
    )]
    public static partial void LogConsumerSetupFailed(
        this ILogger logger,
        Exception exception,
        string queueName
    );

    [LoggerMessage(
        EventId = 2009,
        Level = LogLevel.Warning,
        Message = "Consumer for queue '{queueName}' already exists"
    )]
    public static partial void LogConsumerAlreadyExists(this ILogger logger, string queueName);
}
