using System.Collections.Frozen;
using AutoFixture;
using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Moq;
using SharedLibrary.Enums;
using SharedLibrary.Extensions.Testing;
using SVC_External.Clients.Exchanges.Interfaces;
using SVC_External.Clients.MarketDataProviders.Interfaces;
using SVC_External.DataCollectors;
using SVC_External.Models.Exchanges.Input;
using SVC_External.Models.Exchanges.Output;
using SVC_External.Models.Input;
using SVC_External.Models.MarketDataProviders.Output;
using SVC_External.Models.Output;

namespace SVC_External.Tests.Unit.DataCollectors;

public class ExchangesDataCollectorTests
{
    private readonly IFixture _fixture;
    private readonly Mock<IExchangesClient> _firstClientMock;
    private readonly Mock<IExchangesClient> _secondClientMock;
    private readonly Mock<ICoinGeckoClient> _coinGeckoClientMock;
    private readonly Mock<HybridCache> _hybridCacheMock;
    private readonly FakeLogger<ExchangesDataCollector> _logger;
    private readonly ExchangesDataCollector _dataCollector;

    public ExchangesDataCollectorTests()
    {
        _fixture = new Fixture();

        _firstClientMock = new Mock<IExchangesClient>();
        _firstClientMock.Setup(c => c.GetAllSpotCoins()).ReturnsAsync(TestData.exchangeCoins1);
        _firstClientMock.Setup(c => c.CurrentExchange).Returns(Exchange.Binance);

        _secondClientMock = new Mock<IExchangesClient>();
        _secondClientMock.Setup(c => c.GetAllSpotCoins()).ReturnsAsync(TestData.exchangeCoins2);
        _secondClientMock.Setup(c => c.CurrentExchange).Returns(Exchange.Bybit);

        _coinGeckoClientMock = new Mock<ICoinGeckoClient>();
        _coinGeckoClientMock.Setup(c => c.GetCoinsList()).ReturnsAsync(TestData.coinGeckoCoins);
        _coinGeckoClientMock
            .Setup(c => c.GetSymbolToIdMapForExchange(It.IsAny<string>()))
            .ReturnsAsync(TestData.symbolToIdMap);

        _hybridCacheMock = new Mock<HybridCache>();
        _hybridCacheMock.SetupGetOrCreateAsyncToExecuteFactory<
            Func<CancellationToken, ValueTask<IEnumerable<Coin>>>,
            IEnumerable<Coin>
        >("all_current_active_spot_coins");

        _logger = new FakeLogger<ExchangesDataCollector>();

        _dataCollector = new ExchangesDataCollector(
            [_firstClientMock.Object, _secondClientMock.Object],
            _coinGeckoClientMock.Object,
            _hybridCacheMock.Object,
            _logger
        );
    }

    [Fact]
    public async Task GetAllCurrentActiveSpotCoins_GetOrCreateAsyncCalled()
    {
        // Arrange
        var expectedCoins = _fixture.CreateMany<Coin>();
        _hybridCacheMock.SetupGetOrCreateAsync<
            Func<CancellationToken, ValueTask<IEnumerable<Coin>>>,
            IEnumerable<Coin>
        >("all_current_active_spot_coins", expectedCoins);

        // Act
        await _dataCollector.GetAllCurrentActiveSpotCoins();

        // Assert
        _hybridCacheMock.VerifyGetOrCreateAsyncCalled<
            Func<CancellationToken, ValueTask<IEnumerable<Coin>>>,
            IEnumerable<Coin>
        >("all_current_active_spot_coins", Times.Once());
    }

    [Fact]
    public async Task GetAllCurrentActiveSpotCoins_ReturnsResultFromCache()
    {
        // Arrange
        var expectedCoins = _fixture.CreateMany<Coin>();
        _hybridCacheMock.SetupGetOrCreateAsync<
            Func<CancellationToken, ValueTask<IEnumerable<Coin>>>,
            IEnumerable<Coin>
        >("all_current_active_spot_coins", expectedCoins);

        // Act
        var result = await _dataCollector.GetAllCurrentActiveSpotCoins();

        // Assert
        result.Should().BeEquivalentTo(expectedCoins);
    }

