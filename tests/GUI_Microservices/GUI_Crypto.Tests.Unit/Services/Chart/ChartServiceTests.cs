using FluentAssertions.ArgumentMatchers.Moq;
using FluentResults;
using GUI_Crypto.ApiContracts.Requests.KlineData;
using GUI_Crypto.MicroserviceClients.SvcCoins;
using GUI_Crypto.MicroserviceClients.SvcExternal;
using GUI_Crypto.Services.Chart;
using SharedLibrary.Enums;
using SharedLibrary.Errors;
using SharedLibrary.Models;
using SvcCoins = GUI_Crypto.MicroserviceClients.SvcCoins.Contracts;
using SvcExternal = GUI_Crypto.MicroserviceClients.SvcExternal.Contracts;

namespace GUI_Crypto.Tests.Unit.Services.Chart;

public class ChartServiceTests
{
    private readonly Mock<ISvcCoinsClient> _mockCoinsClient;
    private readonly Mock<ISvcExternalClient> _mockExternalClient;
    private readonly ChartService _chartService;

    public ChartServiceTests()
    {
        _mockCoinsClient = new Mock<ISvcCoinsClient>();
        _mockExternalClient = new Mock<ISvcExternalClient>();
        _chartService = new ChartService(_mockCoinsClient.Object, _mockExternalClient.Object);
    }

    #region GetChartData Tests

    [Fact]
    public async Task GetChartData_WhenSuccessfulFlow_ShouldReturnChartDataWithDefaultParameters()
    {
        // Arrange
        var idCoin = 1;
        var idTradingPair = 101;

        _mockExternalClient
            .Setup(client => client.GetKlineData(It.IsAny<SvcExternal.Requests.KlineDataRequest>()))
            .ReturnsAsync(Result.Ok(TestData.BitcoinKlineDataResponse));

        _mockCoinsClient
            .Setup(client => client.GetCoinById(It.IsAny<int>()))
            .ReturnsAsync(Result.Ok(TestData.BitcoinCoin));

        // Act
        var result = await _chartService.GetChartData(idCoin, idTradingPair);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Coin.Should().BeEquivalentTo(TestData.BitcoinCoin);
        result.Value.KlineResponse.Should().BeEquivalentTo(TestData.BitcoinKlineDataResponse);

        // Verify client calls with proper parameters
        _mockExternalClient.Verify(
            client =>
                client.GetKlineData(Its.EquivalentTo(TestData.ExpectedDefaultKlineDataRequest)),
            Times.Once
        );
        _mockCoinsClient.Verify(client => client.GetCoinById(idCoin), Times.Once);
    }

    [Fact]
    public async Task GetChartData_WhenExternalClientFails_ShouldReturnFailureResult()
    {
        // Arrange
        var idCoin = 1;
        var idTradingPair = 101;
        var externalServiceError = new GenericErrors.InternalError("External service unavailable");

        _mockExternalClient
            .Setup(client => client.GetKlineData(It.IsAny<SvcExternal.Requests.KlineDataRequest>()))
            .ReturnsAsync(Result.Fail(externalServiceError));

        _mockCoinsClient
            .Setup(client => client.GetCoinById(It.IsAny<int>()))
            .ReturnsAsync(Result.Ok(TestData.BitcoinCoin));

        // Act
        var result = await _chartService.GetChartData(idCoin, idTradingPair);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(externalServiceError);

        // Verify both clients were called
        _mockExternalClient.Verify(
            client =>
                client.GetKlineData(Its.EquivalentTo(TestData.ExpectedDefaultKlineDataRequest)),
            Times.Once
        );
        _mockCoinsClient.Verify(client => client.GetCoinById(idCoin), Times.Once);
    }

