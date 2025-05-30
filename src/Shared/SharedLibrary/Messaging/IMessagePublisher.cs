using SharedLibrary.Models.Messaging;

namespace SharedLibrary.Messaging;

/// <summary>
/// Interface for publishing messages to a message broker.
/// </summary>
public interface IMessagePublisher
{
    /// <summary>
    /// Publishes a message to the default exchange with the specified routing key.
    /// </summary>
    /// <param name="routingKey">The routing key for message routing.</param>
    /// <param name="message">The message to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task, that publishes the message to the message broker.</returns>
    Task PublishAsync(
        string routingKey,
        JobCompletedMessage message,
        CancellationToken cancellationToken = default
    );
}
