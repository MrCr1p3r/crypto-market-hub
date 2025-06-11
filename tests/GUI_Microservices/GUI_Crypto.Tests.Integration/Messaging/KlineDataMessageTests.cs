using GUI_Crypto.ServiceModels.Messaging;
using SharedLibrary.Constants;
using SharedLibrary.Models;

namespace GUI_Crypto.Tests.Integration.Messaging;

/// <summary>
/// Integration tests for KlineDataMessageHandler testing the full messaging pipeline:
/// RabbitMQ Message → MessageHandler → SignalR Hub → Connected Clients.
/// </summary>
[Collection("Messaging Integration Tests")]
public class KlineDataMessageTests(CustomWebApplicationFactory factory)
    : BaseMessagingIntegrationTest(factory)
{
    #region Happy Path Tests

    [Fact]
    public async Task KlineDataMessage_WithValidData_ShouldBroadcastToSignalRClients()
    {
        // Arrange
        var connection = await GetSignalRConnection();
        var listener = CreateSignalRListener<IEnumerable<KlineData>>(
            connection,
            "ReceiveKlineDataUpdate"
        );

        var testData = TestData.ValidKlineData;

        // Act - Publish message to RabbitMQ (simulating SVC_Scheduler)
        await MessagePublisher.PublishJobCompletedMessageAsync(
            JobConstants.QueueNames.GuiKlineDataUpdated,
            testData
        );

        // Wait for the message to be received
        await listener.WaitAsync();

        // Assert
        listener
            .ReceivedMessages.Should()
            .HaveCount(1, "exactly one kline data update should be received");

        var received = listener.FirstMessage.ToList();
        received.Should().HaveCount(2, "test data contains 2 trading pairs");

        // Verify BTC/USDT trading pair data
        var btcPairData = received.First(pair => pair.IdTradingPair == 101);
        btcPairData.Klines.Should().HaveCount(2, "BTC pair should have 2 klines");

        var firstBtcKline = btcPairData.Klines.First();
        firstBtcKline.OpenPrice.Should().Be(46000.50m);
        firstBtcKline.ClosePrice.Should().Be(46800.00m);
        firstBtcKline.Volume.Should().Be(123.456m);

        // Verify ETH/USDT trading pair data
        var ethPairData = received.First(pair => pair.IdTradingPair == 102);
        ethPairData.Klines.Should().HaveCount(2, "ETH pair should have 2 klines");

        var firstEthKline = ethPairData.Klines.First();
        firstEthKline.OpenPrice.Should().Be(3000.00m);
        firstEthKline.ClosePrice.Should().Be(3050.00m);
        firstEthKline.Volume.Should().Be(200.000m);
    }

    [Fact]
    public async Task KlineDataMessage_WithMultipleClients_ShouldBroadcastToAllClients()
    {
        // Arrange
        const int clientCount = 3;
        var connections = (await GetMultipleSignalRConnections(clientCount)).ToList();
        var listeners = connections
            .Select(c => CreateSignalRListener<IEnumerable<KlineData>>(c, "ReceiveKlineDataUpdate"))
            .ToList();

        var testData = TestData.ValidKlineData;

        // Act
        await MessagePublisher.PublishJobCompletedMessageAsync(
            JobConstants.QueueNames.GuiKlineDataUpdated,
            testData
        );

        // Wait for all clients to receive the message
        await Task.WhenAll(listeners.Select(l => l.WaitAsync()));

        // Assert - All clients should receive the same update
        listeners
            .Should()
            .AllSatisfy(listener =>
            {
                listener
                    .ReceivedMessages.Should()
                    .HaveCount(1, "each client should receive exactly one update");
                listener
                    .FirstMessage.Should()
                    .HaveCount(2, "each update should contain 2 trading pairs");
            });
    }

    [Fact]
    public async Task KlineDataMessage_WithSingleTradingPair_ShouldBroadcastCorrectly()
    {
        // Arrange
        var connection = await GetSignalRConnection();
        var listener = CreateSignalRListener<IEnumerable<KlineData>>(
            connection,
            "ReceiveKlineDataUpdate"
        );

        var singlePairData = new List<KlineData>
        {
            new() { IdTradingPair = 101, Klines = TestData.BtcKlines },
        };

        // Act
        await MessagePublisher.PublishJobCompletedMessageAsync(
            JobConstants.QueueNames.GuiKlineDataUpdated,
            singlePairData
        );

        await listener.WaitAsync();

        // Assert
        listener.ReceivedMessages.Should().HaveCount(1);
        var received = listener.FirstMessage.ToList();
        received.Should().HaveCount(1, "only one trading pair was sent");
        received[0].IdTradingPair.Should().Be(101);
        received[0].Klines.Should().HaveCount(2);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task KlineDataMessage_WithFailedJob_ShouldNotBroadcastToClients()
    {
        // Arrange
        var connection = await GetSignalRConnection();
        var listener = CreateSignalRListener<IEnumerable<KlineData>>(
            connection,
            "ReceiveKlineDataUpdate"
        );

        // Act - Publish failed job message
        await MessagePublisher.PublishJobFailedMessageAsync(
            JobConstants.QueueNames.GuiKlineDataUpdated,
            "Kline data fetch failed"
        );

        // Assert - No broadcast should occur for failed jobs
        listener
            .ReceivedMessages.Should()
            .BeEmpty("failed job messages should not trigger broadcasts");
    }

    [Fact]
    public async Task KlineDataMessage_WithNullData_ShouldNotBroadcastToClients()
    {
        // Arrange
        var connection = await GetSignalRConnection();
        var listener = CreateSignalRListener<IEnumerable<KlineData>>(
            connection,
            "ReceiveKlineDataUpdate"
        );

        // Act - Publish message with null data
        await MessagePublisher.PublishJobCompletedMessageAsync<object?>(
            JobConstants.QueueNames.GuiKlineDataUpdated,
            null
        );

        // Assert - No broadcast should occur for null data
        listener
            .ReceivedMessages.Should()
            .BeEmpty("messages with null data should not trigger broadcasts");
    }

    [Fact]
    public async Task KlineDataMessage_WithEmptyData_ShouldBroadcastEmptyCollection()
    {
        // Arrange
        var connection = await GetSignalRConnection();
        var listener = CreateSignalRListener<IEnumerable<KlineData>>(
            connection,
            "ReceiveKlineDataUpdate"
        );

        var emptyData = Array.Empty<KlineData>();

        // Act
        await MessagePublisher.PublishJobCompletedMessageAsync(
            JobConstants.QueueNames.GuiKlineDataUpdated,
            emptyData
        );

        await listener.WaitAsync();

        // Assert
        listener
            .ReceivedMessages.Should()
            .HaveCount(1, "empty data should still trigger a broadcast");
        listener.FirstMessage.Should().BeEmpty("the broadcast should contain an empty collection");
    }

    [Fact]
    public async Task KlineDataMessage_WithEmptyKlinesForTradingPair_ShouldBroadcastCorrectly()
    {
        // Arrange
        var connection = await GetSignalRConnection();
        var listener = CreateSignalRListener<IEnumerable<KlineData>>(
            connection,
            "ReceiveKlineDataUpdate"
        );

        var dataWithEmptyKlines = new List<KlineData>
        {
            new()
            {
                IdTradingPair = 101,
                Klines = [], // Empty klines collection
            },
        };

        // Act
        await MessagePublisher.PublishJobCompletedMessageAsync(
            JobConstants.QueueNames.GuiKlineDataUpdated,
            dataWithEmptyKlines
        );

        await listener.WaitAsync();

        // Assert
        listener.ReceivedMessages.Should().HaveCount(1);
        var received = listener.FirstMessage.ToList();
        received.Should().HaveCount(1);
        received[0].IdTradingPair.Should().Be(101);
        received[0].Klines.Should().BeEmpty("klines collection should be empty");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task KlineDataMessage_WithNoConnectedClients_ShouldNotThrowError()
    {
        // Arrange - No SignalR clients connected
        var testData = TestData.ValidKlineData;

        // Act & Assert - Should not throw an exception
        var act = async () =>
        {
            await MessagePublisher.PublishJobCompletedMessageAsync(
                JobConstants.QueueNames.GuiKlineDataUpdated,
                testData
            );
        };

        await act.Should().NotThrowAsync("broadcasting to no clients should not cause errors");
    }

    [Fact]
    public async Task KlineDataMessage_WithClientDisconnectingDuringProcessing_ShouldNotThrowError()
    {
        // Arrange
        var connection = await GetSignalRConnection();
        var listener = CreateSignalRListener<IEnumerable<KlineData>>(
            connection,
            "ReceiveKlineDataUpdate"
        );

        var testData = TestData.ValidKlineData;

        // Act - Disconnect client before message processing
        await connection.DisposeAsync();

        var act = async () =>
        {
            await MessagePublisher.PublishJobCompletedMessageAsync(
                JobConstants.QueueNames.GuiKlineDataUpdated,
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
        public static readonly List<Kline> BtcKlines =
        [
            new()
            {
                OpenTime = 1640995200000L,
                OpenPrice = 46000.50m,
                HighPrice = 47000.75m,
                LowPrice = 45500.25m,
                ClosePrice = 46800.00m,
                Volume = 123.456m,
                CloseTime = 1640998800000L,
            },
            new()
            {
                OpenTime = 1640998800000L,
                OpenPrice = 46800.00m,
                HighPrice = 48000.00m,
                LowPrice = 46500.00m,
                ClosePrice = 47500.50m,
                Volume = 234.567m,
                CloseTime = 1641002400000L,
            },
        ];

        public static readonly List<Kline> EthKlines =
        [
            new()
            {
                OpenTime = 1640995200000L,
                OpenPrice = 3000.00m,
                HighPrice = 3100.00m,
                LowPrice = 2900.00m,
                ClosePrice = 3050.00m,
                Volume = 200.000m,
                CloseTime = 1640998800000L,
            },
            new()
            {
                OpenTime = 1640998800000L,
                OpenPrice = 3050.00m,
                HighPrice = 3200.00m,
                LowPrice = 3000.00m,
                ClosePrice = 3150.00m,
                Volume = 250.000m,
                CloseTime = 1641002400000L,
            },
        ];

        public static readonly List<KlineData> ValidKlineData =
        [
            new()
            {
                IdTradingPair = 101, // BTC/USDT
                Klines = BtcKlines,
            },
            new()
            {
                IdTradingPair = 102, // ETH/USDT
                Klines = EthKlines,
            },
        ];
    }

    #endregion
}
