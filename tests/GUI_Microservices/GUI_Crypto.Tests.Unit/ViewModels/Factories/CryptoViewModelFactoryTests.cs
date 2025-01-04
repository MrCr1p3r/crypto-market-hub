using AutoFixture;
using FluentAssertions;
using GUI_Crypto.Clients.Interfaces;
using GUI_Crypto.Models.Output;
using GUI_Crypto.ViewModels.Factories;
using Moq;

namespace GUI_Crypto.Tests.Unit.ViewModels.Factories;

public class CryptoViewModelFactoryTests
{
    private readonly IFixture _fixture;
    private readonly Mock<ISvcCoinsClient> _svcCoinsClientMock;
    private readonly Mock<ISvcKlineClient> _svcKlineClientMock;
    private readonly CryptoViewModelFactory _factory;

    public CryptoViewModelFactoryTests()
    {
        _fixture = new Fixture();
        _svcCoinsClientMock = new Mock<ISvcCoinsClient>();
        _svcKlineClientMock = new Mock<ISvcKlineClient>();
        _factory = new CryptoViewModelFactory(
            _svcCoinsClientMock.Object,
            _svcKlineClientMock.Object
        );
    }

    [Fact]
    public async Task CreateOverviewViewModel_ShouldCallGetAllCoins()
    {
        // Arrange
        _svcCoinsClientMock.Setup(client => client.GetAllCoins()).ReturnsAsync([]);
        _svcKlineClientMock.Setup(client => client.GetAllKlineData()).ReturnsAsync([]);

        // Act
        await _factory.CreateOverviewViewModel();

        // Assert
        _svcCoinsClientMock.Verify(client => client.GetAllCoins(), Times.Once);
    }

    [Fact]
    public async Task CreateOverviewViewModel_ShouldCallGetAllKlineData()
    {
        // Arrange
        _svcCoinsClientMock.Setup(client => client.GetAllCoins()).ReturnsAsync([]);
        _svcKlineClientMock.Setup(client => client.GetAllKlineData()).ReturnsAsync([]);

        // Act
        await _factory.CreateOverviewViewModel();

        // Assert
        _svcKlineClientMock.Verify(client => client.GetAllKlineData(), Times.Once);
    }

    [Fact]
    public async Task CreateOverviewViewModel_ShouldReturnExpectedViewModel()
    {
        // Arrange
        var coins = _fixture.CreateMany<Coin>().ToList();
        var klineData = _fixture.CreateMany<KlineData>().ToList();

        _svcCoinsClientMock.Setup(client => client.GetAllCoins()).ReturnsAsync(coins);
        _svcKlineClientMock.Setup(client => client.GetAllKlineData()).ReturnsAsync(klineData);

        // Act
        var result = await _factory.CreateOverviewViewModel();

        // Assert
        result.Coins.Should().HaveSameCount(coins);
        foreach (var coin in result.Coins)
        {
            var expectedCoin = coins.First(c => c.Id == coin.Id);
            coin.Symbol.Should().Be(expectedCoin.Symbol);
            coin.Name.Should().Be(expectedCoin.Name);
            coin.KlineData.Should()
                .BeEquivalentTo(
                    klineData.Where(k =>
                        expectedCoin.TradingPairs.Any(tp => tp.Id == k.IdTradePair)
                    )
                );
        }
    }
}
