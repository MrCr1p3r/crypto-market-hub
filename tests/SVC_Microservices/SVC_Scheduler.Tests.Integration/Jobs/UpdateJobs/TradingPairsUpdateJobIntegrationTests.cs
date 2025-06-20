using System.Text.Json;
using SharedLibrary.Constants;
using SVC_Scheduler.Jobs.UpdateJobs;
using WireMock.Admin.Mappings;

namespace SVC_Scheduler.Tests.Integration.Jobs.UpdateJobs;

[Collection("Scheduler Integration Tests")]
public class TradingPairsUpdateJobIntegrationTests(CustomWebApplicationFactory factory)
    : BaseSchedulerIntegrationTest(factory)
{
    [Fact]
    public async Task Invoke_WithSuccessfulUpdate_ShouldPublishSuccessMessage()
    {
        // Arrange
        await Factory.SvcBridgeServerMock.PostMappingAsync(
            WireMockMappings.SvcBridge.UpdateTradingPairsSuccess
        );

        var job = GetRequiredService<TradingPairsUpdateJob>();

        // Act
        await job.Invoke();

        // Assert
        var messageReceived = await WaitForJobCompletionMessageAsync(
            JobConstants.Names.TradingPairsUpdate,
            expectedSuccess: true,
            TimeSpan.FromSeconds(10)
        );
        messageReceived
            .Should()
            .BeTrue("Trading pairs update message should be published on successful update");

        var message = GetPublishedMessage(JobConstants.Names.TradingPairsUpdate);
        message.Should().NotBeNull();
        message!.Success.Should().BeTrue();
        message.JobType.Should().Be(JobConstants.Types.DataSync);
        message.Source.Should().Be(JobConstants.Sources.Scheduler);
        message.Data.Should().NotBeNull();
        message.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));

        // Verify routing key was correct
        Factory
            .PublishedRoutingKeys.Should()
            .Contain(JobConstants.RoutingKeys.TradingPairsUpdated);

        // Verify data structure - should be array of Coin with trading pairs
        var coinsArray = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(
            JsonSerializer.Serialize(message.Data)
        );
        coinsArray.Should().HaveCount(2);

        var firstCoin = coinsArray![0];
        firstCoin.Should().ContainKey("Id");
        firstCoin.Should().ContainKey("Symbol");
        firstCoin.Should().ContainKey("Name");
        firstCoin.Should().ContainKey("TradingPairs");

        // Verify trading pairs structure
        var tradingPairsJson = JsonSerializer.Serialize(firstCoin["TradingPairs"]);
        var tradingPairs = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(
            tradingPairsJson
        );
        tradingPairs.Should().NotBeEmpty();
        tradingPairs![0].Should().ContainKey("Id");
    }

    [Fact]
    public async Task Invoke_WithEmptyTradingPairs_ShouldPublishSuccessMessageWithEmptyData()
    {
        // Arrange
        await Factory.SvcBridgeServerMock.PostMappingAsync(
            WireMockMappings.SvcBridge.UpdateTradingPairsEmpty
        );

        var job = GetRequiredService<TradingPairsUpdateJob>();

        // Act
        await job.Invoke();

        // Assert
        var messageReceived = await WaitForJobCompletionMessageAsync(
            JobConstants.Names.TradingPairsUpdate,
            expectedSuccess: true,
            TimeSpan.FromSeconds(10)
        );
        messageReceived
            .Should()
            .BeTrue("Trading pairs update message should be published even with empty data");

        var message = GetPublishedMessage(JobConstants.Names.TradingPairsUpdate);
        message.Should().NotBeNull();
        message!.Success.Should().BeTrue();
        message.Data.Should().NotBeNull();

        // Verify empty data structure
        var coinsArray = JsonSerializer.Deserialize<List<object>>(
            JsonSerializer.Serialize(message.Data)
        );
        coinsArray.Should().BeEmpty();
    }

    [Fact]
    public async Task Invoke_WhenBridgeServiceFails_ShouldPublishErrorMessage()
    {
        // Arrange
        await Factory.SvcBridgeServerMock.PostMappingAsync(
            WireMockMappings.SvcBridge.UpdateTradingPairsError
        );

        var job = GetRequiredService<TradingPairsUpdateJob>();

        // Act
        await job.Invoke();

        // Assert
        var messageReceived = await WaitForJobCompletionMessageAsync(
            JobConstants.Names.TradingPairsUpdate,
            expectedSuccess: false,
            TimeSpan.FromSeconds(10)
        );
        messageReceived
            .Should()
            .BeTrue("Error message should be published when bridge service fails");

        var message = GetPublishedMessage(JobConstants.Names.TradingPairsUpdate, success: false);
        message.Should().NotBeNull();
        message!.Success.Should().BeFalse();
        message.ErrorMessage.Should().NotBeNullOrEmpty();
        message.Data.Should().BeNull();

        // Verify routing key was correct even for error
        Factory
            .PublishedRoutingKeys.Should()
            .Contain(JobConstants.RoutingKeys.TradingPairsUpdated);
    }

    private static class WireMockMappings
    {
        public static class SvcBridge
        {
            public static MappingModel UpdateTradingPairsSuccess =>
                new()
                {
                    Request = new RequestModel
                    {
                        Path = "/bridge/trading-pairs",
                        Methods = ["POST"],
                    },
                    Response = new ResponseModel
                    {
                        StatusCode = 200,
                        Headers = new Dictionary<string, object>
                        {
                            ["Content-Type"] = "application/json",
                        },
                        BodyAsJson = TestData.ValidTradingPairsResponse,
                    },
                };

            public static MappingModel UpdateTradingPairsEmpty =>
                new()
                {
                    Request = new RequestModel
                    {
                        Path = "/bridge/trading-pairs",
                        Methods = ["POST"],
                    },
                    Response = new ResponseModel
                    {
                        StatusCode = 200,
                        Headers = new Dictionary<string, object>
                        {
                            ["Content-Type"] = "application/json",
                        },
                        BodyAsJson = new List<object>(),
                    },
                };

            public static MappingModel UpdateTradingPairsError =>
                new()
                {
                    Request = new RequestModel
                    {
                        Path = "/bridge/trading-pairs",
                        Methods = ["POST"],
                    },
                    Response = new ResponseModel
                    {
                        StatusCode = 500,
                        Headers = new Dictionary<string, object>
                        {
                            ["Content-Type"] = "application/json",
                        },
                        BodyAsJson = new
                        {
                            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                            Title = "Internal Server Error",
                            Status = 500,
                            Detail = "An error occurred while processing the request",
                            Instance = "/bridge/trading-pairs",
                        },
                    },
                };
        }
    }

    private static class TestData
    {
        public static readonly List<dynamic> ValidTradingPairsResponse =
        [
            new
            {
                Id = 1,
                Symbol = "BTC",
                Name = "Bitcoin",
                Category = (string?)null,
                IdCoinGecko = "bitcoin",
                MarketCapUsd = 1_200_000_000_000L,
                PriceUsd = "50000.00",
                PriceChangePercentage24h = 3.5m,
                TradingPairs = new[]
                {
                    new
                    {
                        Id = 101,
                        CoinQuote = new
                        {
                            Id = 5,
                            Symbol = "USDT",
                            Name = "Tether",
                            Category = 0, // Stablecoin
                            IdCoinGecko = "tether",
                        },
                        Exchanges = new[] { 1 }, // Binance
                    },
                },
            },
            new
            {
                Id = 2,
                Symbol = "ETH",
                Name = "Ethereum",
                Category = (string?)null,
                IdCoinGecko = "ethereum",
                MarketCapUsd = 600_000_000_000L,
                PriceUsd = "3500.00",
                PriceChangePercentage24h = -1.2m,
                TradingPairs = new[]
                {
                    new
                    {
                        Id = 102,
                        CoinQuote = new
                        {
                            Id = 5,
                            Symbol = "USDT",
                            Name = "Tether",
                            Category = 0, // Stablecoin
                            IdCoinGecko = "tether",
                        },
                        Exchanges = new[] { 1 }, // Binance
                    },
                },
            },
        ];
    }
}
