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

    [Fact]
    public async Task GetKlineData_CallsDataCollectorWithCorrectParameters()
    {
        // Arrange
        var request = _fixture.Create<KlineDataRequest>();
        var expectedKlineData = _fixture.CreateMany<KlineData>(5);
        _mockDataCollector.Setup(dc => dc.GetKlineData(request)).ReturnsAsync(expectedKlineData);

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
        var expectedKlineData = _fixture.CreateMany<KlineData>(5).ToList();
        _mockDataCollector.Setup(dc => dc.GetKlineData(request)).ReturnsAsync(expectedKlineData);

        // Act
        var result = await _controller.GetKlineData(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(expectedKlineData);
    }

    [Fact]
    public async Task GetKlineData_ReturnsEmptyListIfNoDataFound()
    {
        // Arrange
        var request = _fixture.Create<KlineDataRequest>();
        _mockDataCollector.Setup(dc => dc.GetKlineData(request)).ReturnsAsync(new List<KlineData>());

        // Act
        var result = await _controller.GetKlineData(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(new List<KlineData>());
    }
}
