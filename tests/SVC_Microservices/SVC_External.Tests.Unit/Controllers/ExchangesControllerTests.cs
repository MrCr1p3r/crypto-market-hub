using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
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

    #region GetKlineData Tests
    [Fact]
    public async Task GetKlineData_CallsDataCollectorWithCorrectParameters()
    {
        // Arrange
        var request = _fixture.Create<KlineDataRequest>();
        var expectedResponse = _fixture.Create<KlineDataRequestResponse>();
        _mockDataCollector.Setup(dc => dc.GetKlineData(request)).ReturnsAsync(expectedResponse);

        // Act
        await _controller.GetKlineData(request);

        // Assert
        _mockDataCollector.Verify(dc => dc.GetKlineData(request), Times.Once);
    }

    [Fact]
    public async Task GetKlineData_ReturnsOkResultWithExpectedData()
    {
        // Arrange
        var request = _fixture.Create<KlineDataRequest>();
        var expectedResponse = _fixture.Create<KlineDataRequestResponse>();
        _mockDataCollector.Setup(dc => dc.GetKlineData(request)).ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetKlineData(request);

        // Assert
        result
            .Should()
            .BeOfType<OkObjectResult>()
            .Which.Value.Should()
            .BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task GetKlineData_ReturnsEmptyResponseWhenNoDataFound()
    {
        // Arrange
        var request = _fixture.Create<KlineDataRequest>();
        var emptyResponse = new KlineDataRequestResponse();
        _mockDataCollector.Setup(dc => dc.GetKlineData(request)).ReturnsAsync(emptyResponse);

        // Act
        var result = await _controller.GetKlineData(request);

        // Assert
        result
            .Should()
            .BeOfType<OkObjectResult>()
            .Which.Value.Should()
            .BeEquivalentTo(emptyResponse);
    }
    #endregion

    #region GetKlineDataBatch Tests
    [Fact]
    public async Task GetKlineDataBatch_CallsDataCollectorWithCorrectParameters()
    {
        // Arrange
        var request = _fixture.Create<KlineDataBatchRequest>();
        var expectedResponses = _fixture.CreateMany<KlineDataRequestResponse>(3).ToList();
        _mockDataCollector
            .Setup(dc => dc.GetKlineDataBatch(request))
            .ReturnsAsync(expectedResponses);

        // Act
        await _controller.GetKlineDataBatch(request);

        // Assert
        _mockDataCollector.Verify(dc => dc.GetKlineDataBatch(request), Times.Once);
    }

    [Fact]
    public async Task GetKlineDataBatch_ReturnsOkResultWithExpectedData()
    {
        // Arrange
        var request = _fixture.Create<KlineDataBatchRequest>();
        var expectedResponses = _fixture.CreateMany<KlineDataRequestResponse>(3).ToList();
        _mockDataCollector
            .Setup(dc => dc.GetKlineDataBatch(request))
            .ReturnsAsync(expectedResponses);

        // Act
        var result = await _controller.GetKlineDataBatch(request);

        // Assert
        result
            .Should()
            .BeOfType<OkObjectResult>()
            .Which.Value.Should()
            .BeEquivalentTo(expectedResponses);
    }

    [Fact]
    public async Task GetKlineDataBatch_ReturnsEmptyListWhenNoDataFound()
    {
        // Arrange
        var request = _fixture.Create<KlineDataBatchRequest>();
        var emptyResponses = new List<KlineDataRequestResponse>();
        _mockDataCollector.Setup(dc => dc.GetKlineDataBatch(request)).ReturnsAsync(emptyResponses);

        // Act
        var result = await _controller.GetKlineDataBatch(request);

        // Assert
        result
            .Should()
            .BeOfType<OkObjectResult>()
            .Which.Value.Should()
            .BeEquivalentTo(emptyResponses);
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

    [Fact]
    public async Task GetAllSpotCoins_Returns503ServiceUnavailableWhenNoCoins()
    {
        // Arrange
        var expectedCoins = new List<Coin>();
        _mockDataCollector
            .Setup(dc => dc.GetAllCurrentActiveSpotCoins())
            .ReturnsAsync(expectedCoins);

        // Act
        var result = await _controller.GetAllSpotCoins();

        // Assert
        result
            .Should()
            .BeOfType<StatusCodeResult>()
            .Which.StatusCode.Should()
            .Be(StatusCodes.Status503ServiceUnavailable);
    }
    #endregion
}
