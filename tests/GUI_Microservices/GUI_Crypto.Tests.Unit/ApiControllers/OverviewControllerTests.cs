using FluentResults;
using GUI_Crypto.ApiContracts.Requests.CoinCreation;
using GUI_Crypto.ApiContracts.Responses.CandidateCoin;
using GUI_Crypto.ApiContracts.Responses.OverviewCoin;
using GUI_Crypto.ApiControllers;
using GUI_Crypto.Services.Overview;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Errors;

namespace GUI_Crypto.Tests.Unit.ApiControllers;

public class OverviewControllerTests : IDisposable
{
    private readonly Mock<IOverviewService> _mockOverviewService;
    private readonly OverviewController _testedController;

    public OverviewControllerTests()
    {
        _mockOverviewService = new Mock<IOverviewService>();
        _testedController = new OverviewController(_mockOverviewService.Object);
    }

    #region RenderOverview Tests

    [Fact]
    public void RenderOverview_Always_ReturnsOverviewView()
    {
        // Act
        var result = _testedController.RenderOverview();

        // Assert
        result.Should().BeOfType<ViewResult>().Which.ViewName.Should().Be("Overview");

        _mockOverviewService.VerifyNoOtherCalls();
    }

    #endregion

    #region GetOverviewCoins Tests

