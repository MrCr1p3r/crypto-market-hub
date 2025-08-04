using FluentAssertions.ArgumentMatchers.Moq;
using FluentResults;
using GUI_Crypto.ApiContracts.Requests.KlineData;
using GUI_Crypto.ApiControllers;
using GUI_Crypto.ServiceModels;
using GUI_Crypto.Services.Chart;
using GUI_Crypto.ViewModels;
using GUI_Crypto.ViewModels.Chart;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Enums;
using SharedLibrary.Errors;
using SharedLibrary.Models;
using SvcCoins = GUI_Crypto.MicroserviceClients.SvcCoins.Contracts.Responses;
using SvcExternal = GUI_Crypto.MicroserviceClients.SvcExternal.Contracts.Responses;

namespace GUI_Crypto.Tests.Unit.ApiControllers;

public class ChartControllerTests : IDisposable
{
    private readonly Mock<IChartService> _mockChartService;
    private readonly Mock<ICryptoViewModelFactory> _mockViewModelFactory;
    private readonly ChartController _testedController;

    public ChartControllerTests()
    {
        _mockChartService = new Mock<IChartService>();
        _mockViewModelFactory = new Mock<ICryptoViewModelFactory>();
        _testedController = new ChartController(
            _mockChartService.Object,
            _mockViewModelFactory.Object
        );
    }

    #region Chart Tests

