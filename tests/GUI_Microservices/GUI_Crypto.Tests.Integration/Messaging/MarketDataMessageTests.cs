using GUI_Crypto.ServiceModels.Messaging;
using SharedLibrary.Constants;

namespace GUI_Crypto.Tests.Integration.Messaging;

/// <summary>
/// Integration tests for MarketDataMessageHandler testing the full messaging pipeline:
/// RabbitMQ Message → MessageHandler → SignalR Hub → Connected Clients.
/// </summary>
[Collection("Messaging Integration Tests")]
public class MarketDataMessageTests(CustomWebApplicationFactory factory)
    : BaseMessagingIntegrationTest(factory)
{
    #region Happy Path Tests

    [Fact]
    public async Task MarketDataMessage_WithValidData_ShouldBroadcastToSignalRClients()
    {
        // Arrange
        var connection = await GetSignalRConnection();
        var listener = CreateSignalRListener<IEnumerable<CoinMarketData>>(
            connection,
            "ReceiveMarketDataUpdate"
        );

        var testData = TestData.ValidMarketData;

        // Act - Publish message to RabbitMQ (simulating SVC_Scheduler)
        await MessagePublisher.PublishJobCompletedMessageAsync(
            JobConstants.QueueNames.GuiMarketDataUpdated,
            testData
        );

        // Wait for the message to be received
        await listener.WaitAsync();

        // Assert
        listener
            .ReceivedMessages.Should()
            .HaveCount(1, "exactly one market data update should be received");

        var received = listener.FirstMessage.ToList();
        received.Should().HaveCount(2, "test data contains 2 coins");

        // Verify Bitcoin data
        var btcData = received.First(coin => coin.Id == 1);
        btcData.MarketCapUsd.Should().Be(1_200_000_000_000);
        btcData.PriceUsd.Should().Be("50000.00");
        btcData.PriceChangePercentage24h.Should().Be(3.5m);

        // Verify Ethereum data
        var ethData = received.First(coin => coin.Id == 2);
        ethData.MarketCapUsd.Should().Be(600_000_000_000);
        ethData.PriceUsd.Should().Be("3500.00");
        ethData.PriceChangePercentage24h.Should().Be(-1.2m);
    }

    [Fact]
    public async Task MarketDataMessage_WithMultipleClients_ShouldBroadcastToAllClients()
    {
        // Arrange
        const int clientCount = 3;
        var connections = (await GetMultipleSignalRConnections(clientCount)).ToList();
        var listeners = connections
            .Select(c =>
                CreateSignalRListener<IEnumerable<CoinMarketData>>(c, "ReceiveMarketDataUpdate")
            )
            .ToList();

        var testData = TestData.ValidMarketData;

        // Act
        await MessagePublisher.PublishJobCompletedMessageAsync(
            JobConstants.QueueNames.GuiMarketDataUpdated,
            testData
        );

        await Task.WhenAll(listeners.Select(l => l.WaitAsync()));

        // Assert - All clients should receive the same update
        listeners
            .Should()
            .AllSatisfy(listener =>
            {
                listener
                    .ReceivedMessages.Should()
                    .HaveCount(1, "each client should receive exactly one update");
                listener.FirstMessage.Should().HaveCount(2, "each update should contain 2 coins");
            });
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task MarketDataMessage_WithFailedJob_ShouldNotBroadcastToClients()
    {
        // Arrange
        var connection = await GetSignalRConnection();
        var listener = CreateSignalRListener<IEnumerable<CoinMarketData>>(
            connection,
            "ReceiveMarketDataUpdate"
        );

        // Act - Publish failed job message
        await MessagePublisher.PublishJobFailedMessageAsync(
            JobConstants.QueueNames.GuiMarketDataUpdated,
            "Test error occurred"
        );

        // Assert - No broadcast should occur for failed jobs
        listener
            .ReceivedMessages.Should()
            .BeEmpty("failed job messages should not trigger broadcasts");
    }

    [Fact]
    public async Task MarketDataMessage_WithNullData_ShouldNotBroadcastToClients()
    {
        // Arrange
        var connection = await GetSignalRConnection();
        var listener = CreateSignalRListener<IEnumerable<CoinMarketData>>(
            connection,
            "ReceiveMarketDataUpdate"
        );

        // Act - Publish message with null data
        await MessagePublisher.PublishJobCompletedMessageAsync<object?>(
            JobConstants.QueueNames.GuiMarketDataUpdated,
            null
        );

        // Assert - No broadcast should occur for null data
        listener
            .ReceivedMessages.Should()
            .BeEmpty("messages with null data should not trigger broadcasts");
    }

    [Fact]
    public async Task MarketDataMessage_WithEmptyData_ShouldBroadcastEmptyCollection()
    {
        // Arrange
        var connection = await GetSignalRConnection();
        var listener = CreateSignalRListener<IEnumerable<CoinMarketData>>(
            connection,
            "ReceiveMarketDataUpdate"
        );

        var emptyData = Array.Empty<CoinMarketData>();

        // Act
        await MessagePublisher.PublishJobCompletedMessageAsync(
            JobConstants.QueueNames.GuiMarketDataUpdated,
            emptyData
        );

        await listener.WaitAsync();

        // Assert
        listener
            .ReceivedMessages.Should()
            .HaveCount(1, "empty data should still trigger a broadcast");
        listener.FirstMessage.Should().BeEmpty("the broadcast should contain an empty collection");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task MarketDataMessage_WithNoConnectedClients_ShouldNotThrowError()
    {
        // Arrange - No SignalR clients connected
        var testData = TestData.ValidMarketData;

        // Act & Assert - Should not throw an exception
        var act = async () =>
        {
            await MessagePublisher.PublishJobCompletedMessageAsync(
                JobConstants.QueueNames.GuiMarketDataUpdated,
                testData
            );
        };

        await act.Should().NotThrowAsync("broadcasting to no clients should not cause errors");
    }

    [Fact]
    public async Task MarketDataMessage_WithClientDisconnectingDuringProcessing_ShouldNotThrowError()
    {
        // Arrange
        var connection = await GetSignalRConnection();
        var listener = CreateSignalRListener<IEnumerable<CoinMarketData>>(
            connection,
            "ReceiveMarketDataUpdate"
        );

        var testData = TestData.ValidMarketData;

        // Act - Disconnect client before message processing
        await connection.DisposeAsync();

        var act = async () =>
        {
            await MessagePublisher.PublishJobCompletedMessageAsync(
                JobConstants.QueueNames.GuiMarketDataUpdated,
                testData
            );
        };

        // Assert - Should not throw even with disconnected clients
        await act.Should().NotThrowAsync("disconnected clients should not cause processing errors");
        listener.ReceivedMessages.Should().BeEmpty();
    }

    #endregion

    #region Test Data

    private static class TestData
    {
        public static readonly List<CoinMarketData> ValidMarketData =
        [
            new()
            {
                Id = 1,
                MarketCapUsd = 1_200_000_000_000,
                PriceUsd = "50000.00",
                PriceChangePercentage24h = 3.5m,
            },
            new()
            {
                Id = 2,
                MarketCapUsd = 600_000_000_000,
                PriceUsd = "3500.00",
                PriceChangePercentage24h = -1.2m,
            },
        ];
    }

    #endregion
}
