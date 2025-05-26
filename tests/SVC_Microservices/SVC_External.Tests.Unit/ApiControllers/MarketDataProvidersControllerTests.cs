using AutoFixture.AutoMoq;
using FluentResults;
using Microsoft.AspNetCore.Mvc;
using SVC_External.ApiContracts.Responses.MarketDataProviders;
using SVC_External.ApiControllers;
using SVC_External.Services.MarketDataProviders.Interfaces;

namespace SVC_External.Tests.Unit.ApiControllers;

public class MarketDataProvidersControllerTests
{
    private readonly IFixture _fixture;
    private readonly Mock<ICoinGeckoService> _mockCoinGeckoService;
    private readonly MarketDataProvidersController _controller;

    public MarketDataProvidersControllerTests()
    {
        _fixture = new Fixture().Customize(new AutoMoqCustomization());
        _mockCoinGeckoService = _fixture.Freeze<Mock<ICoinGeckoService>>();
        _controller = new MarketDataProvidersController(_mockCoinGeckoService.Object);
    }

    [Fact]
    public async Task GetCoinGeckoAssetsInfo_CallsCoinGeckoServiceWithCorrectParameters()
    {
        // Arrange
        var coinIds = new[] { "bitcoin", "ethereum", "tether" };
        _mockCoinGeckoService
            .Setup(service => service.GetCoinGeckoAssetsInfo(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(Result.Ok<IEnumerable<CoinGeckoAssetInfo>>([]));

        // Act
        await _controller.GetCoinGeckoAssetsInfo(coinIds);

        // Assert
        _mockCoinGeckoService.Verify(
            service =>
                service.GetCoinGeckoAssetsInfo(
                    It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(coinIds))
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task GetCoinGeckoAssetsInfo_ReturnsOkResultWithExpectedData()
    {
        // Arrange
        var coinIds = new[] { "bitcoin", "ethereum" };

        var expectedAssetInfos = _fixture.CreateMany<CoinGeckoAssetInfo>(2).ToList();
        _mockCoinGeckoService
            .Setup(service => service.GetCoinGeckoAssetsInfo(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(Result.Ok<IEnumerable<CoinGeckoAssetInfo>>(expectedAssetInfos));

        // Act
        var result = await _controller.GetCoinGeckoAssetsInfo(coinIds);

        // Assert
        result
            .Should()
            .BeOfType<OkObjectResult>()
            .Which.Value.Should()
            .BeEquivalentTo(expectedAssetInfos);
    }

    [Fact]
    public async Task GetCoinGeckoAssetsInfo_Returns400BadRequest_WhenNoIdsProvided()
    {
        // Arrange
        var emptyIds = Array.Empty<string>();

        // Act
        var result = await _controller.GetCoinGeckoAssetsInfo(emptyIds);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>().Which.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCoinGeckoAssetsInfo_ReturnsInternalServerError_WhenServiceFails()
    {
        // Arrange
        var coinIds = new[] { "bitcoin", "ethereum" };
        _mockCoinGeckoService
            .Setup(service => service.GetCoinGeckoAssetsInfo(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(Result.Fail("Service error"));

        // Act
        var result = await _controller.GetCoinGeckoAssetsInfo(coinIds);

        // Assert
        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }
}