    [Fact]
    public async Task GetOverviewCoins_OnSuccess_CallsServiceAndReturnsOkWithCoins()
    {
        // Arrange
        var expectedCoins = TestData.OverviewCoins;
        var successResult = Result.Ok(expectedCoins);

        _mockOverviewService
            .Setup(service => service.GetOverviewCoins())
            .ReturnsAsync(successResult);

        // Act
        var result = await _testedController.GetOverviewCoins();

        // Assert
        result
            .Should()
            .BeOfType<OkObjectResult>()
            .Which.Value.Should()
            .BeEquivalentTo(expectedCoins);

        _mockOverviewService.Verify(service => service.GetOverviewCoins(), Times.Once);
        _mockOverviewService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetOverviewCoins_OnSuccessWithEmptyData_CallsServiceAndReturnsOkWithEmptyCollection()
    {
        // Arrange
        var emptyResult = Result.Ok(Enumerable.Empty<OverviewCoin>());

        _mockOverviewService.Setup(service => service.GetOverviewCoins()).ReturnsAsync(emptyResult);

        // Act
        var result = await _testedController.GetOverviewCoins();

        // Assert
        result
            .Should()
            .BeOfType<OkObjectResult>()
            .Which.Value.Should()
            .BeEquivalentTo(Array.Empty<OverviewCoin>());

        _mockOverviewService.Verify(service => service.GetOverviewCoins(), Times.Once);
        _mockOverviewService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetOverviewCoins_OnInternalError_CallsServiceAndReturnsInternalServerError()
    {
        // Arrange
        var errorMessage = "Coins service unavailable";
        var failureResult = Result.Fail<IEnumerable<OverviewCoin>>(
            new GenericErrors.InternalError(errorMessage)
        );

        _mockOverviewService
            .Setup(service => service.GetOverviewCoins())
            .ReturnsAsync(failureResult);

        // Act
        var result = await _testedController.GetOverviewCoins();

        // Assert
        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);

        var problemDetails = result
            .Should()
            .BeOfType<ObjectResult>()
            .Which.Value.Should()
            .BeOfType<ProblemDetails>()
            .Subject;

        problemDetails.Detail.Should().Contain(errorMessage);

        _mockOverviewService.Verify(service => service.GetOverviewCoins(), Times.Once);
        _mockOverviewService.VerifyNoOtherCalls();
    }

    #endregion

    #region GetCandidateCoins Tests

    [Fact]
    public async Task GetCandidateCoins_OnSuccess_CallsServiceAndReturnsOkWithCandidateCoins()
    {
        // Arrange
        var expectedCandidateCoins = TestData.CandidateCoins;
        var successResult = Result.Ok(expectedCandidateCoins);

        _mockOverviewService
            .Setup(service => service.GetCandidateCoins())
            .ReturnsAsync(successResult);

        // Act
        var result = await _testedController.GetCandidateCoins();

        // Assert
        result
            .Should()
            .BeOfType<OkObjectResult>()
            .Which.Value.Should()
            .BeEquivalentTo(expectedCandidateCoins);

        _mockOverviewService.Verify(service => service.GetCandidateCoins(), Times.Once);
        _mockOverviewService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetCandidateCoins_OnSuccessWithEmptyData_CallsServiceAndReturnsOkWithEmptyCollection()
    {
        // Arrange
        var emptyResult = Result.Ok(Enumerable.Empty<CandidateCoin>());

        _mockOverviewService
            .Setup(service => service.GetCandidateCoins())
            .ReturnsAsync(emptyResult);

        // Act
        var result = await _testedController.GetCandidateCoins();

        // Assert
        result
            .Should()
            .BeOfType<OkObjectResult>()
            .Which.Value.Should()
            .BeEquivalentTo(Array.Empty<CandidateCoin>());

        _mockOverviewService.Verify(service => service.GetCandidateCoins(), Times.Once);
        _mockOverviewService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetCandidateCoins_OnInternalError_CallsServiceAndReturnsInternalServerError()
    {
        // Arrange
        var errorMessage = "External service unavailable";
        var failureResult = Result.Fail<IEnumerable<CandidateCoin>>(
            new GenericErrors.InternalError(errorMessage)
        );

        _mockOverviewService
            .Setup(service => service.GetCandidateCoins())
            .ReturnsAsync(failureResult);

        // Act
        var result = await _testedController.GetCandidateCoins();

        // Assert
        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);

        var problemDetails = result
            .Should()
            .BeOfType<ObjectResult>()
            .Which.Value.Should()
            .BeOfType<ProblemDetails>()
            .Subject;

        problemDetails.Detail.Should().Contain(errorMessage);

        _mockOverviewService.Verify(service => service.GetCandidateCoins(), Times.Once);
        _mockOverviewService.VerifyNoOtherCalls();
    }

    #endregion

    #region CreateCoins Tests

    [Fact]
    public async Task CreateCoins_OnSuccess_CallsServiceAndReturnsOkWithCreatedCoins()
    {
        // Arrange
        var coinCreationRequests = TestData.CoinCreationRequests;
        var expectedCreatedCoins = TestData.CreatedOverviewCoins;
        var successResult = Result.Ok(expectedCreatedCoins);

        _mockOverviewService
            .Setup(service => service.CreateCoins(coinCreationRequests))
            .ReturnsAsync(successResult);

        // Act
        var result = await _testedController.CreateCoins(coinCreationRequests);

        // Assert
        result
            .Should()
            .BeOfType<OkObjectResult>()
            .Which.Value.Should()
            .BeEquivalentTo(expectedCreatedCoins);

        _mockOverviewService.Verify(
            service => service.CreateCoins(coinCreationRequests),
            Times.Once
        );
        _mockOverviewService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateCoins_OnValidationError_CallsServiceAndReturnsBadRequest()
    {
        // Arrange
        var coinCreationRequests = TestData.InvalidCoinCreationRequests;
        var errorMessage = "Invalid coin creation data";
        var failureResult = Result.Fail<IEnumerable<OverviewCoin>>(
            new GenericErrors.BadRequestError(errorMessage)
        );

        _mockOverviewService
            .Setup(service => service.CreateCoins(coinCreationRequests))
            .ReturnsAsync(failureResult);

        // Act
        var result = await _testedController.CreateCoins(coinCreationRequests);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>().Which.StatusCode.Should().Be(400);

        var problemDetails = result
            .Should()
            .BeOfType<BadRequestObjectResult>()
            .Which.Value.Should()
            .BeOfType<ProblemDetails>()
            .Subject;

        problemDetails.Detail.Should().Contain(errorMessage);

        _mockOverviewService.Verify(
            service => service.CreateCoins(coinCreationRequests),
            Times.Once
        );
        _mockOverviewService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateCoins_OnInternalError_CallsServiceAndReturnsInternalServerError()
    {
        // Arrange
        var coinCreationRequests = TestData.CoinCreationRequests;
        var errorMessage = "Failed to create coins";
        var failureResult = Result.Fail<IEnumerable<OverviewCoin>>(
            new GenericErrors.InternalError(errorMessage)
        );

        _mockOverviewService
            .Setup(service => service.CreateCoins(coinCreationRequests))
            .ReturnsAsync(failureResult);

        // Act
        var result = await _testedController.CreateCoins(coinCreationRequests);

        // Assert
        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);

        var problemDetails = result
            .Should()
            .BeOfType<ObjectResult>()
            .Which.Value.Should()
            .BeOfType<ProblemDetails>()
            .Subject;

        problemDetails.Detail.Should().Contain(errorMessage);

        _mockOverviewService.Verify(
            service => service.CreateCoins(coinCreationRequests),
            Times.Once
        );
        _mockOverviewService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateCoins_OnEmptyRequest_CallsServiceAndReturnsOkWithEmptyCollection()
    {
        // Arrange
        var emptyRequests = Array.Empty<CoinCreationRequest>();
        var emptyResult = Result.Ok(Enumerable.Empty<OverviewCoin>());

        _mockOverviewService
            .Setup(service => service.CreateCoins(emptyRequests))
            .ReturnsAsync(emptyResult);

        // Act
        var result = await _testedController.CreateCoins(emptyRequests);

        // Assert
        result
            .Should()
            .BeOfType<OkObjectResult>()
            .Which.Value.Should()
            .BeEquivalentTo(Array.Empty<OverviewCoin>());

        _mockOverviewService.Verify(service => service.CreateCoins(emptyRequests), Times.Once);
        _mockOverviewService.VerifyNoOtherCalls();
    }

    #endregion

    #region DeleteMainCoin Tests

    [Fact]
    public async Task DeleteMainCoin_OnSuccess_CallsServiceAndReturnsOk()
    {
        // Arrange
        const int coinId = 1;
        var successResult = Result.Ok();

        _mockOverviewService
            .Setup(service => service.DeleteMainCoin(coinId))
            .ReturnsAsync(successResult);

        // Act
        var result = await _testedController.DeleteMainCoin(coinId);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _mockOverviewService.Verify(service => service.DeleteMainCoin(coinId), Times.Once);
        _mockOverviewService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteMainCoin_OnNotFoundError_CallsServiceAndReturnsNotFound()
    {
        // Arrange
        const int coinId = 999;
        var errorMessage = "Coin with ID 999 not found";
        var failureResult = Result.Fail(new GenericErrors.NotFoundError(errorMessage));

        _mockOverviewService
            .Setup(service => service.DeleteMainCoin(coinId))
            .ReturnsAsync(failureResult);

        // Act
        var result = await _testedController.DeleteMainCoin(coinId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>().Which.StatusCode.Should().Be(404);

        var problemDetails = result
            .Should()
            .BeOfType<NotFoundObjectResult>()
            .Which.Value.Should()
            .BeOfType<ProblemDetails>()
            .Subject;

        problemDetails.Detail.Should().Contain(errorMessage);

        _mockOverviewService.Verify(service => service.DeleteMainCoin(coinId), Times.Once);
        _mockOverviewService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteMainCoin_OnInternalError_CallsServiceAndReturnsInternalServerError()
    {
        // Arrange
        const int coinId = 1;
        var errorMessage = "Failed to delete coin";
        var failureResult = Result.Fail(new GenericErrors.InternalError(errorMessage));

        _mockOverviewService
            .Setup(service => service.DeleteMainCoin(coinId))
            .ReturnsAsync(failureResult);

        // Act
        var result = await _testedController.DeleteMainCoin(coinId);

        // Assert
        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);

        var problemDetails = result
            .Should()
            .BeOfType<ObjectResult>()
            .Which.Value.Should()
            .BeOfType<ProblemDetails>()
            .Subject;

        problemDetails.Detail.Should().Contain(errorMessage);

        _mockOverviewService.Verify(service => service.DeleteMainCoin(coinId), Times.Once);
        _mockOverviewService.VerifyNoOtherCalls();
    }

    #endregion

    #region DeleteAllCoins Tests

    [Fact]
    public async Task DeleteAllCoins_OnSuccess_CallsServiceAndReturnsOk()
    {
        // Arrange
        var successResult = Result.Ok();

        _mockOverviewService.Setup(service => service.DeleteAllCoins()).ReturnsAsync(successResult);

        // Act
        var result = await _testedController.DeleteAllCoins();

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _mockOverviewService.Verify(service => service.DeleteAllCoins(), Times.Once);
        _mockOverviewService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteAllCoins_OnInternalError_CallsServiceAndReturnsInternalServerError()
    {
        // Arrange
        var errorMessage = "Failed to delete all coins";
        var failureResult = Result.Fail(new GenericErrors.InternalError(errorMessage));

        _mockOverviewService.Setup(service => service.DeleteAllCoins()).ReturnsAsync(failureResult);

        // Act
        var result = await _testedController.DeleteAllCoins();

        // Assert
        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);

        var problemDetails = result
            .Should()
            .BeOfType<ObjectResult>()
            .Which.Value.Should()
            .BeOfType<ProblemDetails>()
            .Subject;

        problemDetails.Detail.Should().Contain(errorMessage);

        _mockOverviewService.Verify(service => service.DeleteAllCoins(), Times.Once);
        _mockOverviewService.VerifyNoOtherCalls();
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
        public static readonly IEnumerable<OverviewCoin> OverviewCoins =
        [
            new OverviewCoin
            {
                Id = 1,
                Symbol = "BTC",
                Name = "Bitcoin",
                Category = null,
                MarketCapUsd = 1_200_000_000_000,
                PriceUsd = "50000.00",
                PriceChangePercentage24h = 3.5m,
                TradingPairIds = [101],
                KlineData = new() { TradingPairId = 101, Klines = [] },
            },
            new OverviewCoin
            {
                Id = 2,
                Symbol = "ETH",
                Name = "Ethereum",
                Category = null,
                MarketCapUsd = 400_000_000_000,
                PriceUsd = "3000.00",
                PriceChangePercentage24h = -1.2m,
                TradingPairIds = [102],
                KlineData = new() { TradingPairId = 102, Klines = [] },
            },
        ];

        public static readonly IEnumerable<CandidateCoin> CandidateCoins =
        [
            new CandidateCoin
            {
                Id = null,
                Symbol = "ADA",
                Name = "Cardano",
                Category = null,
                IdCoinGecko = "cardano",
                TradingPairs = [],
            },
            new CandidateCoin
            {
                Id = null,
                Symbol = "DOT",
                Name = "Polkadot",
                Category = null,
                IdCoinGecko = "polkadot",
                TradingPairs = [],
            },
        ];

        public static readonly IEnumerable<CoinCreationRequest> CoinCreationRequests =
        [
            new CoinCreationRequest
            {
                Id = null,
                Symbol = "ADA",
                Name = "Cardano",
                Category = null,
                IdCoinGecko = "cardano",
                TradingPairs = [],
            },
            new CoinCreationRequest
            {
                Id = null,
                Symbol = "DOT",
                Name = "Polkadot",
                Category = null,
                IdCoinGecko = "polkadot",
                TradingPairs = [],
            },
        ];

        public static readonly IEnumerable<CoinCreationRequest> InvalidCoinCreationRequests =
        [
            new CoinCreationRequest
            {
                Id = null,
                Symbol = string.Empty, // Invalid: empty symbol
                Name = "Invalid Coin",
                Category = null,
                IdCoinGecko = null,
                TradingPairs = [],
            },
        ];

        public static readonly IEnumerable<OverviewCoin> CreatedOverviewCoins =
        [
            new OverviewCoin
            {
                Id = 3,
                Symbol = "ADA",
                Name = "Cardano",
                Category = null,
                MarketCapUsd = 15_000_000_000,
                PriceUsd = "0.45",
                PriceChangePercentage24h = 5.2m,
                TradingPairIds = [],
                KlineData = null,
            },
            new OverviewCoin
            {
                Id = 4,
                Symbol = "DOT",
                Name = "Polkadot",
                Category = null,
                MarketCapUsd = 8_000_000_000,
                PriceUsd = "7.50",
                PriceChangePercentage24h = -2.1m,
                TradingPairIds = [],
                KlineData = null,
            },
        ];
    }
}
