using AutoFixture;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SVC_Bridge.Clients.Interfaces;
using SVC_Bridge.Controllers;
using SVC_Bridge.DataCollectors.Interfaces;
using SVC_Bridge.Models.Input;

namespace SVC_Bridge.Tests.Unit.Controllers;

public class KlineDataControllerTests
{
    private readonly IFixture _fixture;
    private readonly Mock<IKlineDataCollector> _mockKlineDataCollector;
    private readonly Mock<ISvcKlineClient> _mockKlineClient;
    private readonly KlineDataController _controller;

    public KlineDataControllerTests()
    {
        _fixture = new Fixture();
        _mockKlineDataCollector = new Mock<IKlineDataCollector>();
        _mockKlineClient = new Mock<ISvcKlineClient>();

        _controller = new KlineDataController(
            _mockKlineDataCollector.Object,
            _mockKlineClient.Object
        );
    }

    [Fact]
    public async Task UpdateEntireKlineData_Calls_CollectEntireKlineData_On_DataCollector()
    {
        // Arrange
        var klineData = _fixture.CreateMany<KlineDataNew>(5).ToList();
        _mockKlineDataCollector
            .Setup(collector => collector.CollectEntireKlineData())
            .ReturnsAsync(klineData);

        // Act
        await _controller.UpdateEntireKlineData();

        // Assert
        _mockKlineDataCollector.Verify(collector => collector.CollectEntireKlineData(), Times.Once);
    }

    [Fact]
    public async Task UpdateEntireKlineData_Calls_ReplaceAllKlineData_On_KlineClient()
    {
        // Arrange
        var klineData = _fixture.CreateMany<KlineDataNew>(5).ToList();
        _mockKlineDataCollector
            .Setup(collector => collector.CollectEntireKlineData())
            .ReturnsAsync(klineData);

        // Act
        await _controller.UpdateEntireKlineData();

        // Assert
        _mockKlineClient.Verify(client => client.ReplaceAllKlineData(klineData), Times.Once);
    }

    [Fact]
    public async Task UpdateEntireKlineData_Returns_BadRequest_When_NoDataCollected()
    {
        // Arrange
        _mockKlineDataCollector
            .Setup(collector => collector.CollectEntireKlineData())
            .ReturnsAsync([]);

        // Act
        var result = await _controller.UpdateEntireKlineData();

        // Assert
        result
            .Should()
            .BeOfType<BadRequestObjectResult>()
            .Which.Value.Should()
            .Be("No kline data was collected.");
    }

    [Fact]
    public async Task UpdateEntireKlineData_Returns_Ok_When_DataCollectedAndUpdated()
    {
        // Arrange
        var klineData = _fixture.CreateMany<KlineDataNew>(5).ToList();
        _mockKlineDataCollector
            .Setup(collector => collector.CollectEntireKlineData())
            .ReturnsAsync(klineData);

        // Act
        var result = await _controller.UpdateEntireKlineData();

        // Assert
        result
            .Should()
            .BeOfType<OkObjectResult>()
            .Which.Value.Should()
            .Be("Kline data updated successfully.");
    }
}
