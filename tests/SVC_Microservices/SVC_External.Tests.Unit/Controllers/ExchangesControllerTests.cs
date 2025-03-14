using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;
using FluentResults;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SVC_External.Controllers;
using SVC_External.DataCollectors.Interfaces;
using SVC_External.Models.Input;
using SVC_External.Models.Output;

namespace SVC_External.Tests.Unit.Controllers;

public class ExchangesControllerTests
{
    private readonly IFixture _fixture;
    private readonly Mock<IExchangesDataCollector> _mockDataCollector;
    private readonly ExchangesController _controller;

    public ExchangesControllerTests()
    {
        _fixture = new Fixture().Customize(new AutoMoqCustomization());
        _mockDataCollector = _fixture.Freeze<Mock<IExchangesDataCollector>>();
        _controller = new ExchangesController(_mockDataCollector.Object);
    }

    #region GetKlineDataForTradingPair Tests
    [Fact]
    public async Task GetKlineDataForTradingPair_CallsDataCollectorWithCorrectParameters()
    {
        // Arrange
        var request = _fixture.Create<KlineDataRequest>();
        var expectedResponse = _fixture.Create<KlineDataRequestResponse>();
        _mockDataCollector
            .Setup(dc => dc.GetKlineDataForTradingPair(request))
            .ReturnsAsync(expectedResponse);

        // Act
        await _controller.GetKlineDataForTradingPair(request);

        // Assert
        _mockDataCollector.Verify(dc => dc.GetKlineDataForTradingPair(request), Times.Once);
    }

    [Fact]
    public async Task GetKlineDataForTradingPair_ReturnsOkResultWithExpectedData()
    {
        // Arrange
        var request = _fixture.Create<KlineDataRequest>();
        var expectedResponse = _fixture.Create<KlineDataRequestResponse>();
        _mockDataCollector
            .Setup(dc => dc.GetKlineDataForTradingPair(request))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetKlineDataForTradingPair(request);

        // Assert
        result
            .Should()
            .BeOfType<OkObjectResult>()
            .Which.Value.Should()
            .BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task GetKlineDataForTradingPair_ReturnsEmptyResponseWhenNoDataFound()
    {
        // Arrange
        var request = _fixture.Create<KlineDataRequest>();
        var emptyResponse = new KlineDataRequestResponse();
        _mockDataCollector
            .Setup(dc => dc.GetKlineDataForTradingPair(request))
            .ReturnsAsync(emptyResponse);

        // Act
        var result = await _controller.GetKlineDataForTradingPair(request);

        // Assert
        result
            .Should()
            .BeOfType<OkObjectResult>()
            .Which.Value.Should()
            .BeEquivalentTo(emptyResponse);
    }
    #endregion

    #region GetFirstSuccessfulKlineDataPerCoin Tests
    [Fact]
    public async Task GetFirstSuccessfulKlineDataPerCoin_CallsDataCollectorWithCorrectParameters()
    {
        // Arrange
        var request = _fixture.Create<KlineDataBatchRequest>();
        var expectedResponse = new Dictionary<int, IEnumerable<KlineData>>
        {
            { 1, _fixture.CreateMany<KlineData>() },
            { 2, _fixture.CreateMany<KlineData>() },
            { 3, _fixture.CreateMany<KlineData>() },
        };

        _mockDataCollector
            .Setup(dc => dc.GetFirstSuccessfulKlineDataPerCoin(request))
            .ReturnsAsync(expectedResponse);

        // Act
        await _controller.GetFirstSuccessfulKlineDataPerCoin(request);

        // Assert
        _mockDataCollector.Verify(dc => dc.GetFirstSuccessfulKlineDataPerCoin(request), Times.Once);
    }

    [Fact]
    public async Task GetFirstSuccessfulKlineDataPerCoin_ReturnsOkResultWithExpectedData()
    {
        // Arrange
        var request = _fixture.Create<KlineDataBatchRequest>();
        var expectedResponse = new Dictionary<int, IEnumerable<KlineData>>
        {
            { 1, _fixture.CreateMany<KlineData>() },
            { 2, _fixture.CreateMany<KlineData>() },
            { 3, _fixture.CreateMany<KlineData>() },
        };

        _mockDataCollector
            .Setup(dc => dc.GetFirstSuccessfulKlineDataPerCoin(request))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetFirstSuccessfulKlineDataPerCoin(request);

        // Assert
        result
            .Should()
            .BeOfType<OkObjectResult>()
            .Which.Value.Should()
            .BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task GetFirstSuccessfulKlineDataPerCoin_ReturnsEmptyDictionaryWhenNoDataFound()
    {
        // Arrange
        var request = _fixture.Create<KlineDataBatchRequest>();
        var emptyResponse = new Dictionary<int, IEnumerable<KlineData>>();

        _mockDataCollector
            .Setup(dc => dc.GetFirstSuccessfulKlineDataPerCoin(request))
            .ReturnsAsync(emptyResponse);

        // Act
        var result = await _controller.GetFirstSuccessfulKlineDataPerCoin(request);

        // Assert
        result
            .Should()
            .BeOfType<OkObjectResult>()
            .Which.Value.Should()
            .BeEquivalentTo(emptyResponse);
    }
    #endregion

    #region GetAllSpotCoins Tests
    [Fact]
    public async Task GetAllSpotCoins_CallsDataCollector()
    {
        // Arrange
        var expectedCoins = _fixture.CreateMany<Coin>().ToList();
        _mockDataCollector
            .Setup(dc => dc.GetAllCurrentActiveSpotCoins())
            .ReturnsAsync(expectedCoins);

        // Act
        await _controller.GetAllSpotCoins();

        // Assert
        _mockDataCollector.Verify(dc => dc.GetAllCurrentActiveSpotCoins(), Times.Once);
    }

    [Fact]
    public async Task GetAllSpotCoins_ReturnsOkResultWithExpectedData()
    {
        // Arrange
        var expectedCoins = _fixture.CreateMany<Coin>().ToList();
        _mockDataCollector
            .Setup(dc => dc.GetAllCurrentActiveSpotCoins())
            .ReturnsAsync(expectedCoins);

        // Act
        var result = await _controller.GetAllSpotCoins();

        // Assert
        result
            .Should()
            .BeOfType<OkObjectResult>()
            .Which.Value.Should()
            .BeEquivalentTo(expectedCoins);
    }
    #endregion

    #region GetCoinGeckoAssetsInfo Tests
    [Fact]
    public async Task GetCoinGeckoAssetsInfo_CallsDataCollectorWithCorrectParameters()
    {
        // Arrange
        var coinIds = new[] { "bitcoin", "ethereum", "tether" };
        _mockDataCollector
            .Setup(dc => dc.GetCoinGeckoAssetsInfo(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(Result.Ok<IEnumerable<CoinGeckoAssetInfo>>([]));

        // Act
        await _controller.GetCoinGeckoAssetsInfo(coinIds);

        // Assert
        _mockDataCollector.Verify(
            dc =>
                dc.GetCoinGeckoAssetsInfo(
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
        _mockDataCollector
            .Setup(dc => dc.GetCoinGeckoAssetsInfo(It.IsAny<IEnumerable<string>>()))
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
    #endregion
}
