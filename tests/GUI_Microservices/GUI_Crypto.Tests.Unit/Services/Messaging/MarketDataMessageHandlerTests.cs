using System.Text.Json;
using GUI_Crypto.Hubs;
using GUI_Crypto.ServiceModels.Messaging;
using GUI_Crypto.Services.Messaging;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SharedLibrary.Models.Messaging;

namespace GUI_Crypto.Tests.Unit.Services.Messaging;

public class MarketDataMessageHandlerTests
{
    private readonly Mock<IHubContext<CryptoHub, ICryptoHubClient>> _mockHubContext;
    private readonly Mock<ILogger<MarketDataMessageHandler>> _mockLogger;
    private readonly Mock<IHubCallerClients<ICryptoHubClient>> _mockClients;
    private readonly Mock<ICryptoHubClient> _mockCryptoHubClient;
    private readonly MarketDataMessageHandler _handler;

    public MarketDataMessageHandlerTests()
    {
        _mockHubContext = new Mock<IHubContext<CryptoHub, ICryptoHubClient>>();
        _mockLogger = new Mock<ILogger<MarketDataMessageHandler>>();
        _mockClients = new Mock<IHubCallerClients<ICryptoHubClient>>();
        _mockCryptoHubClient = new Mock<ICryptoHubClient>();

        // Setup hub context chain
        _mockHubContext.Setup(h => h.Clients).Returns(_mockClients.Object);
        _mockClients
            .Setup(c => c.Group("MarketDataSubscribers"))
            .Returns(_mockCryptoHubClient.Object);

        _handler = new MarketDataMessageHandler(_mockHubContext.Object, _mockLogger.Object);
    }

    #region Market Data Specific Tests

    [Fact]
    public async Task HandleAsync_WhenSuccessWithValidMarketData_ShouldSendToCorrectSignalRGroup()
    {
        // Arrange
        var marketDataMessages = new List<CoinMarketData>
        {
            new()
            {
                Id = 1,
                MarketCapUsd = 1000000000000,
                PriceUsd = "50000.00",
                PriceChangePercentage24h = 3.5m,
            },
        };

        var jsonData = JsonSerializer.Serialize(marketDataMessages);
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonData);

        var successMessage = new JobCompletedMessage
        {
            JobName = "MarketDataUpdate",
            JobType = "MarketData",
            CompletedAt = DateTime.UtcNow,
            Success = true,
            ErrorMessage = null,
            Data = jsonElement,
            Source = "TestSource",
        };

        // Act
        await _handler.HandleAsync(successMessage);

        // Assert
        _mockClients.Verify(c => c.Group("MarketDataSubscribers"), Times.Once);
        _mockCryptoHubClient.Verify(
            client => client.ReceiveMarketDataUpdate(It.IsAny<IEnumerable<CoinMarketData>>()),
            Times.Once
        );
    }

    [Fact]
    public async Task HandleAsync_WhenSuccessWithMultipleMarketDataItems_ShouldSendAllDataToHub()
    {
        // Arrange
        var marketDataMessages = new List<CoinMarketData>
        {
            new()
            {
                Id = 1,
                MarketCapUsd = 1000000000000,
                PriceUsd = "50000.00",
                PriceChangePercentage24h = 3.5m,
            },
            new()
            {
                Id = 2,
                MarketCapUsd = 400000000000,
                PriceUsd = "3000.00",
                PriceChangePercentage24h = -1.2m,
            },
        };

        var jsonData = JsonSerializer.Serialize(marketDataMessages);
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonData);

        var successMessage = new JobCompletedMessage
        {
            JobName = "MarketDataUpdate",
            JobType = "MarketData",
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
                client.ReceiveMarketDataUpdate(
                    It.Is<IEnumerable<CoinMarketData>>(data =>
                        data.Count() == 2 && data.First().Id == 1 && data.Last().Id == 2
                    )
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task HandleAsync_WhenSuccessWithEmptyMarketData_ShouldSendEmptyCollectionToHub()
    {
        // Arrange
        var emptyMarketData = new List<CoinMarketData>();
        var jsonData = JsonSerializer.Serialize(emptyMarketData);
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonData);

        var successMessage = new JobCompletedMessage
        {
            JobName = "MarketDataUpdate",
            JobType = "MarketData",
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
                client.ReceiveMarketDataUpdate(
                    It.Is<IEnumerable<CoinMarketData>>(data => !data.Any())
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task HandleAsync_WhenSuccessWithValidData_ShouldPreserveAllMarketDataProperties()
    {
        // Arrange
        var marketDataMessage = new CoinMarketData
        {
            Id = 42,
            MarketCapUsd = 1500000000000,
            PriceUsd = "75000.50",
            PriceChangePercentage24h = 7.25m,
        };

        var marketDataMessages = new List<CoinMarketData> { marketDataMessage };
        var jsonData = JsonSerializer.Serialize(marketDataMessages);
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonData);

        var successMessage = new JobCompletedMessage
        {
            JobName = "MarketDataUpdate",
            JobType = "MarketData",
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
                client.ReceiveMarketDataUpdate(
                    It.Is<IEnumerable<CoinMarketData>>(data =>
                        data.Single().Id == 42
                        && data.Single().MarketCapUsd == 1500000000000
                        && data.Single().PriceUsd == "75000.50"
                        && data.Single().PriceChangePercentage24h == 7.25m
                    )
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task HandleAsync_WhenCancellationTokenProvided_ShouldStillSendToHub()
    {
        // Arrange
        var marketDataMessages = new List<CoinMarketData>
        {
            new()
            {
                Id = 1,
                MarketCapUsd = 1000000000000,
                PriceUsd = "50000.00",
                PriceChangePercentage24h = 3.5m,
            },
        };

        var jsonData = JsonSerializer.Serialize(marketDataMessages);
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonData);

        var successMessage = new JobCompletedMessage
        {
            JobName = "MarketDataUpdate",
            JobType = "MarketData",
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
                client.ReceiveMarketDataUpdate(
                    It.Is<IEnumerable<CoinMarketData>>(data => data.Single().Id == 1)
                ),
            Times.Once
        );
    }

    #endregion
}
