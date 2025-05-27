using AutoFixture.AutoMoq;
using Microsoft.AspNetCore.Mvc;
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
        var klineDataDict = new Dictionary<int, IEnumerable<KlineData>>
        {
            { 1, _fixture.CreateMany<KlineData>(2) },
            { 2, _fixture.CreateMany<KlineData>(1) },
        };
        _mockRepository.Setup(repo => repo.GetAllKlineData()).ReturnsAsync(klineDataDict);

        // Act
        await _controller.GetAllKlineData();

        // Assert
        _mockRepository.Verify(repo => repo.GetAllKlineData(), Times.Once);
    }

    [Fact]
    public async Task GetAllKlineData_ReturnsOkResultWithExpectedData()
    {
        // Arrange
        var klineDataDict = new Dictionary<int, IEnumerable<KlineData>>
        {
            { 1, _fixture.CreateMany<KlineData>(2) },
            { 2, _fixture.CreateMany<KlineData>(1) },
        };
        _mockRepository.Setup(repo => repo.GetAllKlineData()).ReturnsAsync(klineDataDict);

        // Act
        var result = await _controller.GetAllKlineData();

        // Assert
        result
            .Should()
            .BeOfType<OkObjectResult>()
            .Which.Value.Should()
            .BeEquivalentTo(klineDataDict);
    }

    [Fact]
    public async Task InsertManyKlineData_CallsRepository()
    {
        // Arrange
        var klineDataList = _fixture.CreateMany<KlineDataCreationRequest>(5).ToList();

        // Act
        await _controller.InsertKlineData(klineDataList);

        // Assert
        _mockRepository.Verify(repo => repo.InsertKlineData(klineDataList), Times.Once);
    }

    [Fact]
    public async Task InsertManyKlineData_ValidData_ReturnsOkResult()
    {
        // Arrange
        var klineDataList = _fixture.CreateMany<KlineDataCreationRequest>(5).ToList();

        // Act
        var result = await _controller.InsertKlineData(klineDataList);

        // Assert
        result
            .Should()
            .BeOfType<OkObjectResult>()
            .Which.Value.Should()
            .Be("Multiple Kline data entries inserted successfully.");
    }

    [Fact]
    public async Task ReplaceAllKlineData_CallsRepository()
    {
        // Arrange
        var klineDataList = _fixture.CreateMany<KlineDataCreationRequest>(5).ToArray();

        // Act
        await _controller.ReplaceAllKlineData(klineDataList);

        // Assert
        _mockRepository.Verify(repo => repo.ReplaceAllKlineData(klineDataList), Times.Once);
    }

    [Fact]
    public async Task ReplaceAllKlineData_ValidData_ReturnsOkResult()
    {
        // Arrange
        var klineDataList = _fixture.CreateMany<KlineDataCreationRequest>(5).ToArray();

        // Act
        var result = await _controller.ReplaceAllKlineData(klineDataList);

        // Assert
        result
            .Should()
            .BeOfType<OkObjectResult>()
            .Which.Value.Should()
            .Be("All Kline data replaced successfully.");
    }
}