    [Fact]
    public async Task GetAllCurrentActiveSpotCoins_CallsSpotCoinsFromClients()
    {
        // Act
        var result = await _dataCollector.GetAllCurrentActiveSpotCoins();

        // Assert
        _firstClientMock.Verify(client => client.GetAllSpotCoins(), Times.Once);
        _secondClientMock.Verify(client => client.GetAllSpotCoins(), Times.Once);
    }

    [Fact]
    public async Task GetAllCurrentActiveSpotCoins_CallsCoinsFromCoinGecko()
    {
        // Arrange
        var expectedCoins = _fixture.CreateMany<CoinCoinGecko>();
        _coinGeckoClientMock.Setup(client => client.GetCoinsList()).ReturnsAsync(expectedCoins);

        // Act
        await _dataCollector.GetAllCurrentActiveSpotCoins();

        // Assert
        _coinGeckoClientMock.Verify(client => client.GetCoinsList(), Times.Once);
    }

    [Fact]
    public async Task GetAllCurrentActiveSpotCoins_CallsSymbolToIdMap() //TODO: do not hardcode ids
    {
        // Act
        await _dataCollector.GetAllCurrentActiveSpotCoins();

        // Assert
        _coinGeckoClientMock.Verify(
            client => client.GetSymbolToIdMapForExchange("binance"),
            Times.Once
        );
        _coinGeckoClientMock.Verify(
            client => client.GetSymbolToIdMapForExchange("bybit_spot"),
            Times.Once
        );
    }

    [Fact]
    public async Task GetAllCurrentActiveSpotCoins_ReturnsExpectedCoins()
    {
        // Act
        var result = await _dataCollector.GetAllCurrentActiveSpotCoins();

        // Assert
        result.Should().BeEquivalentTo(TestData.expectedCoins);
    }

    [Fact]
    public async Task GetAllCurrentActiveSpotCoins_LogsSymbolsWithoutNames()
    {
        // Arrange
        _coinGeckoClientMock
            .Setup(c => c.GetSymbolToIdMapForExchange(It.IsAny<string>()))
            .ReturnsAsync(
                new Dictionary<string, string?>
                {
                    { "BTC", "bitcoin" },
                    { "ETH", "ethereum" },
                    { "USDT", "usdt" },
                    // SOL is present but not in the map, should be logged as without name
                }.ToFrozenDictionary()
            );

        // Act
        await _dataCollector.GetAllCurrentActiveSpotCoins();

        // Assert
        _logger.VerifyWasCalled(LogLevel.Warning, "symbols");
        _logger.VerifyWasCalled(LogLevel.Warning, "SOL");
    }

    [Fact]
    public async Task GetAllCurrentActiveSpotCoins_LogsQuoteSymbolsWithoutNames()
    {
        // Arrange
        _coinGeckoClientMock
            .Setup(c => c.GetCoinsList())
            .ReturnsAsync(
                [
                    new()
                    {
                        Id = "bitcoin",
                        Symbol = "BTC",
                        Name = "Bitcoin",
                    },
                    new()
                    {
                        Id = "ethereum",
                        Symbol = "ETH",
                        Name = "Ethereum",
                    },
                    new()
                    {
                        Id = "solana",
                        Symbol = "SOL",
                        Name = "Solana",
                    },
                ]
            );

        _coinGeckoClientMock
            .Setup(c => c.GetSymbolToIdMapForExchange(It.IsAny<string>()))
            .ReturnsAsync(
                new Dictionary<string, string?>
                {
                    { "BTC", "bitcoin" },
                    { "ETH", "ethereum" },
                    { "SOL", "solana" },
                }.ToFrozenDictionary()
            );

        // Act
        await _dataCollector.GetAllCurrentActiveSpotCoins();

        // Assert
        _logger.VerifyWasCalled(LogLevel.Warning, "quote symbols");
        _logger.VerifyWasCalled(LogLevel.Warning, "USDT");
    }

