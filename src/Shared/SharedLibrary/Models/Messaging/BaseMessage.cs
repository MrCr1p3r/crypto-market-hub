namespace SharedLibrary.Models.Messaging;

/// <summary>
/// Base class for all messages sent through the message broker.
/// </summary>
public abstract record BaseMessage
{
    /// <summary>
    /// Unique identifier for this message.
    /// </summary>
    public string MessageId { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Timestamp when the message was created.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Source service that generated this message.
    /// </summary>
    public required string Source { get; init; }
}
