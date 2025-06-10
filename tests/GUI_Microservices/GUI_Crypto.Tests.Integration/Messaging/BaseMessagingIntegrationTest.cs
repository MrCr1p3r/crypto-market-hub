using GUI_Crypto.Tests.Integration.Controllers;
using GUI_Crypto.Tests.Integration.Messaging.Helpers;
using Microsoft.AspNetCore.SignalR.Client;

namespace GUI_Crypto.Tests.Integration.Messaging;

/// <summary>
/// Base class for messaging integration tests with SignalR and RabbitMQ support.
/// </summary>
public abstract class BaseMessagingIntegrationTest : BaseControllerIntegrationTest
{
    private protected MessagePublisher MessagePublisher { get; }

    private readonly List<HubConnection> _signalRConnections = [];

    protected BaseMessagingIntegrationTest(CustomWebApplicationFactory factory)
        : base(factory)
    {
        MessagePublisher = new MessagePublisher(Factory.RabbitMqConnectionFactory);
    }

    /// <summary>
    /// Creates and starts a SignalR connection, automatically subscribing to overview updates.
    /// </summary>
    /// <returns>A connected SignalR hub connection.</returns>
    protected async Task<HubConnection> CreateSignalRConnectionAsync()
    {
        var connection = await Factory.CreateSignalRConnectionAsync();
        await connection.InvokeAsync("SubscribeToOverviewUpdates");

        // Track connection for disposal
        _signalRConnections.Add(connection);

        return connection;
    }

    /// <summary>
    /// Creates multiple SignalR connections for testing multi-client scenarios.
    /// </summary>
    /// <param name="count">Number of connections to create.</param>
    /// <returns>Collection of connected SignalR hub connections.</returns>
    protected async Task<IEnumerable<HubConnection>> CreateMultipleSignalRConnectionsAsync(
        int count
    )
    {
        var connections = new List<HubConnection>();

        for (int i = 0; i < count; i++)
        {
            var connection = await CreateSignalRConnectionAsync();
            connections.Add(connection);
        }

        return connections;
    }

    /// <summary>
    /// Waits for a specified duration to allow asynchronous operations to complete,
    /// especially in tests where no message is expected.
    /// </summary>
    /// <param name="milliseconds">Milliseconds to wait (default: 1000ms).</param>
    /// <returns>A task representing the wait operation.</returns>
    protected static async Task AllowTimeForProcessingAsync(int milliseconds = 1000)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(milliseconds));
    }

    /// <summary>
    /// Creates a SignalR listener that captures messages and signals when a message is received.
    /// </summary>
    /// <typeparam name="T">The type of the message to listen for.</typeparam>
    /// <param name="connection">The SignalR hub connection.</param>
    /// <param name="methodName">The name of the SignalR hub method to listen to.</param>
    /// <returns>A <see cref="SignalRListener{T}"/> instance.</returns>
    protected static SignalRListener<T> CreateSignalRListener<T>(
        HubConnection connection,
        string methodName
    )
    {
        return new SignalRListener<T>(connection, methodName);
    }

    public override async Task DisposeAsync()
    {
        // Dispose all SignalR connections
        foreach (var connection in _signalRConnections)
        {
            await connection.DisposeAsync();
        }

        _signalRConnections.Clear();

        await base.DisposeAsync();
    }

    /// <summary>
    /// A helper class to listen for SignalR messages and wait for their arrival in tests.
    /// </summary>
    /// <typeparam name="T">The type of the message.</typeparam>
    public class SignalRListener<T>
    {
        private readonly TaskCompletionSource _messageReceivedTcs = new(
            TaskCreationOptions.RunContinuationsAsynchronously
        );

        private readonly List<T> _receivedMessages = [];

        private readonly object _lock = new();

        public IReadOnlyList<T> ReceivedMessages
        {
            get
            {
                lock (_lock)
                {
                    return [.. _receivedMessages];
                }
            }
        }

        public T FirstMessage
        {
            get
            {
                lock (_lock)
                {
                    return _receivedMessages.FirstOrDefault()!;
                }
            }
        }

        public SignalRListener(HubConnection connection, string methodName)
        {
            connection.On<T>(
                methodName,
                message =>
                {
                    lock (_lock)
                    {
                        _receivedMessages.Add(message);
                    }

                    _messageReceivedTcs.TrySetResult();
                }
            );
        }

        /// <summary>
        /// Waits for a message to be received. Throws a timeout exception if no message is received within the specified duration.
        /// </summary>
        /// <param name="timeout">The maximum time to wait. Defaults to 5 seconds.</param>
        /// <returns>A task that completes when a message is received.</returns>
        public Task WaitAsync(TimeSpan? timeout = null)
        {
            return _messageReceivedTcs.Task.WaitAsync(timeout ?? TimeSpan.FromSeconds(5));
        }
    }
}
