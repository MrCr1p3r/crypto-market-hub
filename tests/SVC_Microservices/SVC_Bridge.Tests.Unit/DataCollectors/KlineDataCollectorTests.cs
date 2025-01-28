using AutoFixture;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SharedLibrary.Enums;
using SVC_Bridge.Clients.Interfaces;
using SVC_Bridge.DataCollectors;
using SVC_Bridge.DataDistributors.Interfaces;
using SVC_Bridge.Models.Input;
using SVC_Bridge.Models.Output;

namespace SVC_Bridge.Tests.Unit.DataCollectors;

public class KlineDataCollectorTests
{
    private readonly IFixture _fixture;
    private readonly Mock<ISvcCoinsClient> _mockCoinsClient;
    private readonly Mock<ISvcExternalClient> _mockExternalClient;
    private readonly Mock<IKlineDataDistributor> _mockKlineDataDistributor;
    private readonly Mock<ILogger<KlineDataCollector>> _mockLogger;
    private readonly KlineDataCollector _klineDataCollector;
    private readonly KlineDataUpdateRequest _validRequest;

    public KlineDataCollectorTests()
    {
        _fixture = new Fixture();

        _mockLogger = new Mock<ILogger<KlineDataCollector>>();

        _mockCoinsClient = new Mock<ISvcCoinsClient>();
        _mockCoinsClient
            .Setup(client => client.GetAllCoins())
            .ReturnsAsync(_fixture.CreateMany<Coin>(5));
        _mockCoinsClient
            .Setup(client => client.GetQuoteCoinsPrioritized())
            .ReturnsAsync(_fixture.CreateMany<Coin>(3));

        _mockExternalClient = new Mock<ISvcExternalClient>();
        _mockExternalClient
            .Setup(client => client.GetKlineData(It.IsAny<KlineDataRequest>()))
            .ReturnsAsync(_fixture.CreateMany<KlineData>(5));

        _mockKlineDataDistributor = new Mock<IKlineDataDistributor>();
        _mockKlineDataDistributor
            .Setup(distributor => distributor.InsertTradingPair(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(1);

        _klineDataCollector = new KlineDataCollector(
            _mockCoinsClient.Object,
            _mockExternalClient.Object,
            _mockKlineDataDistributor.Object,
            _mockLogger.Object
        );

        _validRequest = new KlineDataUpdateRequest
        {
            Interval = ExchangeKlineInterval.FourHours,
            StartTime = DateTime.UtcNow.AddDays(-7),
            EndTime = DateTime.UtcNow,
            Limit = 100,
        };
    }

    [Fact]
    public async Task CollectEntireKlineData_ShouldCall_GetAllCoins()
    {
        // Act
        await _klineDataCollector.CollectEntireKlineData(_validRequest);

        // Assert
        _mockCoinsClient.Verify(client => client.GetAllCoins(), Times.Once);
    }

    [Fact]
    public async Task CollectEntireKlineData_ShouldCall_GetQuoteCoinsPrioritized()
    {
        // Act
        await _klineDataCollector.CollectEntireKlineData(_validRequest);

        // Assert
        _mockCoinsClient.Verify(client => client.GetQuoteCoinsPrioritized(), Times.Once);
    }

    [Fact]
    public async Task CollectEntireKlineData_ShouldCall_GetKlineData()
    {
        // Act
        await _klineDataCollector.CollectEntireKlineData(_validRequest);

        // Assert
        _mockExternalClient.Verify(
            client =>
                client.GetKlineData(
                    It.Is<KlineDataRequest>(r =>
                        r.Interval == _validRequest.Interval
                        && r.StartTime == _validRequest.StartTime
                        && r.EndTime == _validRequest.EndTime
                        && r.Limit == _validRequest.Limit
                    )
                ),
            Times.AtLeastOnce
        );
    }

    [Fact]
    public async Task CollectEntireKlineData_ShouldReturnKlineData_WhenDataExists()
    {
        // Arrange
        var nonStableCoins = _fixture.Build<Coin>().With(c => c.IsStablecoin, false).CreateMany(2);
        var quoteCoins = _fixture.CreateMany<Coin>(1);
        var klineData = _fixture.CreateMany<KlineData>(2);

        _mockCoinsClient.Setup(client => client.GetAllCoins()).ReturnsAsync(nonStableCoins);
        _mockCoinsClient
            .Setup(client => client.GetQuoteCoinsPrioritized())
            .ReturnsAsync(quoteCoins);
        _mockExternalClient
            .Setup(client => client.GetKlineData(It.IsAny<KlineDataRequest>()))
            .ReturnsAsync(klineData);

        var expectedResult = nonStableCoins.SelectMany(mainCoin =>
            klineData.Select(kd => new KlineDataNew
            {
                IdTradePair = 1,
                OpenTime = kd.OpenTime,
                OpenPrice = kd.OpenPrice,
                HighPrice = kd.HighPrice,
                LowPrice = kd.LowPrice,
                ClosePrice = kd.ClosePrice,
                Volume = kd.Volume,
                CloseTime = kd.CloseTime,
            })
        );

        // Act
        var result = await _klineDataCollector.CollectEntireKlineData(_validRequest);

        // Assert
        result.Should().BeEquivalentTo(expectedResult);
    }

    [Fact]
    public async Task CollectEntireKlineData_ShouldExcludeStablecoins()
    {
        // Arrange
        var stableCoin = _fixture
            .Build<Coin>()
            .With(c => c.IsStablecoin, true)
            .With(c => c.Symbol, "USDT")
            .Create();
        var nonStableCoin = _fixture
            .Build<Coin>()
            .With(c => c.IsStablecoin, false)
            .With(c => c.Symbol, "BTC")
            .Create();
        var allCoins = new[] { stableCoin, nonStableCoin };
        var quoteCoins = _fixture.CreateMany<Coin>(1);
        var klineData = _fixture.CreateMany<KlineData>(2);

        _mockCoinsClient.Setup(client => client.GetAllCoins()).ReturnsAsync(allCoins);
        _mockCoinsClient
            .Setup(client => client.GetQuoteCoinsPrioritized())
            .ReturnsAsync(quoteCoins);
        _mockExternalClient
            .Setup(client => client.GetKlineData(It.IsAny<KlineDataRequest>()))
            .ReturnsAsync(klineData);

        // Act
        await _klineDataCollector.CollectEntireKlineData(_validRequest);

        // Assert
        _mockExternalClient.Verify(
            client =>
                client.GetKlineData(
                    It.Is<KlineDataRequest>(r => r.CoinMainSymbol == nonStableCoin.Symbol)
                ),
            Times.AtLeastOnce
        );
        _mockExternalClient.Verify(
            client =>
                client.GetKlineData(
                    It.Is<KlineDataRequest>(r => r.CoinMainSymbol == stableCoin.Symbol)
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task CollectEntireKlineData_ShouldLogWarning_WhenNoKlineDataIsFetched()
    {
        // Arrange
        _mockExternalClient
            .Setup(client => client.GetKlineData(It.IsAny<KlineDataRequest>()))
            .ReturnsAsync([]);

        // Act
        var result = await _klineDataCollector.CollectEntireKlineData(_validRequest);

        // Assert
        result.Should().BeEmpty();
        _mockLogger.Verify(
            logger =>
                logger.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (o, t) => o.ToString()!.Contains("No kline data could be fetched")
                    ),
                    null,
                    It.Is<Func<It.IsAnyType, Exception?, string>>((state, ex) => true)
                ),
            Times.AtLeastOnce
        );
    }
}
