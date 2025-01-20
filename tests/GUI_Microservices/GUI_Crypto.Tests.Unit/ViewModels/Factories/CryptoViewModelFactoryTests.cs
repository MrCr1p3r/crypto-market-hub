using AutoFixture;
using FluentAssertions;
using GUI_Crypto.Clients.Interfaces;
using GUI_Crypto.Models.Chart;
using GUI_Crypto.Models.Input;
using GUI_Crypto.Models.Output;
using GUI_Crypto.ViewModels.Factories;
using Moq;
using SharedLibrary.Enums;

namespace GUI_Crypto.Tests.Unit.ViewModels.Factories;

public class CryptoViewModelFactoryTests
{
    private readonly IFixture _fixture;
    private readonly Mock<ISvcCoinsClient> _svcCoinsClientMock;
    private readonly Mock<ISvcKlineClient> _svcKlineClientMock;
    private readonly Mock<ISvcExternalClient> _svcExternalClientMock;
    private readonly CryptoViewModelFactory _factory;

    public CryptoViewModelFactoryTests()
    {
        _fixture = new Fixture();
        _svcCoinsClientMock = new Mock<ISvcCoinsClient>();
        _svcKlineClientMock = new Mock<ISvcKlineClient>();
        _svcExternalClientMock = new Mock<ISvcExternalClient>();
        _factory = new CryptoViewModelFactory(
            _svcCoinsClientMock.Object,
            _svcKlineClientMock.Object,
            _svcExternalClientMock.Object
        );
    }

    [Fact]
    public async Task CreateOverviewViewModel_ShouldCallGetAllCoins()
    {
        // Arrange
        _svcCoinsClientMock.Setup(client => client.GetAllCoins()).ReturnsAsync(new List<Coin>());
        _svcKlineClientMock
            .Setup(client => client.GetAllKlineData())
            .ReturnsAsync(new Dictionary<int, IEnumerable<KlineData>>());

        // Act
        await _factory.CreateOverviewViewModel();

        // Assert
        _svcCoinsClientMock.Verify(client => client.GetAllCoins(), Times.Once);
    }

    [Fact]
    public async Task CreateOverviewViewModel_ShouldCallGetAllKlineData()
    {
        // Arrange
        _svcCoinsClientMock.Setup(client => client.GetAllCoins()).ReturnsAsync(new List<Coin>());
        _svcKlineClientMock
            .Setup(client => client.GetAllKlineData())
            .ReturnsAsync(new Dictionary<int, IEnumerable<KlineData>>());

        // Act
        await _factory.CreateOverviewViewModel();

        // Assert
        _svcKlineClientMock.Verify(client => client.GetAllKlineData(), Times.Once);
    }

    [Fact]
    public async Task CreateOverviewViewModel_ShouldReturnExpectedViewModel()
    {
        // Arrange
        var tradingPairId = _fixture.Create<int>();
        var coin = _fixture.Create<Coin>();
        coin.TradingPairs =
        [
            new TradingPair
            {
                Id = tradingPairId,
                CoinQuote = new TradingPairCoinQuote
                {
                    Id = _fixture.Create<int>(),
                    Symbol = "USDT",
                    Name = "Tether",
                },
            },
        ];
        var klineData = _fixture.CreateMany<KlineData>().ToList();
        var klineDataDict = new Dictionary<int, IEnumerable<KlineData>>
        {
            { tradingPairId, klineData },
        };

        _svcCoinsClientMock.Setup(client => client.GetAllCoins()).ReturnsAsync([coin]);
        _svcKlineClientMock.Setup(client => client.GetAllKlineData()).ReturnsAsync(klineDataDict);

        // Act
        var result = await _factory.CreateOverviewViewModel();

        // Assert
        result.Coins.Should().HaveCount(1);
        var resultCoin = result.Coins.First();
        resultCoin.Id.Should().Be(coin.Id);
        resultCoin.Symbol.Should().Be(coin.Symbol);
        resultCoin.Name.Should().Be(coin.Name);
        resultCoin.TradingPair.Should().BeEquivalentTo(coin.TradingPairs.First());
        resultCoin.KlineData.Should().BeEquivalentTo(klineData);
    }