    [Fact]
    public async Task GetAllCurrentActiveSpotCoins_LogsInactiveCoinGeckoCoins()
    {
        // Arrange
        var symbolToIdMap = new Dictionary<string, string?>
        {
            { "BTC", "bitcoin" },
            { "ETH", "ethereum" },
            { "SOL", "solana" }, // SOL has ID in map but is missing from coinGeckoCoins
            { "USDT", "usdt" },
            { "EUR", null },
        }.ToFrozenDictionary();

        _coinGeckoClientMock
            .Setup(c => c.GetSymbolToIdMapForExchange(It.IsAny<string>()))
            .ReturnsAsync(symbolToIdMap);

        _coinGeckoClientMock
            .Setup(c => c.GetCoinsList())
            .ReturnsAsync(
                [
                    new()
                    {
                        Id = "bitcoin",
                        Symbol = "BTC",
                        Name = "Bitcoin",
                    },
                    new()
                    {
                        Id = "ethereum",
                        Symbol = "ETH",
                        Name = "Ethereum",
                    },
                    new()
                    {
                        Id = "usdt",
                        Symbol = "USDT",
                        Name = "Tether",
                    },
                    // SOL with ID "solana" is missing - should be logged as inactive
                ]
            );

        // Act
        await _dataCollector.GetAllCurrentActiveSpotCoins();

        // Assert
        _logger.VerifyWasCalled(LogLevel.Warning, "inactive");
        _logger.VerifyWasCalled(LogLevel.Warning, "binance");
        _logger.VerifyWasCalled(LogLevel.Warning, "SOL");
    }

