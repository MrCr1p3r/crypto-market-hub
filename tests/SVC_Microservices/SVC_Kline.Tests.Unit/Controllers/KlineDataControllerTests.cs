using AutoFixture.AutoMoq;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Models;
using SVC_Kline.ApiContracts.Requests;
using SVC_Kline.ApiContracts.Responses;
using SVC_Kline.ApiControllers;
using SVC_Kline.Repositories;

namespace SVC_Kline.Tests.Unit.Controllers;

public class KlineDataControllerTests
{
    private readonly IFixture _fixture;
    private readonly Mock<IKlineDataRepository> _mockRepository;
    private readonly KlineDataController _controller;

    public KlineDataControllerTests()
    {
        _fixture = new Fixture().Customize(new AutoMoqCustomization());
        _mockRepository = new Mock<IKlineDataRepository>();
        _controller = new KlineDataController(_mockRepository.Object);
    }

    [Fact]
    public async Task GetAllKlineData_CallsRepository()
    {
        // Arrange
        var klineDataResponses = new List<KlineDataResponse>
        {
            new() { IdTradingPair = 1, Klines = _fixture.CreateMany<Kline>(2) },
            new() { IdTradingPair = 2, Klines = _fixture.CreateMany<Kline>(1) },
        };
        _mockRepository.Setup(repo => repo.GetAllKlineData()).ReturnsAsync(klineDataResponses);

        // Act
        await _controller.GetAllKlineData();

        // Assert
        _mockRepository.Verify(repo => repo.GetAllKlineData(), Times.Once);
    }

    [Fact]
    public async Task GetAllKlineData_ReturnsOkResultWithExpectedData()
    {
        // Arrange
        var klineDataResponses = new List<KlineDataResponse>
        {
            new() { IdTradingPair = 1, Klines = _fixture.CreateMany<Kline>(2) },
            new() { IdTradingPair = 2, Klines = _fixture.CreateMany<Kline>(1) },
        };
        _mockRepository.Setup(repo => repo.GetAllKlineData()).ReturnsAsync(klineDataResponses);

        // Act
        var result = await _controller.GetAllKlineData();

        // Assert
        result
            .Should()
            .BeOfType<OkObjectResult>()
            .Which.Value.Should()
            .BeEquivalentTo(klineDataResponses);
    }

    [Fact]
    public async Task InsertKlineData_CallsRepository()
    {
        // Arrange
        var klineDataList = _fixture.CreateMany<KlineDataCreationRequest>(5).ToList();
        var expectedResponse = new List<KlineDataResponse>
        {
            new() { IdTradingPair = 1, Klines = _fixture.CreateMany<Kline>(3) },
            new() { IdTradingPair = 2, Klines = _fixture.CreateMany<Kline>(2) },
        };
        _mockRepository
            .Setup(repo => repo.InsertKlineData(klineDataList))
            .ReturnsAsync(expectedResponse);

        // Act
        await _controller.InsertKlineData(klineDataList);

        // Assert
        _mockRepository.Verify(repo => repo.InsertKlineData(klineDataList), Times.Once);
    }

    [Fact]
    public async Task InsertKlineData_ValidData_ReturnsOkResultWithInsertedData()
    {
        // Arrange
        var klineDataList = _fixture.CreateMany<KlineDataCreationRequest>(5).ToList();
        var expectedResponse = new List<KlineDataResponse>
        {
            new() { IdTradingPair = 1, Klines = _fixture.CreateMany<Kline>(3) },
            new() { IdTradingPair = 2, Klines = _fixture.CreateMany<Kline>(2) },
        };
        _mockRepository
            .Setup(repo => repo.InsertKlineData(klineDataList))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.InsertKlineData(klineDataList);

        // Assert
        result
            .Should()
            .BeOfType<OkObjectResult>()
            .Which.Value.Should()
            .BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task ReplaceAllKlineData_CallsRepository()
    {
        // Arrange
        var klineDataList = _fixture.CreateMany<KlineDataCreationRequest>(5).ToArray();
        var expectedResponse = new List<KlineDataResponse>
        {
            new() { IdTradingPair = 1, Klines = _fixture.CreateMany<Kline>(5) },
        };
        _mockRepository
            .Setup(repo => repo.ReplaceAllKlineData(klineDataList))
            .ReturnsAsync(expectedResponse);

        // Act
        await _controller.ReplaceAllKlineData(klineDataList);

        // Assert
        _mockRepository.Verify(repo => repo.ReplaceAllKlineData(klineDataList), Times.Once);
    }

    [Fact]
    public async Task ReplaceAllKlineData_ValidData_ReturnsOkResultWithReplacedData()
    {
        // Arrange
        var klineDataList = _fixture.CreateMany<KlineDataCreationRequest>(5).ToArray();
        var expectedResponse = new List<KlineDataResponse>
        {
            new() { IdTradingPair = 1, Klines = _fixture.CreateMany<Kline>(5) },
        };
        _mockRepository
            .Setup(repo => repo.ReplaceAllKlineData(klineDataList))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.ReplaceAllKlineData(klineDataList);

        // Assert
        result
            .Should()
            .BeOfType<OkObjectResult>()
            .Which.Value.Should()
            .BeEquivalentTo(expectedResponse);
    }
}
