using AutoFixture;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SVC_Coins.Controllers;
using SVC_Coins.Models.Input;
using SVC_Coins.Models.Output;
using SVC_Coins.Repositories.Interfaces;

namespace SVC_Coins.Tests.Unit.Controllers;

public class CoinsControllerTests
{
    private readonly IFixture _fixture;
    private readonly Mock<ICoinsRepository> _mockRepository;
    private readonly CoinsController _controller;

    public CoinsControllerTests()
    {
        _fixture = new Fixture();
        _fixture.Behaviors.Remove(new ThrowingRecursionBehavior());
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        _mockRepository = new Mock<ICoinsRepository>();
        _controller = new CoinsController(_mockRepository.Object);
    }

    [Fact]
    public async Task InsertCoin_CallsRepository()
    {
        // Arrange
        var coin = _fixture.Create<CoinNew>();

        // Act
        await _controller.InsertCoin(coin);

        // Assert
        _mockRepository.Verify(repo => repo.InsertCoin(coin), Times.Once);
    }

    [Fact]
    public async Task InsertCoin_ReturnsOkResult()
    {
        // Arrange
        var coin = _fixture.Create<CoinNew>();

        // Act
        var result = await _controller.InsertCoin(coin);

        // Assert
        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be("Coin inserted successfully.");
    }

    [Fact]
    public async Task GetAllCoins_CallsRepository()
    {
        // Arrange
        var coinsList = _fixture.CreateMany<Coin>(5);
        _mockRepository.Setup(repo => repo.GetAllCoins()).ReturnsAsync(coinsList);

        // Act
        await _controller.GetAllCoins();

        // Assert
        _mockRepository.Verify(repo => repo.GetAllCoins(), Times.Once);
    }

    [Fact]
    public async Task GetAllCoins_ReturnsOkResultWithExpectedData()
    {
        // Arrange
        var coinsList = _fixture.CreateMany<Coin>(5);
        _mockRepository.Setup(repo => repo.GetAllCoins()).ReturnsAsync(coinsList);

        // Act
        var result = await _controller.GetAllCoins();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(coinsList);
    }

    [Fact]
    public async Task DeleteCoin_CallsRepository()
    {
        // Arrange
        var idCoin = _fixture.Create<int>();

        // Act
        await _controller.DeleteCoin(idCoin);

        // Assert
        _mockRepository.Verify(repo => repo.DeleteCoin(idCoin), Times.Once);
    }

    [Fact]
    public async Task DeleteCoin_ReturnsOkResult()
    {
        // Arrange
        var idCoin = _fixture.Create<int>();

        // Act
        var result = await _controller.DeleteCoin(idCoin);

        // Assert
        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be($"Coin with ID {idCoin} deleted successfully.");
    }

    [Fact]
    public async Task InsertTradingPair_CallsRepository()
    {
        // Arrange
        var tradingPair = _fixture.Create<TradingPairNew>();

        // Act
        await _controller.InsertTradingPair(tradingPair);

        // Assert
        _mockRepository.Verify(repo => repo.InsertTradingPair(tradingPair), Times.Once);
    }

    [Fact]
    public async Task InsertTradingPair_ReturnsOkResult()
    {
        // Arrange
        var tradingPair = _fixture.Create<TradingPairNew>();

        // Act
        var result = await _controller.InsertTradingPair(tradingPair);

        // Assert
        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be("Trading pair inserted successfully.");
    }
}
