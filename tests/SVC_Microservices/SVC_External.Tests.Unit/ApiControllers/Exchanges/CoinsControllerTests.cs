using AutoFixture.AutoMoq;
using FluentResults;
using Microsoft.AspNetCore.Mvc;
using SVC_External.ApiContracts.Responses.Exchanges.Coins;
using SVC_External.ApiControllers.Exchanges;
using SVC_External.Services.Exchanges.Interfaces;

namespace SVC_External.Tests.Unit.ApiControllers.Exchanges;

public class CoinsControllerTests
{
    private readonly IFixture _fixture;
    private readonly Mock<ICoinsService> _mockCoinsService;
    private readonly CoinsController _controller;

    public CoinsControllerTests()
    {
        _fixture = new Fixture().Customize(new AutoMoqCustomization());
        _mockCoinsService = _fixture.Freeze<Mock<ICoinsService>>();
        _controller = new CoinsController(_mockCoinsService.Object);
    }

    [Fact]
    public async Task GetAllSpotCoins_CallsCoinsService()
    {
        // Arrange
        var expectedCoins = _fixture.CreateMany<Coin>().ToList();
        _mockCoinsService
            .Setup(service => service.GetAllCurrentActiveSpotCoins())
            .ReturnsAsync(Result.Ok<IEnumerable<Coin>>(expectedCoins));

        // Act
        await _controller.GetAllSpotCoins();

        // Assert
        _mockCoinsService.Verify(service => service.GetAllCurrentActiveSpotCoins(), Times.Once);
    }

    [Fact]
    public async Task GetAllSpotCoins_ReturnsOkResultWithExpectedData()
    {
        // Arrange
        var expectedCoins = _fixture.CreateMany<Coin>().ToList();
        _mockCoinsService
            .Setup(service => service.GetAllCurrentActiveSpotCoins())
            .ReturnsAsync(Result.Ok<IEnumerable<Coin>>(expectedCoins));

        // Act
        var result = await _controller.GetAllSpotCoins();

        // Assert
        result
            .Should()
            .BeOfType<OkObjectResult>()
            .Which.Value.Should()
            .BeEquivalentTo(expectedCoins);
    }

    [Fact]
    public async Task GetAllSpotCoins_ReturnsInternalServerError_WhenServiceFails()
    {
        // Arrange
        _mockCoinsService
            .Setup(service => service.GetAllCurrentActiveSpotCoins())
            .ReturnsAsync(Result.Fail("Service error"));

        // Act
        var result = await _controller.GetAllSpotCoins();

        // Assert
        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }
}
