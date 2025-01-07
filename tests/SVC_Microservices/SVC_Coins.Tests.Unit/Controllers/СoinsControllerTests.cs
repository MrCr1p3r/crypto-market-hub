using AutoFixture;
using FluentAssertions;
using FluentResults;
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
        _mockRepository = new Mock<ICoinsRepository>();
        _controller = new CoinsController(_mockRepository.Object);
    }

    [Fact]
    public async Task InsertCoin_CallsRepository()
    {
        // Arrange
        var coin = _fixture.Create<CoinNew>();
        _mockRepository.Setup(repo => repo.InsertCoin(coin)).ReturnsAsync(Result.Ok());

        // Act
        await _controller.InsertCoin(coin);

        // Assert
        _mockRepository.Verify(repo => repo.InsertCoin(coin), Times.Once);
    }

    [Fact]
    public async Task InsertCoin_ReturnsNoContent_WhenSuccessful()
    {
        // Arrange
        var coin = _fixture.Create<CoinNew>();
        _mockRepository.Setup(repo => repo.InsertCoin(coin)).ReturnsAsync(Result.Ok());

        // Act
        var result = await _controller.InsertCoin(coin);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _mockRepository.Verify(repo => repo.InsertCoin(coin), Times.Once);
    }

    [Fact]
    public async Task InsertCoin_ReturnsConflict_WhenCoinAlreadyExists()
    {
        // Arrange
        var coin = _fixture.Create<CoinNew>();
        _mockRepository
            .Setup(repo => repo.InsertCoin(coin))
            .ReturnsAsync(Result.Fail("Coin already exists in the database."));

        // Act
        var result = await _controller.InsertCoin(coin);

        // Assert
        result
            .Should()
            .BeOfType<ConflictObjectResult>()
            .Which.Value.Should()
            .Be("Coin already exists in the database.");
        _mockRepository.Verify(repo => repo.InsertCoin(coin), Times.Once);
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
        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(coinsList);
    }

    [Fact]
    public async Task GetAllCoins_ReturnsOkWithEmptyList_WhenNoCoinsExist()
    {
        // Arrange
        var emptyList = new List<Coin>();
        _mockRepository.Setup(repo => repo.GetAllCoins()).ReturnsAsync(emptyList);

        // Act
        var result = await _controller.GetAllCoins();

        // Assert
        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(emptyList);
        _mockRepository.Verify(repo => repo.GetAllCoins(), Times.Once);
    }

    [Fact]
    public async Task DeleteCoin_CallsRepository()
    {
        // Arrange
        var idCoin = _fixture.Create<int>();
        _mockRepository.Setup(repo => repo.DeleteCoin(idCoin)).ReturnsAsync(Result.Ok());

        // Act
        await _controller.DeleteCoin(idCoin);

        // Assert
        _mockRepository.Verify(repo => repo.DeleteCoin(idCoin), Times.Once);
    }

    [Fact]
    public async Task DeleteCoin_ReturnsNoContent_WhenSuccessful()
    {
        // Arrange
        var idCoin = _fixture.Create<int>();
        _mockRepository.Setup(repo => repo.DeleteCoin(idCoin)).ReturnsAsync(Result.Ok());

        // Act
        var result = await _controller.DeleteCoin(idCoin);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _mockRepository.Verify(repo => repo.DeleteCoin(idCoin), Times.Once);
    }

    [Fact]
    public async Task DeleteCoin_ReturnsNotFound_WhenCoinDoesNotExist()
    {
        // Arrange
        var idCoin = _fixture.Create<int>();
        _mockRepository
            .Setup(repo => repo.DeleteCoin(idCoin))
            .ReturnsAsync(Result.Fail($"Coin with ID {idCoin} not found."));

        // Act
        var result = await _controller.DeleteCoin(idCoin);

        // Assert
        result
            .Should()
            .BeOfType<NotFoundObjectResult>()
            .Which.Value.Should()
            .Be($"Coin with ID {idCoin} not found.");
        _mockRepository.Verify(repo => repo.DeleteCoin(idCoin), Times.Once);
    }

    [Fact]
    public async Task InsertTradingPair_CallsRepository()
    {
        // Arrange
        var tradingPair = _fixture.Create<TradingPairNew>();
        _mockRepository
            .Setup(repo => repo.InsertTradingPair(tradingPair))
            .ReturnsAsync(Result.Ok(1));

        // Act
        await _controller.InsertTradingPair(tradingPair);

        // Assert
        _mockRepository.Verify(repo => repo.InsertTradingPair(tradingPair), Times.Once);
    }

    [Fact]
    public async Task InsertTradingPair_ReturnsOkWithId_WhenSuccessful()
    {
        // Arrange
        var tradingPair = _fixture.Create<TradingPairNew>();
        var expectedId = _fixture.Create<int>();

        _mockRepository
            .Setup(repo => repo.InsertTradingPair(tradingPair))
            .ReturnsAsync(Result.Ok(expectedId));

        // Act
        var result = await _controller.InsertTradingPair(tradingPair);

        // Assert
        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(expectedId);
        _mockRepository.Verify(repo => repo.InsertTradingPair(tradingPair), Times.Once);
    }

    [Fact]
    public async Task InsertTradingPair_ReturnsBadRequest_WhenInsertionFails()
    {
        // Arrange
        var tradingPair = _fixture.Create<TradingPairNew>();
        _mockRepository
            .Setup(repo => repo.InsertTradingPair(tradingPair))
            .ReturnsAsync(Result.Fail("This trading pair already exists."));

        // Act
        var result = await _controller.InsertTradingPair(tradingPair);

        // Assert
        result
            .Should()
            .BeOfType<BadRequestObjectResult>()
            .Which.Value.Should()
            .Be("This trading pair already exists.");
        _mockRepository.Verify(repo => repo.InsertTradingPair(tradingPair), Times.Once);
    }

    [Fact]
    public async Task GetQuoteCoinsPrioritized_CallsRepository()
    {
        // Arrange
        var prioritizedCoins = _fixture.CreateMany<Coin>(5);
        _mockRepository
            .Setup(repo => repo.GetQuoteCoinsPrioritized())
            .ReturnsAsync(prioritizedCoins);

        // Act
        await _controller.GetQuoteCoinsPrioritized();

        // Assert
        _mockRepository.Verify(repo => repo.GetQuoteCoinsPrioritized(), Times.Once);
    }

    [Fact]
    public async Task GetQuoteCoinsPrioritized_ReturnsOkResultWithExpectedData()
    {
        // Arrange
        var prioritizedCoins = _fixture.CreateMany<Coin>(5);
        _mockRepository
            .Setup(repo => repo.GetQuoteCoinsPrioritized())
            .ReturnsAsync(prioritizedCoins);

        // Act
        var result = await _controller.GetQuoteCoinsPrioritized();

        // Assert
        result
            .Should()
            .BeOfType<OkObjectResult>()
            .Which.Value.Should()
            .BeEquivalentTo(prioritizedCoins);
    }

    [Fact]
    public async Task GetQuoteCoinsPrioritized_ReturnsOkResultWhenNoData()
    {
        // Arrange
        var emptyList = Enumerable.Empty<Coin>();
        _mockRepository.Setup(repo => repo.GetQuoteCoinsPrioritized()).ReturnsAsync(emptyList);

        // Act
        var result = await _controller.GetQuoteCoinsPrioritized();

        // Assert
        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(emptyList);
    }

    [Fact]
    public async Task GetCoinsByIds_CallsRepository()
    {
        // Arrange
        var ids = new List<int> { 1, 2 };
        var coins = _fixture.CreateMany<Coin>(2);
        _mockRepository.Setup(repo => repo.GetCoinsByIds(ids)).ReturnsAsync(coins);

        // Act
        await _controller.GetCoinsByIds(ids);

        // Assert
        _mockRepository.Verify(repo => repo.GetCoinsByIds(ids), Times.Once);
    }

    [Fact]
    public async Task GetCoinsByIds_ReturnsOkResultWithExpectedData()
    {
        // Arrange
        var ids = new List<int> { 1, 2 };
        var coins = _fixture.CreateMany<Coin>(2);
        _mockRepository.Setup(repo => repo.GetCoinsByIds(ids)).ReturnsAsync(coins);

        // Act
        var result = await _controller.GetCoinsByIds(ids);

        // Assert
        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(coins);
    }

    [Fact]
    public async Task GetCoinsByIds_ReturnsOkWithEmptyList_WhenNoCoinsExist()
    {
        // Arrange
        var ids = new List<int> { 1, 2 };
        var emptyList = new List<Coin>();
        _mockRepository.Setup(repo => repo.GetCoinsByIds(ids)).ReturnsAsync(emptyList);

        // Act
        var result = await _controller.GetCoinsByIds(ids);

        // Assert
        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(emptyList);
    }
}
