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
        return connection;
    }

    public new async Task DisposeAsync()
    {
        await _svcCoinsWireMockContainer.DisposeAsync();
        await _svcExternalWireMockContainer.DisposeAsync();
        await _svcKlineWireMockContainer.DisposeAsync();
        await _rabbitMqContainer.DisposeAsync();
        await base.DisposeAsync();
    }
}
