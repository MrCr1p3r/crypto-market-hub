using System.Text.Json;
using SVC_Scheduler.Jobs.CacheWarmup;
using WireMock.Admin.Mappings;

namespace SVC_Scheduler.Tests.Integration.Jobs.CacheWarmup;

[Collection("Scheduler Integration Tests")]
public class SpotCoinsCacheWarmupJobIntegrationTests(CustomWebApplicationFactory factory)
    : BaseSchedulerIntegrationTest(factory)
{
    [Fact]
    public async Task Invoke_WithValidDataFirstTime_ShouldPublishCacheWarmupMessage()
    {
        // Arrange
        await Factory.SvcExternalServerMock.PostMappingAsync(
            WireMockMappings.SvcExternal.GetSpotCoinsSuccess
        );

        // Reset static state to ensure first warmup
        ResetCacheWarmupState();

        var job = GetRequiredService<SpotCoinsCacheWarmupJob>();

        // Act
        await job.Invoke();

        // Assert
        var messageReceived = await WaitForCacheWarmupMessageAsync(TimeSpan.FromSeconds(10));
        messageReceived
            .Should()
            .BeTrue("Cache warmup message should be published on first successful warmup");

        var message = GetPublishedMessage("Spot Coins Cache Warmup");
        message.Should().NotBeNull();
        message!.Success.Should().BeTrue();
        message.Data.Should().NotBeNull();

        // The job publishes cache warmup metadata, not the actual coin data
        var warmupData = JsonSerializer.Deserialize<Dictionary<string, object>>(
            JsonSerializer.Serialize(message.Data)
        );
        warmupData.Should().ContainKey("CoinCount");
        warmupData.Should().ContainKey("IsFirstWarmup");
        warmupData!["IsFirstWarmup"].ToString().Should().Be("True");
    }

    [Fact]
    public async Task Invoke_SubsequentCall_ShouldNotPublishMessage()
    {
        // Arrange
        await Factory.SvcExternalServerMock.PostMappingAsync(
            WireMockMappings.SvcExternal.GetSpotCoinsSuccess
        );

        var job = GetRequiredService<SpotCoinsCacheWarmupJob>();

        // First call to set the warmup state
        await job.Invoke();
        await WaitForCacheWarmupMessageAsync();

        Factory.ClearPublishedMessages();

        // Act - Second call
        await job.Invoke();

        // Assert
        await Task.Delay(TimeSpan.FromSeconds(2)); // Wait to ensure no message is published
        AssertNoMessagesPublished();
    }

    [Fact]
    public async Task Invoke_WhenExternalServiceFails_ShouldNotPublishMessage()
    {
        // Arrange
        await Factory.SvcExternalServerMock.PostMappingAsync(
            WireMockMappings.SvcExternal.GetSpotCoinsError
        );

        // Reset static state to ensure first warmup attempt
        ResetCacheWarmupState();

        var job = GetRequiredService<SpotCoinsCacheWarmupJob>();

        // Act
        await job.Invoke();

        // Assert
        await Task.Delay(TimeSpan.FromSeconds(2)); // Wait to ensure no message is published
        AssertNoMessagesPublished();
    }

    [Fact]
    public async Task Invoke_WithEmptyData_ShouldPublishCacheWarmupMessageWithEmptyData()
    {
        // Arrange
        await Factory.SvcExternalServerMock.PostMappingAsync(
            WireMockMappings.SvcExternal.GetSpotCoinsEmpty
        );

        // Reset static state to ensure first warmup
        ResetCacheWarmupState();

        var job = GetRequiredService<SpotCoinsCacheWarmupJob>();

        // Act
        await job.Invoke();

        // Assert
        var messageReceived = await WaitForCacheWarmupMessageAsync(TimeSpan.FromSeconds(10));
        messageReceived
            .Should()
            .BeTrue("Cache warmup message should be published even with empty data");

        var message = GetPublishedMessage("Spot Coins Cache Warmup");
        message.Should().NotBeNull();
        message!.Success.Should().BeTrue();
        message.Data.Should().NotBeNull();

        // The job publishes cache warmup metadata, not the actual coin data
        var warmupData = JsonSerializer.Deserialize<Dictionary<string, object>>(
            JsonSerializer.Serialize(message.Data)
        );
        warmupData.Should().ContainKey("CoinCount");
        warmupData.Should().ContainKey("IsFirstWarmup");
        warmupData!["IsFirstWarmup"].ToString().Should().Be("True");
    }

    [Fact]
    public async Task Invoke_ConcurrentCalls_ShouldHandleThreadSafety()
    {
        // Arrange
        await Factory.SvcExternalServerMock.PostMappingAsync(
            WireMockMappings.SvcExternal.GetSpotCoinsSuccess
        );

        // Reset static state to ensure first warmup
        ResetCacheWarmupState();

        var job = GetRequiredService<SpotCoinsCacheWarmupJob>();

        // Act - Execute multiple concurrent calls
        var tasks = Enumerable.Range(0, 5).Select(_ => job.Invoke()).ToArray();

        await Task.WhenAll(tasks);

        // Assert - Only one message should be published despite multiple concurrent calls
        await Task.Delay(TimeSpan.FromSeconds(3)); // Wait for any delayed messages

        var cacheWarmupMessages = Factory
            .PublishedMessages.Where(m => m.JobName.Contains("Cache Warmup"))
            .ToList();

        cacheWarmupMessages
            .Should()
            .HaveCount(
                1,
                "Only one cache warmup message should be published regardless of concurrent calls"
            );
    }

    private static void ResetCacheWarmupState()
    {
        // Use reflection to reset the static state for testing
        var jobType = typeof(SpotCoinsCacheWarmupJob);
        var field = jobType.GetField(
            "_firstWarmupCompleted",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static
        );
        field?.SetValue(null, false);
    }

    private static class WireMockMappings
    {
        public static class SvcExternal
        {
            public static MappingModel GetSpotCoinsSuccess =>
                new()
                {
                    Request = new RequestModel
                    {
                        Path = "/exchanges/coins/spot",
                        Methods = ["GET"],
                    },
                    Response = new ResponseModel
                    {
                        StatusCode = 200,
                        Headers = new Dictionary<string, object>
                        {
                            ["Content-Type"] = "application/json",
                        },
                        BodyAsJson = TestData.ValidSpotCoinsResponse,
                    },
                };

            public static MappingModel GetSpotCoinsError =>
                new()
                {
                    Request = new RequestModel
                    {
                        Path = "/exchanges/coins/spot",
                        Methods = ["GET"],
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
                            type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                            title = "Internal Server Error",
                            status = 500,
                            detail = "Internal server error occurred while retrieving spot coins.",
                            instance = "/exchanges/coins/spot",
                        },
                    },
                };

            public static MappingModel GetSpotCoinsEmpty =>
                new()
                {
                    Request = new RequestModel
                    {
                        Path = "/exchanges/coins/spot",
                        Methods = ["GET"],
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
        }
    }

    private static class TestData
    {
        public static readonly List<dynamic> ValidSpotCoinsResponse =
        [
            new
            {
                symbol = "BTC",
                name = "Bitcoin",
                category = (string?)null,
                idCoinGecko = "bitcoin",
                tradingPairs = new[]
                {
                    new
                    {
                        coinQuote = new
                        {
                            symbol = "USDT",
                            name = "Tether",
                            category = (string?)null,
                            idCoinGecko = "tether",
                        },
                        exchangeInfos = new[]
                        {
                            new
                            {
                                exchange = 1,
                                baseAsset = "BTC",
                                quoteAsset = "USDT",
                                status = 0,
                            },
                        },
                    },
                },
            },
            new
            {
                symbol = "ETH",
                name = "Ethereum",
                category = (string?)null,
                idCoinGecko = "ethereum",
                tradingPairs = new[]
                {
                    new
                    {
                        coinQuote = new
                        {
                            symbol = "USDT",
                            name = "Tether",
                            category = (string?)null,
                            idCoinGecko = "tether",
                        },
                        exchangeInfos = new[]
                        {
                            new
                            {
                                exchange = 1,
                                baseAsset = "ETH",
                                quoteAsset = "USDT",
                                status = 0,
                            },
                        },
                    },
                },
            },
            new
            {
                symbol = "BNB",
                name = "Binance Coin",
                category = (string?)null,
                idCoinGecko = "binancecoin",
                tradingPairs = new[]
                {
                    new
                    {
                        coinQuote = new
                        {
                            symbol = "USDT",
                            name = "Tether",
                            category = (string?)null,
                            idCoinGecko = "tether",
                        },
                        exchangeInfos = new[]
                        {
                            new
                            {
                                exchange = 1,
                                baseAsset = "BNB",
                                quoteAsset = "USDT",
                                status = 0,
                            },
                        },
                    },
                },
            },
        ];
    }
}
