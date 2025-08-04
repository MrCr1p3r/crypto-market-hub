using System.Text.Json;
using GUI_Crypto.Hubs;
using GUI_Crypto.ServiceModels.Messaging;
using GUI_Crypto.Services.Messaging;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SharedLibrary.Models.Messaging;

namespace GUI_Crypto.Tests.Unit.Services.Messaging;

public class KlineDataMessageHandlerTests
{
    private readonly Mock<IHubContext<CryptoHub, ICryptoHubClient>> _mockHubContext;
    private readonly Mock<ILogger<KlineDataMessageHandler>> _mockLogger;
    private readonly Mock<IHubCallerClients<ICryptoHubClient>> _mockClients;
    private readonly Mock<ICryptoHubClient> _mockCryptoHubClient;
    private readonly KlineDataMessageHandler _handler;

    public KlineDataMessageHandlerTests()
    {
        _mockHubContext = new Mock<IHubContext<CryptoHub, ICryptoHubClient>>();
        _mockLogger = new Mock<ILogger<KlineDataMessageHandler>>();
        _mockClients = new Mock<IHubCallerClients<ICryptoHubClient>>();
        _mockCryptoHubClient = new Mock<ICryptoHubClient>();

        // Setup hub context chain
        _mockHubContext.Setup(h => h.Clients).Returns(_mockClients.Object);
        _mockClients
            .Setup(c => c.Group("KlineDataSubscribers"))
            .Returns(_mockCryptoHubClient.Object);

        _handler = new KlineDataMessageHandler(_mockHubContext.Object, _mockLogger.Object);
    }

    #region Kline Data Specific Tests

    [Fact]
    public async Task HandleAsync_WhenSuccessWithValidKlineData_ShouldSendToCorrectSignalRGroup()
    {
        // Arrange
        var klineDataMessages = new List<KlineData>
        {
            new() { IdTradingPair = 1, Klines = [CreateSampleKline(1, 50000m)] },
        };

        var jsonData = JsonSerializer.Serialize(klineDataMessages);
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonData);

        var successMessage = new JobCompletedMessage
        {
            JobName = "KlineDataUpdate",
            JobType = "KlineData",
            CompletedAt = DateTime.UtcNow,
            Success = true,
            ErrorMessage = null,
            Data = jsonElement,
            Source = "TestSource",
        };

        // Act
        await _handler.HandleAsync(successMessage);

