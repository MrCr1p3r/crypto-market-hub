using System.Text.Json;
using SharedLibrary.Constants;
using SVC_Scheduler.Jobs.UpdateJobs;
using WireMock.Admin.Mappings;

namespace SVC_Scheduler.Tests.Integration.Jobs.UpdateJobs;

[Collection("Scheduler Integration Tests")]
public class KlineDataUpdateJobIntegrationTests(CustomWebApplicationFactory factory)
    : BaseSchedulerIntegrationTest(factory)
{
    [Fact]
    public async Task Invoke_WithSuccessfulUpdate_ShouldPublishSuccessMessage()
    {
        // Arrange
        await Factory.SvcBridgeServerMock.PostMappingAsync(
            WireMockMappings.SvcBridge.UpdateKlineDataSuccess
        );

        var job = GetRequiredService<KlineDataUpdateJob>();

        // Act
        await job.Invoke();

        // Assert
        var messageReceived = await WaitForJobCompletionMessageAsync(
            JobConstants.Names.KlineDataUpdate,
            expectedSuccess: true,
            TimeSpan.FromSeconds(10)
        );
        messageReceived
            .Should()
            .BeTrue("Kline data update message should be published on successful update");

        var message = GetPublishedMessage(JobConstants.Names.KlineDataUpdate);
        message.Should().NotBeNull();
        message!.Success.Should().BeTrue();
        message.JobType.Should().Be(JobConstants.Types.DataSync);
        message.Source.Should().Be(JobConstants.Sources.Scheduler);
        message.Data.Should().NotBeNull();
        message.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));

        // Verify routing key was correct
        Factory.PublishedRoutingKeys.Should().Contain(JobConstants.RoutingKeys.KlineDataUpdated);

        // Verify data structure - should be array of KlineDataResponse
        var klineDataArray = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(
            JsonSerializer.Serialize(message.Data)
        );
        klineDataArray.Should().HaveCount(2);

        var firstKlineData = klineDataArray![0];
        firstKlineData.Should().ContainKey("IdTradingPair");
        firstKlineData.Should().ContainKey("Klines");
    }

    [Fact]
    public async Task Invoke_WithEmptyKlineData_ShouldPublishSuccessMessageWithEmptyData()
    {
        // Arrange
        await Factory.SvcBridgeServerMock.PostMappingAsync(
            WireMockMappings.SvcBridge.UpdateKlineDataEmpty
        );

        var job = GetRequiredService<KlineDataUpdateJob>();

        // Act
        await job.Invoke();

        // Assert
        var messageReceived = await WaitForJobCompletionMessageAsync(
            JobConstants.Names.KlineDataUpdate,
            expectedSuccess: true,
            TimeSpan.FromSeconds(10)
        );
        messageReceived
            .Should()
            .BeTrue("Kline data update message should be published even with empty data");

        var message = GetPublishedMessage(JobConstants.Names.KlineDataUpdate);
        message.Should().NotBeNull();
        message!.Success.Should().BeTrue();
        message.Data.Should().NotBeNull();

        // Verify empty data structure
        var klineDataArray = JsonSerializer.Deserialize<List<object>>(
            JsonSerializer.Serialize(message.Data)
        );
        klineDataArray.Should().BeEmpty();
    }

    [Fact]
    public async Task Invoke_WhenBridgeServiceFails_ShouldPublishErrorMessage()
    {
        // Arrange
        await Factory.SvcBridgeServerMock.PostMappingAsync(
            WireMockMappings.SvcBridge.UpdateKlineDataError
        );

        var job = GetRequiredService<KlineDataUpdateJob>();

        // Act
        await job.Invoke();

        // Assert
        var messageReceived = await WaitForJobCompletionMessageAsync(
            JobConstants.Names.KlineDataUpdate,
            expectedSuccess: false,
            TimeSpan.FromSeconds(10)
        );
        messageReceived
            .Should()
            .BeTrue("Error message should be published when bridge service fails");

        var message = GetPublishedMessage(JobConstants.Names.KlineDataUpdate, success: false);
        message.Should().NotBeNull();
        message!.Success.Should().BeFalse();
        message.ErrorMessage.Should().NotBeNullOrEmpty();
        message.Data.Should().BeNull();

        // Verify routing key was correct even for error
        Factory.PublishedRoutingKeys.Should().Contain(JobConstants.RoutingKeys.KlineDataUpdated);
    }

    private static class WireMockMappings
    {
        public static class SvcBridge
        {
            public static MappingModel UpdateKlineDataSuccess =>
                new()
                {
                    Request = new RequestModel { Path = "/bridge/kline", Methods = ["POST"] },
                    Response = new ResponseModel
                    {
                        StatusCode = 200,
                        Headers = new Dictionary<string, object>
                        {
                            ["Content-Type"] = "application/json",
                        },
                        BodyAsJson = TestData.ValidKlineDataResponse,
                    },
                };

            public static MappingModel UpdateKlineDataEmpty =>
                new()
                {
                    Request = new RequestModel { Path = "/bridge/kline", Methods = ["POST"] },
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

            public static MappingModel UpdateKlineDataError =>
                new()
                {
                    Request = new RequestModel { Path = "/bridge/kline", Methods = ["POST"] },
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
                            detail = "An error occurred while updating kline data.",
                            instance = "/bridge/kline",
                        },
                    },
                };
        }
    }

    private static class TestData
    {
        public static readonly List<dynamic> ValidKlineDataResponse =
        [
            new
            {
                IdTradingPair = 101,
                Klines = new[]
                {
                    new
                    {
                        openTime = 1640995200000L,
                        openPrice = "46000.50",
                        highPrice = "47000.75",
                        lowPrice = "45500.25",
                        closePrice = "46800.00",
                        volume = "123.456",
                        closeTime = 1640998800000L,
                    },
                    new
                    {
                        openTime = 1640998800000L,
                        openPrice = "46800.00",
                        highPrice = "48000.00",
                        lowPrice = "46500.00",
                        closePrice = "47500.50",
                        volume = "234.567",
                        closeTime = 1641002400000L,
                    },
                },
            },
            new
            {
                IdTradingPair = 102,
                Klines = new[]
                {
                    new
                    {
                        openTime = 1640995200000L,
                        openPrice = "3000.00",
                        highPrice = "3100.00",
                        lowPrice = "2900.00",
                        closePrice = "3050.00",
                        volume = "200.000",
                        closeTime = 1640998800000L,
                    },
                },
            },
        ];
    }
}
