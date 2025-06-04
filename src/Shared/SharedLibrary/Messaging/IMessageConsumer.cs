namespace SharedLibrary.Messaging;

/// <summary>
/// Interface for consuming messages from a message broker.
/// </summary>
public interface IMessageConsumer
{
    /// <summary>
    /// Starts consuming messages from the specified queue.
    /// </summary>
    /// <typeparam name="T">The type of message to consume.</typeparam>
    /// <param name="queueName">The name of the queue to consume from.</param>
    /// <param name="messageHandler">Handler function for processing received messages.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task, that starts consuming messages from the specified queue.</returns>
    Task StartConsumingAsync<T>(
        string queueName,
        Func<T, Task> messageHandler,
        CancellationToken cancellationToken = default
    )
        where T : class;

    /// <summary>
    /// Stops consuming messages from all queues.
    /// </summary>
    /// <returns>A task, that stops consuming messages from all queues.</returns>
    Task StopConsumingAsync();
}