    [Fact]
    public async Task Chart_WhenSuccessfulFlow_ShouldCallBothServicesAndReturnChartView()
    {
        // Arrange
        var idCoin = 1;
        var idTradingPair = 101;
        var chartData = TestData.BitcoinChartData;
        var chartViewModel = TestData.BitcoinChartViewModel;

        _mockChartService
            .Setup(service => service.GetChartData(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(Result.Ok(chartData));

        _mockViewModelFactory
            .Setup(factory => factory.CreateChartViewModel(It.IsAny<ChartData>()))
            .Returns(chartViewModel);

        // Act
        var result = await _testedController.Chart(idCoin, idTradingPair);

        // Assert
        result.Should().BeOfType<ViewResult>().Which.ViewName.Should().Be("Chart");

        result.Should().BeOfType<ViewResult>().Which.Model.Should().BeEquivalentTo(chartViewModel);

        // Verify service calls
        _mockChartService.Verify(
            service => service.GetChartData(idCoin, idTradingPair),
            Times.Once
        );
        _mockViewModelFactory.Verify(
            factory => factory.CreateChartViewModel(Its.EquivalentTo(chartData)),
            Times.Once
        );
    }

    [Fact]
    public async Task Chart_WhenChartServiceFails_ShouldReturnErrorWithoutCallingViewModelFactory()
    {
        // Arrange
        var idCoin = 1;
        var idTradingPair = 101;
        var serviceError = new GenericErrors.InternalError("Chart service unavailable");

        _mockChartService
            .Setup(service => service.GetChartData(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(Result.Fail<ChartData>(serviceError));

        // Act
        var result = await _testedController.Chart(idCoin, idTradingPair);

        // Assert
        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);

        var problemDetails = result
            .Should()
            .BeOfType<ObjectResult>()
            .Which.Value.Should()
            .BeOfType<ProblemDetails>()
            .Subject;

        problemDetails.Detail.Should().Contain("Chart service unavailable");

        // Verify service calls
        _mockChartService.Verify(
            service => service.GetChartData(idCoin, idTradingPair),
            Times.Once
        );
        _mockViewModelFactory.Verify(
            factory => factory.CreateChartViewModel(It.IsAny<ChartData>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Chart_WhenBadRequestError_ShouldReturnBadRequest()
    {
        // Arrange
        var idCoin = -1; // Invalid ID
        var idTradingPair = -1; // Invalid trading pair ID
        var validationError = new GenericErrors.BadRequestError("Invalid request parameters");

        _mockChartService
            .Setup(service => service.GetChartData(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(Result.Fail<ChartData>(validationError));

        // Act
        var result = await _testedController.Chart(idCoin, idTradingPair);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>().Which.StatusCode.Should().Be(400);

        var problemDetails = result
            .Should()
            .BeOfType<BadRequestObjectResult>()
            .Which.Value.Should()
            .BeOfType<ProblemDetails>()
            .Subject;

        problemDetails.Detail.Should().Contain("Invalid request parameters");

        _mockChartService.Verify(
            service => service.GetChartData(idCoin, idTradingPair),
            Times.Once
        );
    }

    [Fact]
    public async Task Chart_WhenNotFoundError_ShouldReturnNotFound()
    {
        // Arrange
        var idCoin = 999; // Non-existent coin ID
        var idTradingPair = 999; // Non-existent trading pair ID
        var notFoundError = new GenericErrors.NotFoundError("Coin not found");

        _mockChartService
            .Setup(service => service.GetChartData(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(Result.Fail<ChartData>(notFoundError));

        // Act
        var result = await _testedController.Chart(idCoin, idTradingPair);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>().Which.StatusCode.Should().Be(404);

        var problemDetails = result
            .Should()
            .BeOfType<NotFoundObjectResult>()
            .Which.Value.Should()
            .BeOfType<ProblemDetails>()
            .Subject;

        problemDetails.Detail.Should().Contain("Coin not found");
    }

    #endregion

    #region GetKlineData Tests

    [Fact]
    public async Task GetKlineData_WhenSuccessfulFlow_ShouldCallServiceAndReturnOkWithKlineData()
    {
        // Arrange
        var request = TestData.BasicKlineDataRequest;
        var expectedKlineData = TestData.BitcoinKlineData;

        _mockChartService
            .Setup(service => service.GetKlineData(It.IsAny<KlineDataRequest>()))
            .ReturnsAsync(Result.Ok(expectedKlineData));

        // Act
        var result = await _testedController.GetKlineData(request);

        // Assert
        result
            .Should()
            .BeOfType<OkObjectResult>()
            .Which.Value.Should()
            .BeEquivalentTo(expectedKlineData);

        _mockChartService.Verify(
            service => service.GetKlineData(Its.EquivalentTo(request)),
            Times.Once
        );
    }

    [Fact]
    public async Task GetKlineData_WhenSuccessWithEmptyData_ShouldReturnOkWithEmptyCollection()
    {
        // Arrange
        var request = TestData.BasicKlineDataRequest;
        var emptyKlineData = Enumerable.Empty<Kline>();

        _mockChartService
            .Setup(service => service.GetKlineData(It.IsAny<KlineDataRequest>()))
            .ReturnsAsync(Result.Ok(emptyKlineData));

        // Act
        var result = await _testedController.GetKlineData(request);

        // Assert
        result
            .Should()
            .BeOfType<OkObjectResult>()
            .Which.Value.Should()
            .BeEquivalentTo(Array.Empty<Kline>());

        _mockChartService.Verify(
            service => service.GetKlineData(Its.EquivalentTo(request)),
            Times.Once
        );
    }

    [Fact]
    public async Task GetKlineData_WhenServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        var request = TestData.BasicKlineDataRequest;
        var serviceError = new GenericErrors.InternalError("External service unavailable");

        _mockChartService
            .Setup(service => service.GetKlineData(It.IsAny<KlineDataRequest>()))
            .ReturnsAsync(Result.Fail<IEnumerable<Kline>>(serviceError));

        // Act
        var result = await _testedController.GetKlineData(request);

        // Assert
        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);

        var problemDetails = result
            .Should()
            .BeOfType<ObjectResult>()
            .Which.Value.Should()
            .BeOfType<ProblemDetails>()
            .Subject;

        problemDetails.Detail.Should().Contain("External service unavailable");

        _mockChartService.Verify(
            service => service.GetKlineData(Its.EquivalentTo(request)),
            Times.Once
        );
    }

    [Fact]
    public async Task GetKlineData_WhenBadRequestError_ShouldReturnBadRequest()
    {
        // Arrange
        var request = TestData.InvalidKlineDataRequest;
        var validationError = new GenericErrors.BadRequestError("Invalid kline data parameters");

        _mockChartService
            .Setup(service => service.GetKlineData(It.IsAny<KlineDataRequest>()))
            .ReturnsAsync(Result.Fail<IEnumerable<Kline>>(validationError));

        // Act
        var result = await _testedController.GetKlineData(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>().Which.StatusCode.Should().Be(400);

        var problemDetails = result
            .Should()
            .BeOfType<BadRequestObjectResult>()
            .Which.Value.Should()
            .BeOfType<ProblemDetails>()
            .Subject;

        problemDetails.Detail.Should().Contain("Invalid kline data parameters");
    }

    [Fact]
    public async Task GetKlineData_WhenNotFoundError_ShouldReturnNotFound()
    {
        // Arrange
        var request = TestData.BasicKlineDataRequest;
        var notFoundError = new GenericErrors.NotFoundError("Trading pair not found");

        _mockChartService
            .Setup(service => service.GetKlineData(It.IsAny<KlineDataRequest>()))
            .ReturnsAsync(Result.Fail<IEnumerable<Kline>>(notFoundError));

        // Act
        var result = await _testedController.GetKlineData(request);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>().Which.StatusCode.Should().Be(404);

        var problemDetails = result
            .Should()
            .BeOfType<NotFoundObjectResult>()
            .Which.Value.Should()
            .BeOfType<ProblemDetails>()
            .Subject;

        problemDetails.Detail.Should().Contain("Trading pair not found");
    }

    #endregion

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _testedController.Dispose();
        }
    }

    private static class TestData
    {
        public static readonly KlineDataRequest BasicKlineDataRequest = new()
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
                Id = 5,
                Symbol = "USDT",
                Name = "Tether",
            },
            Exchanges = [Exchange.Binance],
            Interval = ExchangeKlineInterval.FifteenMinutes,
            StartTime = DateTime.Parse("2024-01-01T00:00:00Z"),
            EndTime = DateTime.Parse("2024-01-02T00:00:00Z"),
            Limit = 1000,
        };

        public static readonly KlineDataRequest InvalidKlineDataRequest = new()
        {
            CoinMain = new KlineDataRequestCoin
            {
                Id = -1, // Invalid ID
                Symbol = string.Empty, // Invalid symbol
                Name = string.Empty, // Invalid name
            },
            IdTradingPair = -1, // Invalid trading pair ID
            CoinQuote = new KlineDataRequestCoin
            {
                Id = -1,
                Symbol = string.Empty,
                Name = string.Empty,
            },
            Exchanges = [], // Empty exchanges
        };

        public static readonly SvcCoins.Coin BitcoinCoin = new()
        {
            Id = 1,
            Symbol = "BTC",
            Name = "Bitcoin",
            Category = null,
            IdCoinGecko = "bitcoin",
            TradingPairs =
            [
                new SvcCoins.TradingPair
                {
                    Id = 101,
                    CoinQuote = new SvcCoins.TradingPairCoinQuote
                    {
                        Id = 5,
                        Symbol = "USDT",
                        Name = "Tether",
                    },
                    Exchanges = [Exchange.Binance],
                },
            ],
        };

        public static readonly SvcExternal.KlineDataResponse BitcoinKlineResponse = new()
        {
            IdTradingPair = 101,
            Klines =
            [
                new Kline
                {
                    OpenTime = 1640995200000,
                    OpenPrice = "46000.50",
                    HighPrice = "47000.75",
                    LowPrice = "45500.25",
                    ClosePrice = "46800.00",
                    Volume = "123.456",
                    CloseTime = 1640998800000,
                },
                new Kline
                {
                    OpenTime = 1640998800000,
                    OpenPrice = "46800.00",
                    HighPrice = "48000.00",
                    LowPrice = "46500.00",
                    ClosePrice = "47500.50",
                    Volume = "234.567",
                    CloseTime = 1641002400000,
                },
            ],
        };

        public static readonly ChartData BitcoinChartData = new()
        {
            Coin = BitcoinCoin,
            KlineResponse = BitcoinKlineResponse,
        };

        public static readonly ChartViewModel BitcoinChartViewModel = new()
        {
            Coin = new GUI_Crypto.ViewModels.Chart.Models.CoinChart
            {
                Id = 1,
                Symbol = "BTC",
                Name = "Bitcoin",
                SelectedQuoteCoinSymbol = "USDT",
                TradingPairs = [],
                Klines = [],
            },
        };

        public static readonly IEnumerable<Kline> BitcoinKlineData =
        [
            new Kline
            {
                OpenTime = 1640995200000,
                OpenPrice = "46000.50",
                HighPrice = "47000.75",
                LowPrice = "45500.25",
                ClosePrice = "46800.00",
                Volume = "123.456",
                CloseTime = 1640998800000,
            },
            new Kline
            {
                OpenTime = 1640998800000,
                OpenPrice = "46800.00",
                HighPrice = "48000.00",
                LowPrice = "46500.00",
                ClosePrice = "47500.50",
                Volume = "234.567",
                CloseTime = 1641002400000,
            },
        ];
    }
}
