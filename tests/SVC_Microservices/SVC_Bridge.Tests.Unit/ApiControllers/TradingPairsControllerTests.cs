using FluentResults;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Errors;
using SVC_Bridge.ApiContracts.Responses.Coins;
using SVC_Bridge.ApiControllers;
using SVC_Bridge.Services.Interfaces;

namespace SVC_Bridge.Tests.Unit.ApiControllers;

public class TradingPairsControllerTests
{
    private readonly Mock<ITradingPairsService> _mockTradingPairsService;
    private readonly TradingPairsController _testedController;

    public TradingPairsControllerTests()
    {
        _mockTradingPairsService = new Mock<ITradingPairsService>();
        _testedController = new TradingPairsController(_mockTradingPairsService.Object);
    }

    [Fact]
    public async Task UpdateTradingPairs_OnSuccess_CallsServiceAndReturnsOkWithData()
    {
        // Arrange
        var expectedCoins = TestData.SampleCoinsWithTradingPairs;
        var successResult = Result.Ok(expectedCoins);

        _mockTradingPairsService
            .Setup(service => service.UpdateTradingPairs())
            .ReturnsAsync(successResult);

        // Act
        var result = await _testedController.UpdateTradingPairs();

        // Assert
        result
            .Should()
            .BeOfType<OkObjectResult>()
            .Which.Value.Should()
            .BeEquivalentTo(expectedCoins);

        _mockTradingPairsService.Verify(service => service.UpdateTradingPairs(), Times.Once);
        _mockTradingPairsService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateTradingPairs_OnSuccessWithEmptyData_CallsServiceAndReturnsOkWithEmptyCollection()
    {
        // Arrange
        var emptyResult = Result.Ok(Enumerable.Empty<Coin>());

        _mockTradingPairsService
            .Setup(service => service.UpdateTradingPairs())
            .ReturnsAsync(emptyResult);

        // Act
        var result = await _testedController.UpdateTradingPairs();

        // Assert
        result
            .Should()
            .BeOfType<OkObjectResult>()
            .Which.Value.Should()
            .BeEquivalentTo(Array.Empty<Coin>());

        _mockTradingPairsService.Verify(service => service.UpdateTradingPairs(), Times.Once);
        _mockTradingPairsService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateTradingPairs_OnInternalError_CallsServiceAndReturnsInternalServerError()
    {
        // Arrange
        var errorMessage = "Failed to retrieve coins from external service";
        var failureResult = Result.Fail<IEnumerable<Coin>>(
            new GenericErrors.InternalError(errorMessage)
        );

        _mockTradingPairsService
            .Setup(service => service.UpdateTradingPairs())
            .ReturnsAsync(failureResult);

        // Act
        var result = await _testedController.UpdateTradingPairs();

        // Assert
        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);

        var problemDetails = result
            .Should()
            .BeOfType<ObjectResult>()
            .Which.Value.Should()
            .BeOfType<ProblemDetails>()
            .Subject;

        problemDetails.Detail.Should().Contain(errorMessage);

        _mockTradingPairsService.Verify(service => service.UpdateTradingPairs(), Times.Once);
        _mockTradingPairsService.VerifyNoOtherCalls();
    }

    private static class TestData
    {
        public static readonly IEnumerable<Coin> SampleCoinsWithTradingPairs =
        [
            new()
            {
                Id = 1,
                Symbol = "BTC",
                Name = "Bitcoin",
                Category = null,
                IdCoinGecko = "bitcoin",
                MarketCapUsd = 1000000000,
                PriceUsd = "50000",
                PriceChangePercentage24h = 2.5m,
                TradingPairs =
                [
                    new()
                    {
                        Id = 1,
                        CoinQuote = new()
                        {
                            Id = 2,
                            Symbol = "USDT",
                            Name = "Tether",
                            Category = SharedLibrary.Enums.CoinCategory.Stablecoin,
                            IdCoinGecko = "tether",
                            MarketCapUsd = 50000000,
                            PriceUsd = "1.0",
                            PriceChangePercentage24h = 0.01m,
                        },
                        Exchanges = [SharedLibrary.Enums.Exchange.Binance],
                    },
                ],
            },
            new()
            {
                Id = 2,
                Symbol = "ETH",
                Name = "Ethereum",
                Category = null,
                IdCoinGecko = "ethereum",
                MarketCapUsd = 500000000,
                PriceUsd = "3000",
                PriceChangePercentage24h = 1.8m,
                TradingPairs =
                [
                    new()
                    {
                        Id = 2,
                        CoinQuote = new()
                        {
                            Id = 2,
                            Symbol = "USDT",
                            Name = "Tether",
                            Category = SharedLibrary.Enums.CoinCategory.Stablecoin,
                            IdCoinGecko = "tether",
                            MarketCapUsd = 50000000,
                            PriceUsd = "1.0",
                            PriceChangePercentage24h = 0.01m,
                        },
                        Exchanges =
                        [
                            SharedLibrary.Enums.Exchange.Binance,
                            SharedLibrary.Enums.Exchange.Bybit,
                        ],
                    },
                ],
            },
        ];
    }
}
