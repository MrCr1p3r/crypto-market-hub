using AutoFixture;
using FluentAssertions;
using GUI_Crypto.Clients.Interfaces;
using GUI_Crypto.Controllers;
using GUI_Crypto.Models.Chart;
using GUI_Crypto.Models.Input;
using GUI_Crypto.Models.Output;
using GUI_Crypto.ViewModels;
using GUI_Crypto.ViewModels.Factories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace GUI_Crypto.Tests.Unit.Controllers;

public class ChartControllerTests
{
    private readonly IFixture _fixture;
    private readonly Mock<ICryptoViewModelFactory> _viewModelFactoryMock;
    private readonly Mock<ISvcExternalClient> _svcExternalClientMock;
    private readonly ChartController _controller;

    public ChartControllerTests()
    {
        _fixture = new Fixture();
        _viewModelFactoryMock = new Mock<ICryptoViewModelFactory>();
        _svcExternalClientMock = new Mock<ISvcExternalClient>();
        _controller = new ChartController(
            _viewModelFactoryMock.Object,
            _svcExternalClientMock.Object
        );
    }

    [Fact]
    public async Task Chart_ShouldCallCreateChartViewModel()
    {
        // Arrange
        var request = _fixture.Create<CoinChartRequest>();
        var expectedViewModel = _fixture.Create<ChartViewModel>();
        _viewModelFactoryMock
            .Setup(factory => factory.CreateChartViewModel(It.IsAny<CoinChartRequest>()))
            .ReturnsAsync(expectedViewModel);

        // Act
        await _controller.Chart(request);

        // Assert
        _viewModelFactoryMock.Verify(factory => factory.CreateChartViewModel(request), Times.Once);
    }

    [Fact]
    public async Task Chart_ShouldReturnViewWithExpectedViewModel()
    {
        // Arrange
        var request = _fixture.Create<CoinChartRequest>();
        var expectedViewModel = _fixture.Create<ChartViewModel>();
        _viewModelFactoryMock
            .Setup(factory => factory.CreateChartViewModel(It.IsAny<CoinChartRequest>()))
            .ReturnsAsync(expectedViewModel);

        // Act
        var result = await _controller.Chart(request);

        // Assert
        var viewResult = result as ViewResult;
        viewResult!.ViewName.Should().Be("Chart");
        viewResult.Model.Should().BeEquivalentTo(expectedViewModel);
    }

    [Fact]
    public async Task GetKlineData_ShouldCallGetKlineData()
    {
        // Arrange
        var request = _fixture.Create<KlineDataRequest>();
        var expectedKlineData = _fixture.CreateMany<KlineDataExchange>().ToList();
        _svcExternalClientMock
            .Setup(client => client.GetKlineData(It.IsAny<KlineDataRequest>()))
            .ReturnsAsync(expectedKlineData);

        // Act
        await _controller.GetKlineData(request);

        // Assert
        _svcExternalClientMock.Verify(client => client.GetKlineData(request), Times.Once);
    }

    [Fact]
    public async Task GetKlineData_ShouldReturnOkResultWithExpectedData()
    {
        // Arrange
        var request = _fixture.Create<KlineDataRequest>();
        var expectedKlineData = _fixture.CreateMany<KlineDataExchange>().ToList();
        _svcExternalClientMock
            .Setup(client => client.GetKlineData(It.IsAny<KlineDataRequest>()))
            .ReturnsAsync(expectedKlineData);

        // Act
        var result = await _controller.GetKlineData(request);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(expectedKlineData);
    }
}
