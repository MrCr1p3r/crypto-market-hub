using System.Text.Json;
using SharedLibrary.Constants;
using SVC_Scheduler.Jobs.UpdateJobs;
using WireMock.Admin.Mappings;

namespace SVC_Scheduler.Tests.Integration.Jobs.UpdateJobs;

[Collection("Scheduler Integration Tests")]
public class MarketDataUpdateJobIntegrationTests(CustomWebApplicationFactory factory)
    : BaseSchedulerIntegrationTest(factory)
{
    [Fact]
    public async Task Invoke_WithSuccessfulUpdate_ShouldPublishSuccessMessage()
    {
        // Arrange
        await Factory.SvcBridgeServerMock.PostMappingAsync(
            WireMockMappings.SvcBridge.UpdateMarketDataSuccess
        );

        var job = GetRequiredService<MarketDataUpdateJob>();

        // Act
        await job.Invoke();

        // Assert
        var messageReceived = await WaitForJobCompletionMessageAsync(
            JobConstants.Names.MarketDataUpdate,
            expectedSuccess: true,
            TimeSpan.FromSeconds(10)
        );
        messageReceived
            .Should()
            .BeTrue("Market data update message should be published on successful update");

        var message = GetPublishedMessage(JobConstants.Names.MarketDataUpdate);
        message.Should().NotBeNull();
        message!.Success.Should().BeTrue();
        message.JobType.Should().Be(JobConstants.Types.DataSync);
        message.Source.Should().Be(JobConstants.Sources.Scheduler);
        message.Data.Should().NotBeNull();
        message.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));

        // Verify routing key was correct
        Factory.PublishedRoutingKeys.Should().Contain(JobConstants.RoutingKeys.MarketDataUpdated);

        // Verify data structure - should be array of CoinMarketData
        var marketDataArray = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(
            JsonSerializer.Serialize(message.Data)
        );
        marketDataArray.Should().HaveCount(2);

        var firstMarketData = marketDataArray![0];
        firstMarketData.Should().ContainKey("Id");
        firstMarketData.Should().ContainKey("MarketCapUsd");
        firstMarketData.Should().ContainKey("PriceUsd");
        firstMarketData.Should().ContainKey("PriceChangePercentage24h");
    }

    [Fact]
    public async Task Invoke_WithEmptyMarketData_ShouldPublishSuccessMessageWithEmptyData()
    {
        // Arrange
        await Factory.SvcBridgeServerMock.PostMappingAsync(
            WireMockMappings.SvcBridge.UpdateMarketDataEmpty
        );

        var job = GetRequiredService<MarketDataUpdateJob>();

        // Act
        await job.Invoke();

        // Assert
        var messageReceived = await WaitForJobCompletionMessageAsync(
            JobConstants.Names.MarketDataUpdate,
            expectedSuccess: true,
            TimeSpan.FromSeconds(10)
        );
        messageReceived
            .Should()
            .BeTrue("Market data update message should be published even with empty data");

        var message = GetPublishedMessage(JobConstants.Names.MarketDataUpdate);
        message.Should().NotBeNull();
        message!.Success.Should().BeTrue();
        message.Data.Should().NotBeNull();

        // Verify empty data structure
        var marketDataArray = JsonSerializer.Deserialize<List<object>>(
            JsonSerializer.Serialize(message.Data)
        );
        marketDataArray.Should().BeEmpty();
    }

    [Fact]
    public async Task Invoke_WhenBridgeServiceFails_ShouldPublishErrorMessage()
    {
        // Arrange
        await Factory.SvcBridgeServerMock.PostMappingAsync(
            WireMockMappings.SvcBridge.UpdateMarketDataError
        );

        var job = GetRequiredService<MarketDataUpdateJob>();

        // Act
        await job.Invoke();

        // Assert
        var messageReceived = await WaitForJobCompletionMessageAsync(
            JobConstants.Names.MarketDataUpdate,
            expectedSuccess: false,
            TimeSpan.FromSeconds(10)
        );
        messageReceived
            .Should()
            .BeTrue("Error message should be published when bridge service fails");

        var message = GetPublishedMessage(JobConstants.Names.MarketDataUpdate, success: false);
        message.Should().NotBeNull();
        message!.Success.Should().BeFalse();
        message.ErrorMessage.Should().NotBeNullOrEmpty();
        message.Data.Should().BeNull();

        // Verify routing key was correct even for error
        Factory.PublishedRoutingKeys.Should().Contain(JobConstants.RoutingKeys.MarketDataUpdated);
    }

    private static class WireMockMappings
    {
        public static class SvcBridge
        {
            public static MappingModel UpdateMarketDataSuccess =>
                new()
                {
                    Request = new RequestModel
                    {
                        Path = "/bridge/coins/market-data",
                        Methods = ["POST"],
                    },
                    Response = new ResponseModel
                    {
                        StatusCode = 200,
                        Headers = new Dictionary<string, object>
                        {
                            ["Content-Type"] = "application/json",
                        },
                        BodyAsJson = TestData.ValidMarketDataResponse,
                    },
                };

            public static MappingModel UpdateMarketDataEmpty =>
                new()
                {
                    Request = new RequestModel
                    {
                        Path = "/bridge/coins/market-data",
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

            public static MappingModel UpdateMarketDataError =>
                new()
                {
                    Request = new RequestModel
                    {
                        Path = "/bridge/coins/market-data",
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
                            type = "https://datatracker.ietf.org/doc/html/rfc9110#section-15.6.1",
                            title = "Internal Server Error",
                            status = 500,
                            detail = "An error occurred while updating market data.",
                            instance = "/bridge/coins/market-data",
                        },
                    },
                };
        }
    }

    private static class TestData
    {
        public static readonly List<dynamic> ValidMarketDataResponse =
        [
            new
            {
                Id = 1,
                MarketCapUsd = 1_200_000_000_000L,
                PriceUsd = "50000.00",
                PriceChangePercentage24h = 3.5m,
            },
            new
            {
                Id = 2,
                MarketCapUsd = 600_000_000_000L,
                PriceUsd = "3500.00",
                PriceChangePercentage24h = -1.2m,
            },
        ];
    }
}
