using FluentResults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using SharedLibrary.Enums;
using SharedLibrary.Extensions.Testing;
using SVC_External.ApiContracts.Requests;
using SVC_External.ExternalClients.Exchanges;
using SVC_External.ExternalClients.Exchanges.Contracts.Requests;
using SVC_External.ExternalClients.Exchanges.Contracts.Responses;
using SVC_External.Services.Exchanges;

namespace SVC_External.Tests.Unit.Services.Exchanges;

public class KlineDataServiceTests
{
    private readonly IFixture _fixture;
    private readonly Mock<IExchangesClient> _firstClientMock;
    private readonly Mock<IExchangesClient> _secondClientMock;
    private readonly FakeLogger<KlineDataService> _logger;
    private readonly KlineDataService _klineDataService;

    public KlineDataServiceTests()
    {
        _fixture = new Fixture();

        _firstClientMock = new Mock<IExchangesClient>();
        _firstClientMock.Setup(client => client.CurrentExchange).Returns(Exchange.Binance);

        _secondClientMock = new Mock<IExchangesClient>();
        _secondClientMock.Setup(client => client.CurrentExchange).Returns(Exchange.Bybit);

        _logger = new FakeLogger<KlineDataService>();

        _klineDataService = new KlineDataService(
            [_firstClientMock.Object, _secondClientMock.Object],
            _logger
        );
    }

