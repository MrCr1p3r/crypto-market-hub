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

    private readonly List<HubConnection> _borrowedConnections = [];

    protected BaseMessagingIntegrationTest(CustomWebApplicationFactory factory)
        : base(factory)
    {
        MessagePublisher = new MessagePublisher(Factory.RabbitMqConnectionFactory);
    }

    /// <summary>
    /// Gets a SignalR connection from the factory's pool, automatically subscribing to overview updates.
    /// This reuses connections across test instances for optimal performance.
    /// </summary>
    /// <returns>A connected SignalR hub connection.</returns>
    protected async Task<HubConnection> GetSignalRConnection()
    {
        var connection = await Factory.GetPooledSignalRConnectionAsync();
        await connection.InvokeAsync("SubscribeToOverviewUpdates");

        // Track borrowed connections for proper return to pool
        _borrowedConnections.Add(connection);

        return connection;
    }

    /// <summary>
    /// Creates multiple SignalR connections for testing multi-client scenarios.
    /// The first connection comes from the pool, additional ones are created as needed.
    /// </summary>
    /// <param name="count">Number of connections to create.</param>
    /// <returns>Collection of connected SignalR hub connections.</returns>
    protected async Task<IEnumerable<HubConnection>> GetMultipleSignalRConnections(int count)
    {
        var connections = new List<HubConnection>();

        for (int i = 0; i < count; i++)
        {
            var connection = await GetSignalRConnection();
            connections.Add(connection);
        }

        return connections;
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

    /// <summary>
    /// Creates a SignalR listener for parameterless SignalR methods.
    /// </summary>
    /// <param name="connection">The SignalR hub connection.</param>
    /// <param name="methodName">The name of the SignalR hub method to listen to.</param>
    /// <returns>A <see cref="SignalRParameterlessListener"/> instance.</returns>
    protected static SignalRParameterlessListener CreateParameterlessSignalRListener(
        HubConnection connection,
        string methodName
    )
    {
        return new SignalRParameterlessListener(connection, methodName);
    }

    public override async Task DisposeAsync()
    {
        // Return all borrowed connections to the pool for reuse
        foreach (var connection in _borrowedConnections)
        {
            Factory.ReturnSignalRConnectionToPool(connection);
        }

        _borrowedConnections.Clear();

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

    /// <summary>
    /// A helper class to listen for parameterless SignalR methods and wait for their invocation in tests.
    /// </summary>
    public class SignalRParameterlessListener
    {
        private readonly TaskCompletionSource _messageReceivedTcs = new(
            TaskCreationOptions.RunContinuationsAsynchronously
        );

        private readonly object _lock = new();
        private int _callCount;

        public int CallCount
        {
            get
            {
                lock (_lock)
                {
                    return _callCount;
                }
            }
        }

        public SignalRParameterlessListener(HubConnection connection, string methodName)
        {
            connection.On(
                methodName,
                () =>
                {
                    lock (_lock)
                    {
                        _callCount++;
                    }

                    _messageReceivedTcs.TrySetResult();
                }
            );
        }

        /// <summary>
        /// Waits for the method to be called. Throws a timeout exception if not called within the specified duration.
        /// </summary>
        /// <param name="timeout">The maximum time to wait. Defaults to 5 seconds.</param>
        /// <returns>A task that completes when the method is called.</returns>
        public Task WaitAsync(TimeSpan? timeout = null)
        {
            return _messageReceivedTcs.Task.WaitAsync(timeout ?? TimeSpan.FromSeconds(5));
        }
    }
}