    [Fact]
    public async Task GetChartData_WhenCoinsClientFails_ShouldReturnFailureResult()
    {
        // Arrange
        var idCoin = 1;
        var idTradingPair = 101;
        var coinsServiceError = new GenericErrors.InternalError("Coins service unavailable");

        _mockExternalClient
            .Setup(client => client.GetKlineData(It.IsAny<SvcExternal.Requests.KlineDataRequest>()))
            .ReturnsAsync(Result.Ok(TestData.BitcoinKlineDataResponse));

        _mockCoinsClient
            .Setup(client => client.GetCoinById(It.IsAny<int>()))
            .ReturnsAsync(Result.Fail(coinsServiceError));

        // Act
        var result = await _chartService.GetChartData(idCoin, idTradingPair);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(coinsServiceError);

        // Verify only coins client was called (external client should not be called when coins client fails)
        _mockCoinsClient.Verify(client => client.GetCoinById(idCoin), Times.Once);
        _mockExternalClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetChartData_WhenBothClientsFail_ShouldReturnFirstFailure()
    {
        // Arrange
        var idCoin = 1;
        var idTradingPair = 101;
        var externalServiceError = new GenericErrors.InternalError("External service unavailable");
        var coinsServiceError = new GenericErrors.InternalError("Coins service unavailable");

        _mockExternalClient
            .Setup(client => client.GetKlineData(It.IsAny<SvcExternal.Requests.KlineDataRequest>()))
            .ReturnsAsync(Result.Fail(externalServiceError));

        _mockCoinsClient
            .Setup(client => client.GetCoinById(It.IsAny<int>()))
            .ReturnsAsync(Result.Fail(coinsServiceError));

        // Act
        var result = await _chartService.GetChartData(idCoin, idTradingPair);

        // Assert
        result.IsFailed.Should().BeTrue();
        // Should return the first error encountered (coins service in this case since it's called first)
        result.Errors.Should().Contain(coinsServiceError);
    }

    #endregion

    #region GetKlineData Tests

    [Fact]
    public async Task GetKlineData_WhenSuccessfulFlow_ShouldReturnMappedKlineData()
    {
        // Arrange
        var request = TestData.CustomKlineDataRequest;

        _mockExternalClient
            .Setup(client => client.GetKlineData(It.IsAny<SvcExternal.Requests.KlineDataRequest>()))
            .ReturnsAsync(Result.Ok(TestData.BitcoinKlineDataResponse));

        // Act
        var result = await _chartService.GetKlineData(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);

        var klineDataList = result.Value.ToList();
        klineDataList[0].Should().BeEquivalentTo(TestData.ExpectedFirstKline);
        klineDataList[1].Should().BeEquivalentTo(TestData.ExpectedSecondKline);

        // Verify client call with custom parameters (not defaults)
        _mockExternalClient.Verify(
            client =>
                client.GetKlineData(Its.EquivalentTo(TestData.ExpectedCustomKlineDataRequest)),
            Times.Once
        );
    }

    [Fact]
    public async Task GetKlineData_WhenExternalClientFails_ShouldReturnFailureResult()
    {
        // Arrange
        var request = TestData.CustomKlineDataRequest;
        var externalServiceError = new GenericErrors.InternalError("External service unavailable");

        _mockExternalClient
            .Setup(client => client.GetKlineData(It.IsAny<SvcExternal.Requests.KlineDataRequest>()))
            .ReturnsAsync(Result.Fail(externalServiceError));

        // Act
        var result = await _chartService.GetKlineData(request);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(externalServiceError);

        _mockExternalClient.Verify(
            client =>
                client.GetKlineData(Its.EquivalentTo(TestData.ExpectedCustomKlineDataRequest)),
            Times.Once
        );
    }

    [Fact]
    public async Task GetKlineData_WhenEmptyResponse_ShouldReturnEmptyCollection()
    {
        // Arrange
        var request = TestData.CustomKlineDataRequest;

        _mockExternalClient
            .Setup(client => client.GetKlineData(It.IsAny<SvcExternal.Requests.KlineDataRequest>()))
            .ReturnsAsync(Result.Ok(TestData.EmptyKlineDataResponse));

        // Act
        var result = await _chartService.GetKlineData(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    #endregion

    private static class TestData
    {
        public static readonly KlineDataRequest CustomKlineDataRequest = new()
        {
            CoinMain = new KlineDataRequestCoin
            {
                Id = 1,
                Symbol = "BTC",
                Name = "Bitcoin",
            },
            IdTradingPair = 101,
            CoinQuote = new KlineDataRequestCoin
            {
                Id = 2,
                Symbol = "USDT",
                Name = "Tether",
            },
            Exchanges = [Exchange.Binance],
            Interval = ExchangeKlineInterval.OneHour,
            StartTime = DateTime.Parse("2024-01-01T00:00:00Z"),
            EndTime = DateTime.Parse("2024-01-02T00:00:00Z"),
            Limit = 500,
        };

        public static readonly SvcExternal.Responses.KlineDataResponse BitcoinKlineDataResponse =
            new()
            {
                IdTradingPair = 101,
                Klines =
                [
                    new Kline
                    {
                        OpenTime = 1640995200000, // 2022-01-01T00:00:00Z
                        OpenPrice = "46000.50",
                        HighPrice = "47000.75",
                        LowPrice = "45500.25",
                        ClosePrice = "46800.00",
                        Volume = "123.456",
                        CloseTime = 1640998800000, // 2022-01-01T01:00:00Z
                    },
                    new Kline
                    {
                        OpenTime = 1640998800000, // 2022-01-01T01:00:00Z
                        OpenPrice = "46800.00",
                        HighPrice = "48000.00",
                        LowPrice = "46500.00",
                        ClosePrice = "47500.50",
                        Volume = "234.567",
                        CloseTime = 1641002400000, // 2022-01-01T02:00:00Z
                    },
                ],
            };

        public static readonly SvcExternal.Responses.KlineDataResponse EmptyKlineDataResponse =
            new() { IdTradingPair = 101, Klines = [] };

        public static readonly SvcCoins.Responses.Coin BitcoinCoin = new()
        {
            Id = 1,
            Symbol = "BTC",
            Name = "Bitcoin",
            Category = null,
            IdCoinGecko = "bitcoin",
            TradingPairs =
            [
                new SvcCoins.Responses.TradingPair
                {
                    Id = 101,
                    CoinQuote = new SvcCoins.Responses.TradingPairCoinQuote
                    {
                        Id = 2,
                        Symbol = "USDT",
                        Name = "Tether",
                    },
                    Exchanges = [Exchange.Binance],
                },
            ],
        };

        // Expected mapped requests for verification
        public static readonly SvcExternal.Requests.KlineDataRequest ExpectedDefaultKlineDataRequest =
            new()
            {
                CoinMain = new SvcExternal.Requests.KlineDataRequestCoinBase
                {
                    Id = 1,
                    Symbol = "BTC",
                    Name = "Bitcoin",
                },
                TradingPair = new SvcExternal.Requests.KlineDataRequestTradingPair
                {
                    Id = 101,
                    CoinQuote = new SvcExternal.Requests.KlineDataRequestCoinQuote
                    {
                        Id = 2,
                        Symbol = "USDT",
                        Name = "Tether",
                    },
                    Exchanges = [Exchange.Binance],
                },
                Interval = ChartService.DefaultInterval,
                StartTime = ChartService.DefaultStartTime,
                EndTime = ChartService.DefaultEndTime,
                Limit = ChartService.DefaultLimit,
            };

        public static readonly SvcExternal.Requests.KlineDataRequest ExpectedCustomKlineDataRequest =
            new()
            {
                CoinMain = new SvcExternal.Requests.KlineDataRequestCoinBase
                {
                    Id = 1,
                    Symbol = "BTC",
                    Name = "Bitcoin",
                },
                TradingPair = new SvcExternal.Requests.KlineDataRequestTradingPair
                {
                    Id = 101,
                    CoinQuote = new SvcExternal.Requests.KlineDataRequestCoinQuote
                    {
                        Id = 2,
                        Symbol = "USDT",
                        Name = "Tether",
                    },
                    Exchanges = [Exchange.Binance],
                },
                Interval = ExchangeKlineInterval.OneHour,
                StartTime = DateTime.Parse("2024-01-01T00:00:00Z"),
                EndTime = DateTime.Parse("2024-01-02T00:00:00Z"),
                Limit = 500,
            };

        // Expected mapped API responses
        public static readonly Kline ExpectedFirstKline = new()
        {
            OpenTime = 1640995200000,
            OpenPrice = "46000.50",
            HighPrice = "47000.75",
            LowPrice = "45500.25",
            ClosePrice = "46800.00",
            Volume = "123.456",
            CloseTime = 1640998800000,
        };

        public static readonly Kline ExpectedSecondKline = new()
        {
            OpenTime = 1640998800000,
            OpenPrice = "46800.00",
            HighPrice = "48000.00",
            LowPrice = "46500.00",
            ClosePrice = "47500.50",
            Volume = "234.567",
            CloseTime = 1641002400000,
        };
    }
}
