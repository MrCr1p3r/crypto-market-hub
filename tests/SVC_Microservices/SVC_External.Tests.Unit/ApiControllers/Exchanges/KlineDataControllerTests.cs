using AutoFixture.AutoMoq;
using FluentResults;
using Microsoft.AspNetCore.Mvc;
using SVC_External.ApiContracts.Requests;
using SVC_External.ApiContracts.Responses.Exchanges.KlineData;
using SVC_External.ApiControllers.Exchanges;
using SVC_External.Services.Exchanges.Interfaces;

namespace SVC_External.Tests.Unit.ApiControllers.Exchanges;

public class KlineDataControllerTests
{
    private readonly IFixture _fixture;
    private readonly Mock<IKlineDataService> _mockKlineDataService;
    private readonly KlineDataController _controller;

    public KlineDataControllerTests()
    {
        _fixture = new Fixture().Customize(new AutoMoqCustomization());
        _mockKlineDataService = _fixture.Freeze<Mock<IKlineDataService>>();
        _controller = new KlineDataController(_mockKlineDataService.Object);
    }

    #region GetKlineDataForTradingPair Tests
    [Fact]
    public async Task GetKlineDataForTradingPair_CallsKlineDataServiceWithCorrectParameters()
    {
        // Arrange
        var request = _fixture.Create<KlineDataRequest>();
        var expectedResponse = _fixture.Create<KlineDataResponse>();
        _mockKlineDataService
            .Setup(service => service.GetKlineDataForTradingPair(request))
            .ReturnsAsync(Result.Ok(expectedResponse));

        // Act
        await _controller.GetKlineDataForTradingPair(request);

        // Assert
        _mockKlineDataService.Verify(
            service => service.GetKlineDataForTradingPair(request),
            Times.Once
        );
    }

    [Fact]
    public async Task GetKlineDataForTradingPair_ReturnsOkResultWithExpectedData()
    {
        // Arrange
        var request = _fixture.Create<KlineDataRequest>();
        var expectedResponse = _fixture.Create<KlineDataResponse>();
        _mockKlineDataService
            .Setup(service => service.GetKlineDataForTradingPair(request))
            .ReturnsAsync(Result.Ok(expectedResponse));

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
    public async Task GetKlineDataForTradingPair_ReturnsInternalServerError_WhenServiceFails()
    {
        // Arrange
        var request = _fixture.Create<KlineDataRequest>();
        _mockKlineDataService
            .Setup(service => service.GetKlineDataForTradingPair(request))
            .ReturnsAsync(Result.Fail("Service error"));

        // Act
        var result = await _controller.GetKlineDataForTradingPair(request);

        // Assert
        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }
    #endregion

    #region GetFirstSuccessfulKlineDataPerCoin Tests
    [Fact]
    public async Task GetFirstSuccessfulKlineDataPerCoin_CallsKlineDataServiceWithCorrectParameters()
    {
        // Arrange
        var request = _fixture.Create<KlineDataBatchRequest>();
        var expectedResponse = _fixture.CreateMany<KlineDataResponse>().ToList();

        _mockKlineDataService
            .Setup(service => service.GetFirstSuccessfulKlineDataPerCoin(request))
            .ReturnsAsync(expectedResponse);

        // Act
        await _controller.GetFirstSuccessfulKlineDataPerCoin(request);

        // Assert
        _mockKlineDataService.Verify(
            service => service.GetFirstSuccessfulKlineDataPerCoin(request),
            Times.Once
        );
    }

    [Fact]
    public async Task GetFirstSuccessfulKlineDataPerCoin_ReturnsOkResultWithExpectedData()
    {
        // Arrange
        var request = _fixture.Create<KlineDataBatchRequest>();
        var expectedResponse = _fixture.CreateMany<KlineDataResponse>().ToList();

        _mockKlineDataService
            .Setup(service => service.GetFirstSuccessfulKlineDataPerCoin(request))
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
    public async Task GetFirstSuccessfulKlineDataPerCoin_ReturnsEmptyCollection_WhenNoDataFound()
    {
        // Arrange
        var request = _fixture.Create<KlineDataBatchRequest>();
        var emptyResponse = new List<KlineDataResponse>();

        _mockKlineDataService
            .Setup(service => service.GetFirstSuccessfulKlineDataPerCoin(request))
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
}
