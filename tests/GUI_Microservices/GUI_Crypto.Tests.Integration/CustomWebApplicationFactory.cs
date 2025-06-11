using System.Collections.Concurrent;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using Testcontainers.RabbitMq;
using WireMock.Client;
using WireMock.Net.Testcontainers;

namespace GUI_Crypto.Tests.Integration;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly WireMockContainer _svcCoinsWireMockContainer =
        new WireMockContainerBuilder().Build();

    private readonly WireMockContainer _svcExternalWireMockContainer =
        new WireMockContainerBuilder().Build();

    private readonly WireMockContainer _svcKlineWireMockContainer =
        new WireMockContainerBuilder().Build();

    private readonly RabbitMqContainer _rabbitMqContainer = new RabbitMqBuilder().Build();

    // SignalR connection pool for test performance
    private readonly ConcurrentQueue<HubConnection> _signalRConnectionPool = new();
    private readonly List<HubConnection> _allConnections = [];
    private readonly SemaphoreSlim _connectionSemaphore = new(1, 1);

    // Expose mock servers for tests
    public IWireMockAdminApi SvcCoinsServerMock { get; private set; } = null!;

    public IWireMockAdminApi SvcExternalServerMock { get; private set; } = null!;

    public IWireMockAdminApi SvcKlineServerMock { get; private set; } = null!;

    // Expose RabbitMQ connection factory for messaging tests
    public IConnectionFactory RabbitMqConnectionFactory { get; private set; } = null!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(
            (context, config) =>
            {
                config.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Services:SvcCoinsClient:BaseUrl"] =
                            _svcCoinsWireMockContainer.GetPublicUrl(),
                        ["Services:SvcExternalClient:BaseUrl"] =
                            _svcExternalWireMockContainer.GetPublicUrl(),
                        ["Services:SvcKlineClient:BaseUrl"] =
                            _svcKlineWireMockContainer.GetPublicUrl(),
                        ["ConnectionStrings:rabbitmq"] = _rabbitMqContainer.GetConnectionString(),
                    }
                );
            }
        );
    }

    public async Task InitializeAsync()
    {
        await Task.WhenAll(
            _svcCoinsWireMockContainer.StartAsync(),
            _svcExternalWireMockContainer.StartAsync(),
            _svcKlineWireMockContainer.StartAsync(),
            _rabbitMqContainer.StartAsync()
        );

        SvcCoinsServerMock = _svcCoinsWireMockContainer.CreateWireMockAdminClient();
        SvcExternalServerMock = _svcExternalWireMockContainer.CreateWireMockAdminClient();
        SvcKlineServerMock = _svcKlineWireMockContainer.CreateWireMockAdminClient();

        // Set up RabbitMQ connection factory for messaging tests
        RabbitMqConnectionFactory = new ConnectionFactory
        {
            Uri = new Uri(_rabbitMqContainer.GetConnectionString()),
        };

        // Pre-warm the SignalR connection pool
        await PreWarmSignalRConnectionPoolAsync();
    }

    /// <summary>
    /// Pre-creates a few SignalR connections to improve test startup performance.
    /// </summary>
    private async Task PreWarmSignalRConnectionPoolAsync()
    {
        const int initialPoolSize = 3;
        var preWarmTasks = Enumerable
            .Range(0, initialPoolSize)
            .Select(async _ =>
            {
                var connection = await CreateSignalRConnectionAsync();
                _signalRConnectionPool.Enqueue(connection);
            });

        await Task.WhenAll(preWarmTasks);
    }

    /// <summary>
    /// Gets a SignalR connection from the pool, creating one if necessary.
    /// This dramatically improves test performance by reusing connections.
    /// </summary>
    /// <returns>A configured and connected SignalR hub connection.</returns>
    public async Task<HubConnection> GetPooledSignalRConnectionAsync()
    {
        if (
            _signalRConnectionPool.TryDequeue(out var pooledConnection)
            && pooledConnection.State == HubConnectionState.Connected
        )
        {
            return pooledConnection;
        }

        // Create new connection if pool is empty or connection is stale
        return await CreateSignalRConnectionAsync();
    }

    /// <summary>
    /// Creates a SignalR connection for integration tests.
    /// </summary>
    /// <returns>A configured SignalR hub connection.</returns>
    public async Task<HubConnection> CreateSignalRConnectionAsync()
    {
        var connection = new HubConnectionBuilder()
            .WithUrl(
                $"{Server.BaseAddress}hubs/crypto",
                options =>
                {
                    options.HttpMessageHandlerFactory = _ => Server.CreateHandler();
                }
            )
            .Build();

        await connection.StartAsync();

        // Track all connections for proper disposal
        await _connectionSemaphore.WaitAsync();
        try
        {
            _allConnections.Add(connection);
        }
        finally
        {
            _connectionSemaphore.Release();
        }

        return connection;
    }

    /// <summary>
    /// Returns a SignalR connection to the pool for reuse.
    /// </summary>
    /// <param name="connection">The connection to return to the pool.</param>
    public void ReturnSignalRConnectionToPool(HubConnection connection)
    {
        if (connection.State == HubConnectionState.Connected)
        {
            _signalRConnectionPool.Enqueue(connection);
        }
    }

    public new async Task DisposeAsync()
    {
        // Dispose all SignalR connections
        await _connectionSemaphore.WaitAsync();
        try
        {
            var disposeTasks = _allConnections.Select(async connection =>
            {
                try
                {
                    if (connection.State == HubConnectionState.Connected)
                    {
                        await connection.StopAsync();
                    }

                    await connection.DisposeAsync();
                }
                catch
                {
                    // Ignore disposal errors
                }
            });

            await Task.WhenAll(disposeTasks);
            _allConnections.Clear();
        }
        finally
        {
            _connectionSemaphore.Release();
        }

        await _svcCoinsWireMockContainer.DisposeAsync();
        await _svcExternalWireMockContainer.DisposeAsync();
        await _svcKlineWireMockContainer.DisposeAsync();
        await _rabbitMqContainer.DisposeAsync();
        await base.DisposeAsync();

        _connectionSemaphore.Dispose();
    }
}
