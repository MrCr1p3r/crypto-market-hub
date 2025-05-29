using FluentResults;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Errors;
using SVC_Bridge.ApiContracts.Responses;
using SVC_Bridge.ApiControllers;
using SVC_Bridge.Services.Interfaces;

namespace SVC_Bridge.Tests.Unit.ApiControllers;

public class CoinsControllerTests
{
    private readonly Mock<ICoinsService> _mockCoinsService;
    private readonly CoinsController _testedController;

    public CoinsControllerTests()
    {
        _mockCoinsService = new Mock<ICoinsService>();
        _testedController = new CoinsController(_mockCoinsService.Object);
    }

    [Fact]
    public async Task UpdateCoinsMarketData_OnSuccess_CallsServiceAndReturnsOkWithData()
    {
        // Arrange
        var expectedCoinMarketData = TestData.SampleCoinMarketDataCollection;
        var successResult = Result.Ok(expectedCoinMarketData);

        _mockCoinsService
            .Setup(service => service.UpdateCoinsMarketData())
            .ReturnsAsync(successResult);

        // Act
        var result = await _testedController.UpdateCoinsMarketData();

        // Assert
        result
            .Should()
            .BeOfType<OkObjectResult>()
            .Which.Value.Should()
            .BeEquivalentTo(expectedCoinMarketData);

        _mockCoinsService.Verify(service => service.UpdateCoinsMarketData(), Times.Once);
        _mockCoinsService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateCoinsMarketData_OnSuccessWithEmptyData_CallsServiceAndReturnsOkWithEmptyCollection()
    {
        // Arrange
        var emptyResult = Result.Ok(Enumerable.Empty<CoinMarketData>());

        _mockCoinsService
            .Setup(service => service.UpdateCoinsMarketData())
            .ReturnsAsync(emptyResult);

        // Act
        var result = await _testedController.UpdateCoinsMarketData();

        // Assert
        result
            .Should()
            .BeOfType<OkObjectResult>()
            .Which.Value.Should()
            .BeEquivalentTo(Array.Empty<CoinMarketData>());

        _mockCoinsService.Verify(service => service.UpdateCoinsMarketData(), Times.Once);
        _mockCoinsService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateCoinsMarketData_OnInternalError_CallsServiceAndReturnsInternalServerError()
    {
        // Arrange
        var errorMessage = "External service unavailable";
        var failureResult = Result.Fail<IEnumerable<CoinMarketData>>(
            new GenericErrors.InternalError(errorMessage)
        );

        _mockCoinsService
            .Setup(service => service.UpdateCoinsMarketData())
            .ReturnsAsync(failureResult);

        // Act
        var result = await _testedController.UpdateCoinsMarketData();

        // Assert
        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);

        var problemDetails = result
            .Should()
            .BeOfType<ObjectResult>()
            .Which.Value.Should()
            .BeOfType<ProblemDetails>()
            .Subject;

        problemDetails.Detail.Should().Contain(errorMessage);

        _mockCoinsService.Verify(service => service.UpdateCoinsMarketData(), Times.Once);
        _mockCoinsService.VerifyNoOtherCalls();
    }

    private static class TestData
    {
        public static readonly IEnumerable<CoinMarketData> SampleCoinMarketDataCollection =
        [
            new()
            {
                Id = 1,
                MarketCapUsd = 1000000,
                PriceUsd = "50000",
                PriceChangePercentage24h = 2.5m,
            },
            new()
            {
                Id = 2,
                MarketCapUsd = 500000,
                PriceUsd = "3000",
                PriceChangePercentage24h = -1.2m,
            },
            new()
            {
                Id = 3,
                MarketCapUsd = 100000,
                PriceUsd = "1",
                PriceChangePercentage24h = 5.0m,
            },
        ];
    }
}
