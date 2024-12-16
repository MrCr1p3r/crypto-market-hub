using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SVC_Kline.Controllers;
using SVC_Kline.Models.Input;
using SVC_Kline.Models.Output;
using SVC_Kline.Repositories.Interfaces;

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
    public async Task InsertKlineData_CallsRepository()
    {
        // Arrange
        var klineData = _fixture.Create<KlineDataNew>();

        // Act
        await _controller.InsertKlineData(klineData);

        // Assert
        _mockRepository.Verify(repo => repo.InsertKlineData(klineData), Times.Once);
    }

    [Fact]
    public async Task InsertKlineData_ValidData_ReturnsOkResult()
    {
        // Arrange
        var klineData = _fixture.Create<KlineDataNew>();

        // Act
        var result = await _controller.InsertKlineData(klineData);

        // Assert
        result
            .Should()
            .BeOfType<OkObjectResult>()
            .Which.Value.Should()
            .Be("Kline data inserted successfully.");
    }

    [Fact]
    public async Task InsertManyKlineData_CallsRepository()
    {
        // Arrange
        var klineDataList = _fixture.CreateMany<KlineDataNew>(5).ToList();

        // Act
        await _controller.InsertManyKlineData(klineDataList);

        // Assert
        _mockRepository.Verify(repo => repo.InsertManyKlineData(klineDataList), Times.Once);
    }

    [Fact]
    public async Task InsertManyKlineData_ValidData_ReturnsOkResult()
    {
        // Arrange
        var klineDataList = _fixture.CreateMany<KlineDataNew>(5).ToList();

        // Act
        var result = await _controller.InsertManyKlineData(klineDataList);

        // Assert
        result
            .Should()
            .BeOfType<OkObjectResult>()
            .Which.Value.Should()
            .Be("Multiple Kline data entries inserted successfully.");
    }

    [Fact]
    public async Task GetAllKlineData_CallsRepository()
    {
        // Arrange
        var klineDataList = _fixture.CreateMany<KlineData>(5).ToList();
        _mockRepository.Setup(repo => repo.GetAllKlineData()).ReturnsAsync(klineDataList);

        // Act
        await _controller.GetAllKlineData();

        // Assert
        _mockRepository.Verify(repo => repo.GetAllKlineData(), Times.Once);
    }

    [Fact]
    public async Task GetAllKlineData_ReturnsOkResultWithExpectedData()
    {
        // Arrange
        var klineDataList = _fixture.CreateMany<KlineData>(5).ToList();
        _mockRepository.Setup(repo => repo.GetAllKlineData()).ReturnsAsync(klineDataList);

        // Act
        var result = await _controller.GetAllKlineData();

        // Assert
        result
            .Result.Should()
            .BeOfType<OkObjectResult>()
            .Which.Value.Should()
            .BeEquivalentTo(klineDataList);
    }

    [Fact]
    public async Task DeleteKlineDataForTradingPair_CallsRepository()
    {
        // Arrange
        var idTradePair = _fixture.Create<int>();

        // Act
        await _controller.DeleteKlineDataForTradingPair(idTradePair);

        // Assert
        _mockRepository.Verify(repo => repo.DeleteKlineDataForTradingPair(idTradePair), Times.Once);
    }

    [Fact]
    public async Task DeleteKlineDataForTradingPair_ValidId_ReturnsOkResult()
    {
        // Arrange
        var idTradePair = _fixture.Create<int>();

        // Act
        var result = await _controller.DeleteKlineDataForTradingPair(idTradePair);

        // Assert
        result
            .Should()
            .BeOfType<OkObjectResult>()
            .Which.Value.Should()
            .Be($"Kline data for trading pair ID {idTradePair} deleted successfully.");
    }

    [Fact]
    public async Task ReplaceAllKlineData_CallsRepository()
    {
        // Arrange
        var klineDataList = _fixture.CreateMany<KlineDataNew>(5).ToArray();

        // Act
        await _controller.ReplaceAllKlineData(klineDataList);

        // Assert
        _mockRepository.Verify(repo => repo.ReplaceAllKlineData(klineDataList), Times.Once);
    }

    [Fact]
    public async Task ReplaceAllKlineData_ValidData_ReturnsOkResult()
    {
        // Arrange
        var klineDataList = _fixture.CreateMany<KlineDataNew>(5).ToArray();

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