        // Assert
        _mockClients.Verify(c => c.Group("KlineDataSubscribers"), Times.Once);
        _mockCryptoHubClient.Verify(
            client => client.ReceiveKlineDataUpdate(It.IsAny<IEnumerable<KlineData>>()),
            Times.Once
        );
    }

    [Fact]
    public async Task HandleAsync_WhenSuccessWithMultipleKlineDataItems_ShouldSendAllDataToHub()
    {
        // Arrange
        var klineDataMessages = new List<KlineData>
        {
            new() { IdTradingPair = 1, Klines = [CreateSampleKline(1, 50000m)] },
            new() { IdTradingPair = 2, Klines = [CreateSampleKline(2, 3000m)] },
        };

        var jsonData = JsonSerializer.Serialize(klineDataMessages);
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonData);

        var successMessage = new JobCompletedMessage
        {
            JobName = "KlineDataUpdate",
            JobType = "KlineData",
            CompletedAt = DateTime.UtcNow,
            Success = true,
            ErrorMessage = null,
            Data = jsonElement,
            Source = "TestSource",
        };

        // Act
        await _handler.HandleAsync(successMessage);

        // Assert
        _mockCryptoHubClient.Verify(
            client =>
                client.ReceiveKlineDataUpdate(
                    It.Is<IEnumerable<KlineData>>(data =>
                        data.Count() == 2
                        && data.First().IdTradingPair == 1
                        && data.Last().IdTradingPair == 2
                    )
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task HandleAsync_WhenSuccessWithEmptyKlineData_ShouldSendEmptyCollectionToHub()
    {
        // Arrange
        var emptyKlineData = new List<KlineData>();
        var jsonData = JsonSerializer.Serialize(emptyKlineData);
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonData);

        var successMessage = new JobCompletedMessage
        {
            JobName = "KlineDataUpdate",
            JobType = "KlineData",
            CompletedAt = DateTime.UtcNow,
            Success = true,
            ErrorMessage = null,
            Data = jsonElement,
            Source = "TestSource",
        };

        // Act
        await _handler.HandleAsync(successMessage);

        // Assert
        _mockCryptoHubClient.Verify(
            client =>
                client.ReceiveKlineDataUpdate(It.Is<IEnumerable<KlineData>>(data => !data.Any())),
            Times.Once
        );
    }

    [Fact]
    public async Task HandleAsync_WhenSuccessWithMultipleKlines_ShouldPreserveAllKlineData()
    {
        // Arrange
        var klines = new[]
        {
            CreateSampleKline(1, 50000m),
            CreateSampleKline(2, 51000m),
            CreateSampleKline(3, 49000m),
        };

        var klineDataMessages = new List<KlineData>
        {
            new() { IdTradingPair = 1, Klines = klines },
        };

        var jsonData = JsonSerializer.Serialize(klineDataMessages);
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonData);

        var successMessage = new JobCompletedMessage
        {
            JobName = "KlineDataUpdate",
            JobType = "KlineData",
            CompletedAt = DateTime.UtcNow,
            Success = true,
            ErrorMessage = null,
            Data = jsonElement,
            Source = "TestSource",
        };

        // Act
        await _handler.HandleAsync(successMessage);

        // Assert
        _mockCryptoHubClient.Verify(
            client =>
                client.ReceiveKlineDataUpdate(
                    It.Is<IEnumerable<KlineData>>(data =>
                        data.Single().Klines.Count() == 3 && data.Single().IdTradingPair == 1
                    )
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task HandleAsync_WhenSuccessWithValidData_ShouldPreserveKlineProperties()
    {
        // Arrange
        var specificKline = new SharedLibrary.Models.Kline
        {
            OpenTime = 1640995200000, // 2022-01-01T00:00:00Z
            CloseTime = 1640998800000, // 2022-01-01T01:00:00Z
            OpenPrice = "46000.50",
            HighPrice = "47000.75",
            LowPrice = "45500.25",
            ClosePrice = "46800.00",
            Volume = "123.456",
        };

        var klineDataMessages = new List<KlineData>
        {
            new() { IdTradingPair = 42, Klines = [specificKline] },
        };

        var jsonData = JsonSerializer.Serialize(klineDataMessages);
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonData);

        var successMessage = new JobCompletedMessage
        {
            JobName = "KlineDataUpdate",
            JobType = "KlineData",
            CompletedAt = DateTime.UtcNow,
            Success = true,
            ErrorMessage = null,
            Data = jsonElement,
            Source = "TestSource",
        };

        // Act
        await _handler.HandleAsync(successMessage);

        // Assert
        _mockCryptoHubClient.Verify(
            client =>
                client.ReceiveKlineDataUpdate(
                    It.Is<IEnumerable<KlineData>>(data =>
                        data.Single().IdTradingPair == 42
                        && data.Single().Klines.Single().OpenTime == 1640995200000
                        && data.Single().Klines.Single().CloseTime == 1640998800000
                        && data.Single().Klines.Single().OpenPrice == "46000.50"
                        && data.Single().Klines.Single().HighPrice == "47000.75"
                        && data.Single().Klines.Single().LowPrice == "45500.25"
                        && data.Single().Klines.Single().ClosePrice == "46800.00"
                        && data.Single().Klines.Single().Volume == "123.456"
                    )
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task HandleAsync_WhenCancellationTokenProvided_ShouldStillSendToHub()
    {
        // Arrange
        var klineDataMessages = new List<KlineData>
        {
            new() { IdTradingPair = 1, Klines = [CreateSampleKline(1, 50000m)] },
        };

        var jsonData = JsonSerializer.Serialize(klineDataMessages);
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonData);

        var successMessage = new JobCompletedMessage
        {
            JobName = "KlineDataUpdate",
            JobType = "KlineData",
            CompletedAt = DateTime.UtcNow,
            Success = true,
            ErrorMessage = null,
            Data = jsonElement,
            Source = "TestSource",
        };

        using var cts = new CancellationTokenSource();

        // Act
        await _handler.HandleAsync(successMessage, cts.Token);

        // Assert
        _mockCryptoHubClient.Verify(
            client =>
                client.ReceiveKlineDataUpdate(
                    It.Is<IEnumerable<KlineData>>(data => data.Single().IdTradingPair == 1)
                ),
            Times.Once
        );
    }

    #endregion

    #region Helper Methods

    private static SharedLibrary.Models.Kline CreateSampleKline(long id, decimal price)
    {
        var baseTime = DateTimeOffset.UtcNow.AddHours(-id).ToUnixTimeMilliseconds();
        return new SharedLibrary.Models.Kline
        {
            OpenTime = baseTime,
            CloseTime = baseTime + 3600000, // 1 hour later
            OpenPrice = price.ToString(),
            HighPrice = (price * 1.05m).ToString(),
            LowPrice = (price * 0.95m).ToString(),
            ClosePrice = (price * 1.02m).ToString(),
            Volume = (100m + id).ToString(),
        };
    }

    #endregion
}
