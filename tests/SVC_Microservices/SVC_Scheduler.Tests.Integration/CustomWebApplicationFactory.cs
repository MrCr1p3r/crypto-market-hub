using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SharedLibrary.Constants;
using SharedLibrary.Models.Messaging;
using Testcontainers.RabbitMq;
using WireMock.Client;
using WireMock.Net.Testcontainers;

namespace SVC_Scheduler.Tests.Integration;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly WireMockContainer _svcExternalWireMockContainer =
        new WireMockContainerBuilder().Build();

    private readonly WireMockContainer _svcBridgeWireMockContainer =
        new WireMockContainerBuilder().Build();

    private readonly RabbitMqContainer _rabbitMqContainer = new RabbitMqBuilder().Build();

    private readonly ConcurrentQueue<JobCompletedMessage> _publishedMessages = new();
    private readonly ConcurrentQueue<string> _publishedRoutingKeys = new();

    public IWireMockAdminApi SvcExternalServerMock { get; private set; } = null!;

    public IWireMockAdminApi SvcBridgeServerMock { get; private set; } = null!;

    public IReadOnlyCollection<JobCompletedMessage> PublishedMessages => [.. _publishedMessages];

    public IReadOnlyCollection<string> PublishedRoutingKeys => [.. _publishedRoutingKeys];

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        StartTestcontainers();
        ConfigureApp(builder);
        CreateWireMockClients();
        SetupMessageConsumer().GetAwaiter().GetResult();
    }

    private void StartTestcontainers()
    {
        Task.WhenAll(
                _svcExternalWireMockContainer.StartAsync(),
                _svcBridgeWireMockContainer.StartAsync(),
                _rabbitMqContainer.StartAsync()
            )
            .GetAwaiter()
            .GetResult();
    }

    private void ConfigureApp(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(config =>
        {
            var inMemorySettings = new Dictionary<string, string?>
            {
                ["Services:SvcExternalClient:BaseUrl"] =
                    _svcExternalWireMockContainer.GetPublicUrl(),
                ["Services:SvcBridgeClient:BaseUrl"] = _svcBridgeWireMockContainer.GetPublicUrl(),
                ["ConnectionStrings:rabbitmq"] = _rabbitMqContainer.GetConnectionString(),
            };
            config.AddInMemoryCollection(inMemorySettings);
        });
    }

    private void CreateWireMockClients()
    {
        SvcExternalServerMock = _svcExternalWireMockContainer.CreateWireMockAdminClient();
        SvcBridgeServerMock = _svcBridgeWireMockContainer.CreateWireMockAdminClient();
    }

    private async Task SetupMessageConsumer()
    {
        var factory = new ConnectionFactory
        {
            Uri = new Uri(_rabbitMqContainer.GetConnectionString()),
        };

        var connection = await factory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();

        // Declare exchange and queue for testing
        await channel.ExchangeDeclareAsync(
            JobConstants.Exchanges.CryptoScheduler,
            ExchangeType.Topic,
            durable: true
        );
        var queueResult = await channel.QueueDeclareAsync(
            queue: "test.job.events",
            durable: false,
            exclusive: true,
            autoDelete: true
        );

        await channel.QueueBindAsync(
            queue: queueResult.QueueName,
            exchange: JobConstants.Exchanges.CryptoScheduler,
            routingKey: "#"
        );

        // Setup consumer
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var messageJson = Encoding.UTF8.GetString(body);
                var message = JsonSerializer.Deserialize<JobCompletedMessage>(messageJson);

                if (message != null)
                {
                    _publishedMessages.Enqueue(message);
                    _publishedRoutingKeys.Enqueue(ea.RoutingKey);
                }

                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
            }
            catch
            {
                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
            }
        };

        await channel.BasicConsumeAsync(
            queue: queueResult.QueueName,
            autoAck: false,
            consumer: consumer
        );
    }

    public void ClearPublishedMessages()
    {
        _publishedMessages.Clear();
        _publishedRoutingKeys.Clear();
    }

    public async Task<bool> WaitForMessageAsync(
        Func<JobCompletedMessage, bool> predicate,
        TimeSpan? timeout = null
    )
    {
        var timeoutValue = timeout ?? TimeSpan.FromSeconds(5);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        while (stopwatch.Elapsed < timeoutValue)
        {
            if (_publishedMessages.Any(predicate))
            {
                return true;
            }

            await Task.Delay(100);
        }

        return false;
    }

    public override async ValueTask DisposeAsync()
    {
        await _svcExternalWireMockContainer.DisposeAsync();
        await _svcBridgeWireMockContainer.DisposeAsync();
        await _rabbitMqContainer.DisposeAsync();

        await base.DisposeAsync();
    }
}