    #region GetKlineDataTests
    [Fact]
    public async Task GetKlineData_ReturnsExpectedResult_WhenDataIsFound()
    {
        // Arrange
        var request = TestData.KlineDataRequest();
        var exchangeKlineData = _fixture.CreateMany<ExchangeKlineData>().ToList();

        _firstClientMock
            .Setup(c => c.GetKlineData(It.IsAny<ExchangeKlineDataRequest>()))
            .ReturnsAsync(exchangeKlineData);

        // Act
        var result = await _dataCollector.GetKlineData(request);

        // Assert
        result.IdTradingPair.Should().Be(request.TradingPair.Id);
        result.KlineData.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetKlineData_CallsCorrectClient_BasedOnTradingPairExchanges()
    {
        // Arrange
        var request = TestData.KlineDataRequest();
        _firstClientMock
            .Setup(c => c.GetKlineData(It.IsAny<ExchangeKlineDataRequest>()))
            .ReturnsAsync([]);

        // Act
        await _dataCollector.GetKlineData(request);

        // Assert
        _firstClientMock.Verify(
            c => c.GetKlineData(It.IsAny<ExchangeKlineDataRequest>()),
            Times.Once
        );
    }

    [Fact]
    public async Task GetKlineData_ReturnsEmptyResponse_WhenNoDataFound()
    {
        // Arrange
        var request = TestData.KlineDataRequest();
        _firstClientMock
            .Setup(c => c.GetKlineData(It.IsAny<ExchangeKlineDataRequest>()))
            .ReturnsAsync([]);
        _secondClientMock
            .Setup(c => c.GetKlineData(It.IsAny<ExchangeKlineDataRequest>()))
            .ReturnsAsync([]);

        // Act
        var result = await _dataCollector.GetKlineData(request);

        // Assert
        result.Should().NotBeNull();
        result.KlineData.Should().BeEmpty();
    }

    [Fact]
    public async Task GetKlineData_LogsWarning_WhenNoDataFound()
    {
        // Arrange
        var request = TestData.KlineDataRequest();
        _firstClientMock
            .Setup(c => c.GetKlineData(It.IsAny<ExchangeKlineDataRequest>()))
            .ReturnsAsync([]);

        // Act
        await _dataCollector.GetKlineData(request);

        // Assert
        _logger.VerifyWasCalled(LogLevel.Warning, request.CoinMain.Symbol);
    }

    [Fact]
    public async Task GetKlineData_TriesAllClients_UntilDataIsFound()
    {
        // Arrange
        var request = TestData.KlineDataRequest();
        var exchangeKlineData = _fixture.CreateMany<ExchangeKlineData>().ToList();

        _firstClientMock
            .Setup(c => c.GetKlineData(It.IsAny<ExchangeKlineDataRequest>()))
            .ReturnsAsync([]);
        _secondClientMock
            .Setup(c => c.GetKlineData(It.IsAny<ExchangeKlineDataRequest>()))
            .ReturnsAsync(exchangeKlineData);

        // Act
        var result = await _dataCollector.GetKlineData(request);

        // Assert
        result.Should().NotBeNull();
        result.KlineData.Should().NotBeEmpty();
        _firstClientMock.Verify(
            c => c.GetKlineData(It.IsAny<ExchangeKlineDataRequest>()),
            Times.Once
        );
        _secondClientMock.Verify(
            c => c.GetKlineData(It.IsAny<ExchangeKlineDataRequest>()),
            Times.Once
        );
    }
    #endregion

    #region GetKlineDataBatchTests
    [Fact]
    public async Task GetKlineDataBatch_ReturnsExpectedResults_WhenDataIsFound()
    {
        // Arrange
        var request = TestData.CreateKlineDataBatchRequest();
        var exchangeKlineData = _fixture.CreateMany<ExchangeKlineData>().ToList();

        _firstClientMock
            .Setup(c => c.GetKlineData(It.IsAny<ExchangeKlineDataRequest>()))
            .ReturnsAsync(exchangeKlineData);

        // Act
        var result = await _dataCollector.GetKlineDataBatch(request);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(request.MainCoins.Count());
        result.All(r => r.KlineData.Any()).Should().BeTrue();
    }

    [Fact]
    public async Task GetKlineDataBatch_ReturnsOnlySuccessfulResults()
    {
        // Arrange
        var request = TestData.CreateKlineDataBatchRequest(2);
        var exchangeKlineData = _fixture.CreateMany<ExchangeKlineData>().ToList();

        // Setup first client to succeed for first coin but fail for second coin
        _firstClientMock
            .Setup(c =>
                c.GetKlineData(
                    It.Is<ExchangeKlineDataRequest>(r =>
                        r.CoinMainSymbol == request.MainCoins.First().Symbol
                    )
                )
            )
            .ReturnsAsync(exchangeKlineData);

        _firstClientMock
            .Setup(c =>
                c.GetKlineData(
                    It.Is<ExchangeKlineDataRequest>(r =>
                        r.CoinMainSymbol == request.MainCoins.Last().Symbol
                    )
                )
            )
            .ReturnsAsync([]);

        // Setup second client to fail for both coins
        _secondClientMock
            .Setup(c => c.GetKlineData(It.IsAny<ExchangeKlineDataRequest>()))
            .ReturnsAsync([]);

        // Act
        var result = await _dataCollector.GetKlineDataBatch(request);

        // Assert
        result.Should().HaveCount(1);
        result.First().IdTradingPair.Should().Be(request.MainCoins.First().TradingPairs.First().Id);
    }

    [Fact]
    public async Task GetKlineDataBatch_LogsWarning_WhenNoDataFoundForCoin()
    {
        // Arrange
        var request = TestData.CreateKlineDataBatchRequest(1);

        _firstClientMock
            .Setup(c => c.GetKlineData(It.IsAny<ExchangeKlineDataRequest>()))
            .ReturnsAsync([]);
        _secondClientMock
            .Setup(c => c.GetKlineData(It.IsAny<ExchangeKlineDataRequest>()))
            .ReturnsAsync([]);

        // Act
        await _dataCollector.GetKlineDataBatch(request);

        // Assert
        _logger.VerifyWasCalled(LogLevel.Warning, request.MainCoins.First().Symbol);
    }

    [Fact]
    public async Task GetKlineDataBatch_ReturnsEmptyList_WhenNoCoinsHaveData()
    {
        // Arrange
        var request = TestData.CreateKlineDataBatchRequest(2);

        _firstClientMock
            .Setup(c => c.GetKlineData(It.IsAny<ExchangeKlineDataRequest>()))
            .ReturnsAsync([]);
        _secondClientMock
            .Setup(c => c.GetKlineData(It.IsAny<ExchangeKlineDataRequest>()))
            .ReturnsAsync([]);

        // Act
        var result = await _dataCollector.GetKlineDataBatch(request);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetKlineDataBatch_TriesMultipleTradingPairs_UntilSuccessful()
    {
        // Arrange
        var request = TestData.KlineDataBatchRequestWithMultipleTradingPairs();
        var exchangeKlineData = _fixture.CreateMany<ExchangeKlineData>().ToList();

        // First trading pair fails, second succeeds
        _firstClientMock
            .Setup(c =>
                c.GetKlineData(
                    It.Is<ExchangeKlineDataRequest>(r =>
                        r.CoinQuoteSymbol
                        == request.MainCoins.First().TradingPairs.First().CoinQuote.Symbol
                    )
                )
            )
            .ReturnsAsync([]);

        _firstClientMock
            .Setup(c =>
                c.GetKlineData(
                    It.Is<ExchangeKlineDataRequest>(r =>
                        r.CoinQuoteSymbol
                        == request.MainCoins.First().TradingPairs.Last().CoinQuote.Symbol
                    )
                )
            )
            .ReturnsAsync(exchangeKlineData);

        // Act
        var result = await _dataCollector.GetKlineDataBatch(request);

        // Assert
        result.Should().HaveCount(1);
        result.First().IdTradingPair.Should().Be(request.MainCoins.First().TradingPairs.Last().Id);
    }

    [Fact]
    public async Task GetKlineDataBatch_TriesDifferentExchanges_ForEachTradingPair()
    {
        // Arrange
        var request = TestData.KlineDataBatchRequestWithMultipleExchanges();
        var exchangeKlineData = _fixture.CreateMany<ExchangeKlineData>().ToList();

        _secondClientMock
            .Setup(c => c.GetKlineData(It.IsAny<ExchangeKlineDataRequest>()))
            .ReturnsAsync(exchangeKlineData);

        // Act
        await _dataCollector.GetKlineDataBatch(request);

        // Assert
        _firstClientMock.Verify(
            c => c.GetKlineData(It.IsAny<ExchangeKlineDataRequest>()),
            Times.Once
        );
        _secondClientMock.Verify(
            c => c.GetKlineData(It.IsAny<ExchangeKlineDataRequest>()),
            Times.Once
        );
    }
    #endregion

    #region Helper Methods

    #endregion

    private static class TestData
    {
        public static IEnumerable<ExchangeCoin> exchangeCoins1 =
        [
            new()
            {
                Symbol = "BTC",
                Name = null,
                TradingPairs =
                [
                    new()
                    {
                        CoinQuote = new ExchangeTradingPairCoinQuote
                        {
                            Symbol = "USDT",
                            Name = null,
                        },
                        ExchangeInfo = new ExchangeTradingPairExchangeInfo
                        {
                            Exchange = Exchange.Binance,
                            Status = ExchangeTradingPairStatus.Available,
                        },
                    },
                    new()
                    {
                        CoinQuote = new ExchangeTradingPairCoinQuote
                        {
                            Symbol = "ETH",
                            Name = null,
                        },
                        ExchangeInfo = new ExchangeTradingPairExchangeInfo
                        {
                            Exchange = Exchange.Binance,
                            Status = ExchangeTradingPairStatus.Unavailable,
                        },
                    },
                ],
            },
            new()
            {
                Symbol = "SOL",
                Name = null,
                TradingPairs =
                [
                    new()
                    {
                        CoinQuote = new ExchangeTradingPairCoinQuote
                        {
                            Symbol = "EUR",
                            Name = null,
                        },
                        ExchangeInfo = new ExchangeTradingPairExchangeInfo
                        {
                            Exchange = Exchange.Binance,
                            Status = ExchangeTradingPairStatus.Available,
                        },
                    },
                ],
            },
        ];

        public static IEnumerable<ExchangeCoin> exchangeCoins2 =
        [
            new()
            {
                Symbol = "BTC",
                Name = null,
                TradingPairs =
                [
                    new()
                    {
                        CoinQuote = new ExchangeTradingPairCoinQuote
                        {
                            Symbol = "USDT",
                            Name = null,
                        },
                        ExchangeInfo = new ExchangeTradingPairExchangeInfo
                        {
                            Exchange = Exchange.Bybit,
                            Status = ExchangeTradingPairStatus.Available,
                        },
                    },
                    new()
                    {
                        CoinQuote = new ExchangeTradingPairCoinQuote
                        {
                            Symbol = "ETH",
                            Name = null,
                        },
                        ExchangeInfo = new ExchangeTradingPairExchangeInfo
                        {
                            Exchange = Exchange.Bybit,
                            Status = ExchangeTradingPairStatus.Available,
                        },
                    },
                ],
            },
            new()
            {
                Symbol = "ETH",
                Name = null,
                TradingPairs =
                [
                    new()
                    {
                        CoinQuote = new ExchangeTradingPairCoinQuote
                        {
                            Symbol = "USDT",
                            Name = null,
                        },
                        ExchangeInfo = new ExchangeTradingPairExchangeInfo
                        {
                            Exchange = Exchange.Bybit,
                            Status = ExchangeTradingPairStatus.Available,
                        },
                    },
                ],
            },
        ];

        public static IEnumerable<CoinCoinGecko> coinGeckoCoins =
        [
            new()
            {
                Id = "bitcoin",
                Symbol = "BTC",
                Name = "Bitcoin",
            },
            new()
            {
                Id = "ethereum",
                Symbol = "ETH",
                Name = "Ethereum",
            },
            new()
            {
                Id = "usdt",
                Symbol = "USDT",
                Name = "Tether",
            },
        ];

        public static FrozenDictionary<string, string?> symbolToIdMap = new Dictionary<
            string,
            string?
        >
        {
            { "BTC", "bitcoin" },
            { "ETH", "ethereum" },
            { "SOL", "solana" }, // Is present in map but not in coinGeckoCoins to reflect the behaviour of unactive coins on coingecko
            { "USDT", "usdt" },
            { "EUR", null },
        }.ToFrozenDictionary();

        public static IEnumerable<Coin> expectedCoins =
        [
            new()
            {
                Symbol = "BTC",
                Name = "Bitcoin",
                IdCoinGecko = "bitcoin",
                Category = null,
                TradingPairs =
                [
                    new()
                    {
                        CoinQuote = new TradingPairCoinQuote
                        {
                            Symbol = "USDT",
                            Name = "Tether",
                            Category = null,
                            IdCoinGecko = "usdt",
                        },
                        ExchangeInfos =
                        [
                            new TradingPairExchangeInfo
                            {
                                Exchange = Exchange.Binance,
                                Status = ExchangeTradingPairStatus.Available,
                            },
                            new TradingPairExchangeInfo
                            {
                                Exchange = Exchange.Bybit,
                                Status = ExchangeTradingPairStatus.Available,
                            },
                        ],
                    },
                    new()
                    {
                        CoinQuote = new TradingPairCoinQuote
                        {
                            Symbol = "ETH",
                            Name = "Ethereum",
                            Category = null,
                            IdCoinGecko = "ethereum",
                        },
                        ExchangeInfos =
                        [
                            new TradingPairExchangeInfo
                            {
                                Exchange = Exchange.Bybit,
                                Status = ExchangeTradingPairStatus.Available,
                            },
                        ],
                    },
                ],
            },
            new()
            {
                Symbol = "SOL",
                Name = null,
                IdCoinGecko = null,
                Category = null,
                TradingPairs =
                [
                    new()
                    {
                        CoinQuote = new TradingPairCoinQuote
                        {
                            Symbol = "EUR",
                            Name = "Euro",
                            Category = CoinCategory.Fiat,
                            IdCoinGecko = null,
                        },
                        ExchangeInfos =
                        [
                            new TradingPairExchangeInfo
                            {
                                Exchange = Exchange.Binance,
                                Status = ExchangeTradingPairStatus.Available,
                            },
                        ],
                    },
                ],
            },
            new()
            {
                Symbol = "ETH",
                Name = "Ethereum",
                IdCoinGecko = "ethereum",
                Category = null,
                TradingPairs =
                [
                    new()
                    {
                        CoinQuote = new TradingPairCoinQuote
                        {
                            Symbol = "USDT",
                            Name = "Tether",
                            Category = null,
                            IdCoinGecko = "usdt",
                        },
                        ExchangeInfos =
                        [
                            new TradingPairExchangeInfo
                            {
                                Exchange = Exchange.Bybit,
                                Status = ExchangeTradingPairStatus.Available,
                            },
                        ],
                    },
                ],
            },
        ];

        public static KlineDataRequest KlineDataRequest()
        {
            return new KlineDataRequest
            {
                CoinMain = new KlineDataRequestCoinBase
                {
                    Id = 1,
                    Symbol = "BTC",
                    Name = "Bitcoin",
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
