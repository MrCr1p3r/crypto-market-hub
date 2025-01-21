using AutoFixture;
using FluentAssertions;
using GUI_Crypto.Clients.Interfaces;
using GUI_Crypto.Controllers;
using GUI_Crypto.Models.Input;
using GUI_Crypto.ViewModels;
using GUI_Crypto.ViewModels.Factories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SVC_External.Models.Output;

namespace GUI_Crypto.Tests.Unit.Controllers;

public class OverviewControllerTests
{
    private readonly IFixture _fixture;
    private readonly Mock<ICryptoViewModelFactory> _viewModelFactoryMock;
    private readonly Mock<ISvcExternalClient> _externalClientMock;
    private readonly Mock<ISvcCoinsClient> _coinsClientMock;
    private readonly OverviewController _controller;

    public OverviewControllerTests()
    {
        _fixture = new Fixture();
        _viewModelFactoryMock = new Mock<ICryptoViewModelFactory>();
        _externalClientMock = new Mock<ISvcExternalClient>();
        _coinsClientMock = new Mock<ISvcCoinsClient>();
        _controller = new OverviewController(
            _viewModelFactoryMock.Object,
            _externalClientMock.Object,
            _coinsClientMock.Object
        );
    }

    [Fact]
    public async Task RenderOverview_ShouldCallCreateCoinViewModel()
    {
        // Arrange
        var expectedViewModel = _fixture.Create<OverviewViewModel>();
        _viewModelFactoryMock
            .Setup(factory => factory.CreateOverviewViewModel())
            .ReturnsAsync(expectedViewModel);

        // Act
        await _controller.RenderOverview();

        // Assert
        _viewModelFactoryMock.Verify(factory => factory.CreateOverviewViewModel(), Times.Once);
    }

    [Fact]
    public async Task RenderOverview_ShouldReturnViewWithExpectedViewModel()
    {
        // Arrange
        var expectedViewModel = _fixture.Create<OverviewViewModel>();
        _viewModelFactoryMock
            .Setup(factory => factory.CreateOverviewViewModel())
            .ReturnsAsync(expectedViewModel);

        // Act
        var result = await _controller.RenderOverview();

        // Assert
        var viewResult = result as ViewResult;
        viewResult!.ViewName.Should().Be("Overview");
        viewResult.Model.Should().BeEquivalentTo(expectedViewModel);
    }

    [Fact]
    public async Task GetListedCoins_ShouldReturnListedCoins()
    {
        // Arrange
        var expectedCoins = _fixture.Create<ListedCoins>();
        _externalClientMock.Setup(client => client.GetAllListedCoins()).ReturnsAsync(expectedCoins);

        // Act
        var result = await _controller.GetListedCoins();

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(expectedCoins);
        _externalClientMock.Verify(client => client.GetAllListedCoins(), Times.Once);
    }

    [Fact]
    public async Task CreateCoin_WhenSuccessful_ShouldReturnOk()
    {
        // Arrange
        var coin = new CoinNew
        {
            Symbol = "BTC",
            Name = "Bitcoin",
            QuoteCoinPriority = 1,
            IsStablecoin = false,
        };
        _coinsClientMock.Setup(client => client.CreateCoin(coin)).ReturnsAsync(true);

        // Act
        var result = await _controller.CreateCoin(coin);

        // Assert
        result.Should().BeOfType<OkResult>();
        _coinsClientMock.Verify(client => client.CreateCoin(coin), Times.Once);
    }

    [Fact]
    public async Task CreateCoin_WhenCoinExists_ShouldReturnConflict()
    {
        // Arrange
        var coin = new CoinNew
        {
            Symbol = "USDT",
            Name = "Tether",
            QuoteCoinPriority = 2,
            IsStablecoin = true,
        };
        _coinsClientMock.Setup(client => client.CreateCoin(coin)).ReturnsAsync(false);

        // Act
        var result = await _controller.CreateCoin(coin);

        // Assert
        result.Should().BeOfType<ConflictResult>();
        _coinsClientMock.Verify(client => client.CreateCoin(coin), Times.Once);
    }

    [Fact]
    public async Task DeleteCoin_ShouldReturnOkAndCallService()
    {
        // Arrange
        var idCoin = _fixture.Create<int>();
        _coinsClientMock.Setup(client => client.DeleteCoin(idCoin)).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteCoin(idCoin);

        // Assert
        result.Should().BeOfType<OkResult>();
        _coinsClientMock.Verify(client => client.DeleteCoin(idCoin), Times.Once);
    }
}