    [Fact]
    public async Task GetKlineDataForTradingPair_TriesAllClients_UntilDataIsFound()
    {
        // Arrange
        var request = TestData.KlineDataRequest();
        var exchangeKlineData = _fixture.CreateMany<ExchangeKlineData>();
        var expectedResult = Result.Ok(exchangeKlineData);

        _firstClientMock
            .Setup(client => client.GetKlineData(It.IsAny<ExchangeKlineDataRequest>()))
            .ReturnsAsync(Result.Ok<IEnumerable<ExchangeKlineData>>([]));
        _secondClientMock
            .Setup(client => client.GetKlineData(It.IsAny<ExchangeKlineDataRequest>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _klineDataService.GetKlineDataForTradingPair(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IdTradingPair.Should().Be(request.TradingPair.Id);
        result.Value.KlineData.Should().HaveCount(exchangeKlineData.Count());
        result.Value.KlineData.Should().BeEquivalentTo(exchangeKlineData);
        _firstClientMock.Verify(
            client => client.GetKlineData(It.IsAny<ExchangeKlineDataRequest>()),
            Times.Once
        );
        _secondClientMock.Verify(
            client => client.GetKlineData(It.IsAny<ExchangeKlineDataRequest>()),
            Times.Once
        );
    }

    [Fact]
    public async Task GetKlineDataForTradingPair_ReturnsExpectedResult_WhenDataIsFound()
    {
        // Arrange
        var request = TestData.KlineDataRequest();
        var exchangeKlineData = _fixture.CreateMany<ExchangeKlineData>();
        var expectedResult = Result.Ok(exchangeKlineData);

        _firstClientMock
            .Setup(client => client.GetKlineData(It.IsAny<ExchangeKlineDataRequest>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _klineDataService.GetKlineDataForTradingPair(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IdTradingPair.Should().Be(request.TradingPair.Id);
        result.Value.KlineData.Should().HaveCount(exchangeKlineData.Count());
        result.Value.KlineData.Should().BeEquivalentTo(exchangeKlineData);
    }

    [Fact]
    public async Task GetKlineDataForTradingPair_ReturnsEmptyResponse_WhenNoKlineDataFound()
    {
        // Arrange
        var request = TestData.KlineDataRequest();
        _firstClientMock
            .Setup(client => client.GetKlineData(It.IsAny<ExchangeKlineDataRequest>()))
            .ReturnsAsync(Result.Ok<IEnumerable<ExchangeKlineData>>([]));
        _secondClientMock
            .Setup(client => client.GetKlineData(It.IsAny<ExchangeKlineDataRequest>()))
            .ReturnsAsync(Result.Ok<IEnumerable<ExchangeKlineData>>([]));

        // Act
        var result = await _klineDataService.GetKlineDataForTradingPair(request);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
    }

    [Fact]
    public async Task GetKlineDataForTradingPair_LogsWarning_WhenNoDataFound()
    {
        // Arrange
        var request = TestData.KlineDataRequest();
        _firstClientMock
            .Setup(client => client.GetKlineData(It.IsAny<ExchangeKlineDataRequest>()))
            .ReturnsAsync(Result.Ok<IEnumerable<ExchangeKlineData>>([]));
        _secondClientMock
            .Setup(client => client.GetKlineData(It.IsAny<ExchangeKlineDataRequest>()))
            .ReturnsAsync(Result.Ok<IEnumerable<ExchangeKlineData>>([]));

        // Act
        await _klineDataService.GetKlineDataForTradingPair(request);

        // Assert
        _logger.VerifyWasCalled(LogLevel.Warning, request.CoinMain.Symbol);
    }

    [Fact]
    public async Task GetFirstSuccessfulKlineDataPerCoin_ReturnsExpectedResults_WhenDataIsFound()
    {
        // Arrange
        var request = TestData.CreateKlineDataBatchRequest();
        var exchangeKlineData = _fixture.CreateMany<ExchangeKlineData>();
        var expectedResult = Result.Ok(exchangeKlineData);

        _firstClientMock
            .Setup(client => client.GetKlineData(It.IsAny<ExchangeKlineDataRequest>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _klineDataService.GetFirstSuccessfulKlineDataPerCoin(request);

        // Assert
        result.Should().HaveCount(request.MainCoins.Count());
        result.All(response => response.KlineData.Any()).Should().BeTrue();
    }

    [Fact]
    public async Task GetFirstSuccessfulKlineDataPerCoin_ReturnsPartial_IfForAnyCoinNoKlineDataFound()
    {
        // Arrange
        var request = TestData.CreateKlineDataBatchRequest(2);
        var exchangeKlineData = _fixture.CreateMany<ExchangeKlineData>();
        var expectedResult = Result.Ok(exchangeKlineData);

        // Setup first client to succeed for first coin but fail for second coin
        _firstClientMock
            .Setup(client =>
                client.GetKlineData(
                    It.Is<ExchangeKlineDataRequest>(r =>
                        r.CoinMainSymbol == request.MainCoins.First().Symbol
                    )
                )
            )
            .ReturnsAsync(expectedResult);

        _firstClientMock
            .Setup(client =>
                client.GetKlineData(
                    It.Is<ExchangeKlineDataRequest>(r =>
                        r.CoinMainSymbol == request.MainCoins.Last().Symbol
                    )
                )
            )
            .ReturnsAsync(Result.Ok<IEnumerable<ExchangeKlineData>>([]));

        // Setup second client to fail for both coins
        _secondClientMock
            .Setup(client => client.GetKlineData(It.IsAny<ExchangeKlineDataRequest>()))
            .ReturnsAsync(Result.Ok<IEnumerable<ExchangeKlineData>>([]));

        // Act
        var result = await _klineDataService.GetFirstSuccessfulKlineDataPerCoin(request);

        // Assert
        result.Should().HaveCount(1);
        var tradingPairId = request.MainCoins.First().TradingPairs.First().Id;
        var response = result.Should().ContainSingle(r => r.IdTradingPair == tradingPairId).Subject;
        response.KlineData.Should().HaveCount(exchangeKlineData.Count());
    }

    [Fact]
    public async Task GetFirstSuccessfulKlineDataPerCoin_LogsWarning_WhenNoDataFoundForCoin()
    {
        // Arrange
        var request = TestData.CreateKlineDataBatchRequest(1);

        _firstClientMock
            .Setup(client => client.GetKlineData(It.IsAny<ExchangeKlineDataRequest>()))
            .ReturnsAsync(Result.Ok<IEnumerable<ExchangeKlineData>>([]));
        _secondClientMock
            .Setup(client => client.GetKlineData(It.IsAny<ExchangeKlineDataRequest>()))
            .ReturnsAsync(Result.Ok<IEnumerable<ExchangeKlineData>>([]));

        // Act
        await _klineDataService.GetFirstSuccessfulKlineDataPerCoin(request);

        // Assert
        _logger.VerifyWasCalled(LogLevel.Warning, request.MainCoins.First().Symbol);
    }

    [Fact]
    public async Task GetFirstSuccessfulKlineDataPerCoin_ReturnsEmptyCollection_WhenNoCoinsHaveData()
    {
        // Arrange
        var request = TestData.CreateKlineDataBatchRequest(2);

        _firstClientMock
            .Setup(client => client.GetKlineData(It.IsAny<ExchangeKlineDataRequest>()))
            .ReturnsAsync(Result.Ok<IEnumerable<ExchangeKlineData>>([]));
        _secondClientMock
            .Setup(client => client.GetKlineData(It.IsAny<ExchangeKlineDataRequest>()))
            .ReturnsAsync(Result.Ok<IEnumerable<ExchangeKlineData>>([]));

        // Act
        var result = await _klineDataService.GetFirstSuccessfulKlineDataPerCoin(request);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetFirstSuccessfulKlineDataPerCoin_TriesMultipleTradingPairs_UntilSuccessful()
    {
        // Arrange
        var request = TestData.KlineDataBatchRequestWithMultipleTradingPairs();
        var exchangeKlineData = _fixture.CreateMany<ExchangeKlineData>().ToList();

        // First trading pair fails, second succeeds
        _firstClientMock
            .Setup(client =>
                client.GetKlineData(
                    It.Is<ExchangeKlineDataRequest>(r =>
                        r.CoinQuoteSymbol
                        == request.MainCoins.First().TradingPairs.First().CoinQuote.Symbol
                    )
                )
            )
            .ReturnsAsync(Result.Ok<IEnumerable<ExchangeKlineData>>([]));

        _firstClientMock
            .Setup(client =>
                client.GetKlineData(
                    It.Is<ExchangeKlineDataRequest>(r =>
                        r.CoinQuoteSymbol
                        == request.MainCoins.First().TradingPairs.Last().CoinQuote.Symbol
                    )
                )
            )
            .ReturnsAsync(exchangeKlineData);

        // Act
        var result = await _klineDataService.GetFirstSuccessfulKlineDataPerCoin(request);

        // Assert
        result.Should().HaveCount(1);
        var expectedTradingPairId = request.MainCoins.First().TradingPairs.Last().Id;
        result.Should().ContainSingle(r => r.IdTradingPair == expectedTradingPairId);
    }

    [Fact]
    public async Task GetFirstSuccessfulKlineDataPerCoin_TriesDifferentExchanges_ForEachTradingPair()
    {
        // Arrange
        var request = TestData.KlineDataBatchRequestWithMultipleExchanges();
        var exchangeKlineData = _fixture.CreateMany<ExchangeKlineData>().ToList();

        _firstClientMock
            .Setup(client => client.GetKlineData(It.IsAny<ExchangeKlineDataRequest>()))
            .ReturnsAsync(Result.Ok<IEnumerable<ExchangeKlineData>>([]));
        _secondClientMock
            .Setup(client => client.GetKlineData(It.IsAny<ExchangeKlineDataRequest>()))
            .ReturnsAsync(exchangeKlineData);

        // Act
        await _klineDataService.GetFirstSuccessfulKlineDataPerCoin(request);

        // Assert
        _firstClientMock.Verify(
            client => client.GetKlineData(It.IsAny<ExchangeKlineDataRequest>()),
            Times.Once
        );
        _secondClientMock.Verify(
            client => client.GetKlineData(It.IsAny<ExchangeKlineDataRequest>()),
            Times.Once
        );
    }

    private static class TestData
    {
        public static KlineDataRequest KlineDataRequest()
        {
            return new KlineDataRequest
            {
                CoinMain = new KlineDataRequestCoinMain
                {
                    Id = 1,
                    Symbol = "BTC",
                    Name = "Bitcoin",
                    TradingPairs = [],
                },
                TradingPair = new KlineDataRequestTradingPair
                {
                    Id = 1,
                    CoinQuote = new KlineDataRequestCoinQuote
                    {
                        Id = 2,
                        Symbol = "USDT",
                        Name = "Tether",
                    },
                    Exchanges = [Exchange.Binance, Exchange.Bybit],
                },
                Interval = ExchangeKlineInterval.OneDay,
                StartTime = DateTime.UtcNow.AddDays(-7),
                EndTime = DateTime.UtcNow,
                Limit = 100,
            };
        }

        public static KlineDataBatchRequest CreateKlineDataBatchRequest(int coinCount = 1)
        {
            var mainCoins = new List<KlineDataRequestCoinMain>();

            for (int i = 0; i < coinCount; i++)
            {
                mainCoins.Add(
                    new KlineDataRequestCoinMain
                    {
                        Id = i + 1,
                        Symbol = $"BTC{i}",
                        Name = $"Bitcoin {i}",
                        TradingPairs =
                        [
                            new KlineDataRequestTradingPair
                            {
                                Id = i + 1,
                                CoinQuote = new KlineDataRequestCoinQuote
                                {
                                    Id = 100 + i,
                                    Symbol = "USDT",
                                    Name = "Tether",
                                },
                                Exchanges = [Exchange.Binance],
                            },
                        ],
                    }
                );
            }

            return new KlineDataBatchRequest
            {
                MainCoins = mainCoins,
                Interval = ExchangeKlineInterval.OneDay,
                StartTime = DateTime.UtcNow.AddDays(-7),
                EndTime = DateTime.UtcNow,
                Limit = 100,
            };
        }

        public static KlineDataBatchRequest KlineDataBatchRequestWithMultipleTradingPairs()
        {
            return new KlineDataBatchRequest
            {
                MainCoins =
                [
                    new KlineDataRequestCoinMain
                    {
                        Id = 1,
                        Symbol = "BTC",
                        Name = "Bitcoin",
                        TradingPairs =
                        [
                            new KlineDataRequestTradingPair
                            {
                                Id = 1,
                                CoinQuote = new KlineDataRequestCoinQuote
                                {
                                    Id = 2,
                                    Symbol = "USDT",
                                    Name = "Tether",
                                },
                                Exchanges = [Exchange.Binance],
                            },
                            new KlineDataRequestTradingPair
                            {
                                Id = 2,
                                CoinQuote = new KlineDataRequestCoinQuote
                                {
                                    Id = 3,
                                    Symbol = "EUR",
                                    Name = "Euro",
                                },
                                Exchanges = [Exchange.Binance],
                            },
                        ],
                    },
                ],
                Interval = ExchangeKlineInterval.OneDay,
                StartTime = DateTime.UtcNow.AddDays(-7),
                EndTime = DateTime.UtcNow,
                Limit = 100,
            };
        }

        public static KlineDataBatchRequest KlineDataBatchRequestWithMultipleExchanges()
        {
            return new KlineDataBatchRequest
            {
                MainCoins =
                [
                    new KlineDataRequestCoinMain
                    {
                        Id = 1,
                        Symbol = "BTC",
                        Name = "Bitcoin",
                        TradingPairs =
                        [
                            new KlineDataRequestTradingPair
                            {
                                Id = 1,
                                CoinQuote = new KlineDataRequestCoinQuote
                                {
                                    Id = 2,
                                    Symbol = "USDT",
                                    Name = "Tether",
                                },
                                Exchanges = [Exchange.Binance, Exchange.Bybit],
                            },
                        ],
                    },
                ],
                Interval = ExchangeKlineInterval.OneDay,
                StartTime = DateTime.UtcNow.AddDays(-7),
                EndTime = DateTime.UtcNow,
                Limit = 100,
            };
        }
    }
}