    [Fact]
    public async Task CreateOverviewViewModel_WithNoMatchingKlineData_ShouldReturnEmptyKlineData()
    {
        // Arrange
        var coin = _fixture.Create<Coin>();
        coin.TradingPairs =
        [
            new TradingPair
            {
                Id = _fixture.Create<int>(),
                CoinQuote = new TradingPairCoinQuote
                {
                    Id = _fixture.Create<int>(),
                    Symbol = "USDT",
                    Name = "Tether",
                },
            },
        ];

        _svcCoinsClientMock.Setup(client => client.GetAllCoins()).ReturnsAsync([coin]);
        _svcKlineClientMock
            .Setup(client => client.GetAllKlineData())
            .ReturnsAsync(new Dictionary<int, IEnumerable<KlineData>>());

        // Act
        var result = await _factory.CreateOverviewViewModel();

        // Assert
        result.Coins.Should().HaveCount(1);
        var resultCoin = result.Coins.First();
        resultCoin.KlineData.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateChartViewModel_ShouldCallGetCoinsByIds()
    {
        // Arrange
        var request = _fixture.Create<CoinChartRequest>();
        var IdCoinMain = request.IdCoinMain;
        var coin = _fixture.Create<Coin>();
        _svcCoinsClientMock
            .Setup(client => client.GetCoinsByIds(new[] { IdCoinMain }))
            .ReturnsAsync([coin]);
        _svcExternalClientMock
            .Setup(client => client.GetKlineData(It.IsAny<KlineDataRequest>()))
            .ReturnsAsync([]);

        // Act
        await _factory.CreateChartViewModel(request);

        // Assert
        _svcCoinsClientMock.Verify(
            client => client.GetCoinsByIds(new[] { IdCoinMain }),
            Times.Once
        );
    }

    [Fact]
    public async Task CreateChartViewModel_ShouldCallGetKlineData()
    {
        // Arrange
        var request = _fixture.Create<CoinChartRequest>();
        var IdCoinMain = request.IdCoinMain;
        var coin = _fixture.Create<Coin>();
        _svcCoinsClientMock
            .Setup(client => client.GetCoinsByIds(new[] { IdCoinMain }))
            .ReturnsAsync([coin]);
        _svcExternalClientMock
            .Setup(client => client.GetKlineData(It.IsAny<KlineDataRequest>()))
            .ReturnsAsync([]);

        // Act
        await _factory.CreateChartViewModel(request);

        // Assert
        _svcExternalClientMock.Verify(
            client =>
                client.GetKlineData(
                    It.Is<KlineDataRequest>(r =>
                        r.CoinMainSymbol == request.SymbolCoinMain
                        && r.CoinQuoteSymbol == request.SymbolCoinQuote
                        && r.Interval == ExchangeKlineInterval.FifteenMinutes
                        && r.StartTime <= DateTime.UtcNow
                        && r.EndTime <= DateTime.UtcNow
                    )
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task CreateChartViewModel_ShouldReturnExpectedViewModel()
    {
        // Arrange
        var request = _fixture.Create<CoinChartRequest>();
        var coin = _fixture.Create<Coin>();
        var IdCoinMain = request.IdCoinMain;
        var klineData = _fixture.CreateMany<KlineDataExchange>().ToList();

        _svcCoinsClientMock
            .Setup(client => client.GetCoinsByIds(new[] { IdCoinMain }))
            .ReturnsAsync([coin]);
        _svcExternalClientMock
            .Setup(client => client.GetKlineData(It.IsAny<KlineDataRequest>()))
            .ReturnsAsync(klineData);

        // Act
        var result = await _factory.CreateChartViewModel(request);

        // Assert
        result.Coin.Id.Should().Be(coin.Id);
        result.Coin.Symbol.Should().Be(coin.Symbol);
        result.Coin.Name.Should().Be(coin.Name);
        result.Coin.TradingPairs.Should().BeEquivalentTo(coin.TradingPairs);
        result.Coin.SymbolCoinQuoteCurrent.Should().Be(request.SymbolCoinQuote);
        result.Coin.KlineData.Should().BeEquivalentTo(klineData);
    }
}
