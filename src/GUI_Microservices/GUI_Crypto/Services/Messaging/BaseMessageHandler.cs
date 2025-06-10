using System.Text.Json;
using SharedLibrary.Models.Messaging;

namespace GUI_Crypto.Services.Messaging;

/// <summary>
/// Base class for message handlers providing common functionality.
/// </summary>
/// <typeparam name="TData">The type of data this handler processes.</typeparam>
public abstract class BaseMessageHandler<TData>(ILogger logger)
{
    private readonly ILogger _logger = logger;

    /// <summary>
    /// Handles a job completion message.
    /// </summary>
    /// <param name="message">The job completion message to handle.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task HandleAsync(
        JobCompletedMessage message,
        CancellationToken cancellationToken = default
    )
    {
        if (!message.Success)
        {
            _logger.LogJobFailed(message.JobName, message.ErrorMessage);
            return;
        }

        if (message.Data is null)
        {
            _logger.LogSuccessWithoutData(message.JobName);
            return;
        }

        var data = DeserializeMessageData(message.Data);

        await HandleSuccess(message, data!, cancellationToken);
    }

    /// <summary>
    /// Handles a successful job completion with data.
    /// </summary>
    /// <param name="message">The original message.</param>
    /// <param name="data">The deserialized data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected abstract Task HandleSuccess(
        JobCompletedMessage message,
        TData data,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Deserializes the message data to the expected type.
    /// </summary>
    /// <param name="data">The data to deserialize.</param>
    /// <returns>The deserialized data or null if deserialization fails.</returns>
    private static TData? DeserializeMessageData(object data) =>
        data switch
        {
            JsonElement jsonElement => JsonSerializer.Deserialize<TData>(jsonElement.GetRawText()),
            string jsonString => JsonSerializer.Deserialize<TData>(jsonString),
            _ => throw new JsonException($"Invalid data type: {data.GetType().Name}"),
        };
}
