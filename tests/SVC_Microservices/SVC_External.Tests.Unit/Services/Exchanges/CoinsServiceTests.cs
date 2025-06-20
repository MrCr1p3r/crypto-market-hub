using System.Collections.Frozen;
using FluentResults;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using SharedLibrary.Enums;
using SharedLibrary.Extensions.Testing;
using SVC_External.ApiContracts.Responses.Exchanges.Coins;
using SVC_External.ExternalClients.Exchanges;
using SVC_External.ExternalClients.Exchanges.Contracts.Responses;
using SVC_External.ExternalClients.MarketDataProviders.CoinGecko;
using SVC_External.ExternalClients.MarketDataProviders.CoinGecko.Contracts.Responses;
using SVC_External.Services.Exchanges;

namespace SVC_External.Tests.Unit.Services.Exchanges;

public class CoinsServiceTests
{
    private readonly IFixture _fixture;
    private readonly Mock<IExchangesClient> _firstClientMock;
    private readonly Mock<IExchangesClient> _secondClientMock;
    private readonly Mock<ICoinGeckoClient> _coinGeckoClientMock;
    private readonly Mock<HybridCache> _hybridCacheMock;
    private readonly FakeLogger<CoinsService> _logger;
    private readonly CoinsService _coinsService;

    public CoinsServiceTests()
    {
        _fixture = new Fixture();

        _firstClientMock = new Mock<IExchangesClient>();
        _firstClientMock
            .Setup(client => client.GetAllSpotCoins())
            .ReturnsAsync(TestData.ExchangeCoins1Result);
        _firstClientMock.Setup(client => client.CurrentExchange).Returns(Exchange.Binance);

        _secondClientMock = new Mock<IExchangesClient>();
        _secondClientMock
            .Setup(client => client.GetAllSpotCoins())
            .ReturnsAsync(TestData.ExchangeCoins2Result);
        _secondClientMock.Setup(client => client.CurrentExchange).Returns(Exchange.Bybit);

        _coinGeckoClientMock = new Mock<ICoinGeckoClient>();
        _coinGeckoClientMock
            .Setup(client => client.GetCoinsList())
            .ReturnsAsync(TestData.CoinGeckoCoinsResult);
        _coinGeckoClientMock
            .Setup(client => client.GetSymbolToIdMapForExchange(It.IsAny<string>()))
            .ReturnsAsync(TestData.SymbolToIdMap);

        _hybridCacheMock = new Mock<HybridCache>();
        _hybridCacheMock.SetupGetOrCreateAsyncToExecuteFactory<
            Func<CancellationToken, ValueTask<Result<IEnumerable<Coin>>>>,
            Result<IEnumerable<Coin>>
        >("all_current_active_spot_coins");

        _logger = new FakeLogger<CoinsService>();

        _coinsService = new CoinsService(
            [_firstClientMock.Object, _secondClientMock.Object],
            _coinGeckoClientMock.Object,
            _hybridCacheMock.Object,
            _logger
        );
    }

    [Fact]
    public async Task GetAllCurrentActiveSpotCoins_Caching_GetOrCreateAsyncCalled()
    {
        // Arrange
        var expectedCoins = _fixture.CreateMany<Coin>();
        var expectedResult = Result.Ok(expectedCoins);
        _hybridCacheMock.SetupGetOrCreateAsync<
            Func<CancellationToken, ValueTask<Result<IEnumerable<Coin>>>>,
            Result<IEnumerable<Coin>>
        >("all_current_active_spot_coins", expectedResult);

        // Act
        await _coinsService.GetAllCurrentActiveSpotCoins();

        // Assert
        _hybridCacheMock.VerifyGetOrCreateAsyncCalled<
            Func<CancellationToken, ValueTask<Result<IEnumerable<Coin>>>>,
            Result<IEnumerable<Coin>>
        >("all_current_active_spot_coins", Times.Once());
    }

    [Fact]
    public async Task GetAllCurrentActiveSpotCoins_ReturnsResultFromCache()
    {
        // Arrange
        var expectedCoins = _fixture.CreateMany<Coin>();
        var expectedResult = Result.Ok(expectedCoins);
        _hybridCacheMock.SetupGetOrCreateAsync<
            Func<CancellationToken, ValueTask<Result<IEnumerable<Coin>>>>,
            Result<IEnumerable<Coin>>
        >("all_current_active_spot_coins", expectedResult);

        // Act
        var result = await _coinsService.GetAllCurrentActiveSpotCoins();

        // Assert
        result.Should().BeEquivalentTo(expectedResult);
    }

    [Fact]
    public async Task GetAllCurrentActiveSpotCoins_WhenInternalOperationFails_ReturnsFail()
    {
        // Arrange
        var expectedErrorMessage = "Failed to get spot coins";
        _firstClientMock
            .Setup(client => client.GetAllSpotCoins())
            .ReturnsAsync(Result.Fail(new Error(expectedErrorMessage)));

        // Act
        var result = await _coinsService.GetAllCurrentActiveSpotCoins();

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Message.Should().Contain("No coins found for one or more exchanges");
    }

    [Fact]
    public async Task GetAllCurrentActiveSpotCoins_CallsSpotCoinsFromClients()
    {
        // Act
        await _coinsService.GetAllCurrentActiveSpotCoins();

        // Assert
        _firstClientMock.Verify(client => client.GetAllSpotCoins(), Times.Once);
        _secondClientMock.Verify(client => client.GetAllSpotCoins(), Times.Once);
    }

    [Fact]
    public async Task GetAllCurrentActiveSpotCoins_CallsSpotCoinsFromClients_ReturnsFail()
    {
        // Arrange
        var expectedErrorMessage = "Failed to get spot coins";
        _secondClientMock
            .Setup(client => client.GetAllSpotCoins())
            .ReturnsAsync(Result.Fail(new Error(expectedErrorMessage)));

        // Act
        var result = await _coinsService.GetAllCurrentActiveSpotCoins();

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result
            .Errors[0]
            .Reasons.Any(reason => reason.Message.Contains(expectedErrorMessage))
            .Should()
            .BeTrue();
    }

    [Fact]
    public async Task GetAllCurrentActiveSpotCoins_CallsCoinsFromCoinGecko()
    {
        // Arrange
        var expectedCoins = _fixture.CreateMany<CoinCoinGecko>();
        var expectedResult = Result.Ok(expectedCoins);
        _coinGeckoClientMock.Setup(client => client.GetCoinsList()).ReturnsAsync(expectedResult);

        // Act
        await _coinsService.GetAllCurrentActiveSpotCoins();

        // Assert
        _coinGeckoClientMock.Verify(client => client.GetCoinsList(), Times.Once);
    }

    [Fact]
    public async Task GetAllCurrentActiveSpotCoins_CallsCoinsFromCoinGecko_ReturnsFail()
    {
        // Arrange
        var expectedErrorMessage = "Failed to get coins from CoinGecko";
        _coinGeckoClientMock
            .Setup(client => client.GetCoinsList())
            .ReturnsAsync(Result.Fail(new Error(expectedErrorMessage)));

        // Act
        var result = await _coinsService.GetAllCurrentActiveSpotCoins();

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Message.Should().Contain("Failed to retrieve a coins list from CoinGecko");
    }

    [Fact]
    public async Task GetAllCurrentActiveSpotCoins_CallsSymbolToIdMap()
    {
        // Act
        await _coinsService.GetAllCurrentActiveSpotCoins();

        // Assert
        _coinGeckoClientMock.Verify(
            client => client.GetSymbolToIdMapForExchange(TestData.IdBinance),
            Times.Once
        );
        _coinGeckoClientMock.Verify(
            client => client.GetSymbolToIdMapForExchange(TestData.IdBybit),
            Times.Once
        );
    }

    [Fact]
    public async Task GetAllCurrentActiveSpotCoins_CallsSymbolToIdMap_ReturnsFail()
    {
        // Arrange
        var expectedErrorMessage = "Failed to get symbol to id map";
        _coinGeckoClientMock
            .Setup(client => client.GetSymbolToIdMapForExchange(TestData.IdBinance))
            .ReturnsAsync(Result.Fail(new Error(expectedErrorMessage)));

        // Act
        var result = await _coinsService.GetAllCurrentActiveSpotCoins();

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result
            .Errors[0]
            .Reasons.Any(reason =>
                reason.Reasons.Any(r => r.Message.Contains(expectedErrorMessage))
            )
            .Should()
            .BeTrue();
    }

    [Fact]
    public async Task GetAllCurrentActiveSpotCoins_ReturnsExpectedCoins()
    {
        // Act
        var result = await _coinsService.GetAllCurrentActiveSpotCoins();

        // Assert
        result.Should().BeEquivalentTo(TestData.ExpectedCoinsResult);
    }

    [Fact]
    public async Task GetAllCurrentActiveSpotCoins_LogsSymbolsWithoutNames()
    {
        // Arrange
        _coinGeckoClientMock
            .Setup(client => client.GetSymbolToIdMapForExchange(It.IsAny<string>()))
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
        await _coinsService.GetAllCurrentActiveSpotCoins();

        // Assert
        _logger.VerifyWasCalled(LogLevel.Warning, "symbols");
        _logger.VerifyWasCalled(LogLevel.Warning, "SOL");
    }

    [Fact]
    public async Task GetAllCurrentActiveSpotCoins_LogsQuoteSymbolsWithoutNames()
    {
        // Arrange
        _coinGeckoClientMock
            .Setup(client => client.GetCoinsList())
            .ReturnsAsync(
                Result.Ok<IEnumerable<CoinCoinGecko>>(
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
                )
            );

        _coinGeckoClientMock
            .Setup(client => client.GetSymbolToIdMapForExchange(It.IsAny<string>()))
            .ReturnsAsync(
                new Dictionary<string, string?>
                {
                    { "BTC", "bitcoin" },
                    { "ETH", "ethereum" },
                    { "SOL", "solana" },
                }.ToFrozenDictionary()
            );

        // Act
        await _coinsService.GetAllCurrentActiveSpotCoins();

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
            .Setup(client => client.GetSymbolToIdMapForExchange(It.IsAny<string>()))
            .ReturnsAsync(symbolToIdMap);

        _coinGeckoClientMock
            .Setup(client => client.GetCoinsList())
            .ReturnsAsync(
                Result.Ok<IEnumerable<CoinCoinGecko>>(
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
                )
            );

        // Act
        await _coinsService.GetAllCurrentActiveSpotCoins();

        // Assert
        _logger.VerifyWasCalled(LogLevel.Warning, "inactive");
        _logger.VerifyWasCalled(LogLevel.Warning, "binance");
        _logger.VerifyWasCalled(LogLevel.Warning, "SOL");
    }

    [Fact]
    public async Task GetAllCurrentActiveSpotCoins_GroupsCoinsBySameCoinGeckoId()
    {
        // Arrange
        _firstClientMock
            .Setup(client => client.GetAllSpotCoins())
            .ReturnsAsync(TestData.ExchangeCoinsWithSameCoinGeckoIdResult);

        _secondClientMock
            .Setup(client => client.GetAllSpotCoins())
            .ReturnsAsync(TestData.ExchangeCoins2Result);

        _coinGeckoClientMock
            .Setup(client => client.GetSymbolToIdMapForExchange(TestData.IdBinance))
            .ReturnsAsync(TestData.SymbolToIdMapForSameCoinGeckoIdScenario);

        _coinGeckoClientMock
            .Setup(client => client.GetSymbolToIdMapForExchange(TestData.IdBybit))
            .ReturnsAsync(TestData.SymbolToIdMapForSameCoinGeckoIdScenario);

        _coinGeckoClientMock
            .Setup(client => client.GetCoinsList())
            .ReturnsAsync(TestData.CoinGeckoCoinsForGroupingTestsResult);

        // Act
        var result = await _coinsService.GetAllCurrentActiveSpotCoins();

        // Assert
        result.IsSuccess.Should().BeTrue();
        var coins = result.Value.ToList();

        // Should have only one coin (BTC/WBTC grouped together) plus individual coins
        var bitcoinCoins = coins.Where(c => c.IdCoinGecko == "bitcoin").ToList();
        bitcoinCoins.Should().HaveCount(1, "coins with same CoinGecko ID should be grouped");

        var bitcoinCoin = bitcoinCoins[0];
        bitcoinCoin.Symbol.Should().Be("BTC", "first coin should be used as representative");
        bitcoinCoin.Name.Should().Be("Bitcoin");
        bitcoinCoin
            .TradingPairs.Should()
            .HaveCount(2, "trading pairs from both coins should be merged");
    }

    [Fact]
    public async Task GetAllCurrentActiveSpotCoins_GroupsCoinsFromCoinGeckoGroupingWithSymbolNameGrouping()
    {
        // Arrange
        _firstClientMock
            .Setup(client => client.GetAllSpotCoins())
            .ReturnsAsync(TestData.ExchangeCoinsForCombinedGroupingScenarioResult);

        _secondClientMock
            .Setup(client => client.GetAllSpotCoins())
            .ReturnsAsync(TestData.ExchangeCoins2Result);

        _coinGeckoClientMock
            .Setup(client => client.GetSymbolToIdMapForExchange(TestData.IdBinance))
            .ReturnsAsync(TestData.SymbolToIdMapForCombinedGroupingScenario);

        _coinGeckoClientMock
            .Setup(client => client.GetSymbolToIdMapForExchange(TestData.IdBybit))
            .ReturnsAsync(TestData.SymbolToIdMapForCombinedGroupingScenario);

        _coinGeckoClientMock
            .Setup(client => client.GetCoinsList())
            .ReturnsAsync(TestData.CoinGeckoCoinsForGroupingTestsResult);

        // Act
        var result = await _coinsService.GetAllCurrentActiveSpotCoins();

        // Assert
        result.IsSuccess.Should().BeTrue();
        var coins = result.Value.ToList();

        // Should have only one BTC coin (all three grouped together)
        var bitcoinCoins = coins.Where(c => c.Symbol == "BTC" && c.Name == "Bitcoin").ToList();
        bitcoinCoins
            .Should()
            .HaveCount(1, "coins should be grouped by CoinGecko ID first, then by symbol-name");

        var bitcoinCoin = bitcoinCoins[0];
        bitcoinCoin.IdCoinGecko.Should().Be("bitcoin");
        bitcoinCoin
            .TradingPairs.Should()
            .HaveCount(
                3,
                "all trading pairs should be merged: USDT from BTC, ETH from WBTC, EUR from third BTC"
            );

        // Verify trading pairs from all sources are present
        var quoteCoinSymbols = bitcoinCoin.TradingPairs.Select(tp => tp.CoinQuote.Symbol).ToList();
        quoteCoinSymbols.Should().Contain(["USDT", "ETH", "EUR"]);
    }

    private static class TestData
    {
        public const string IdBinance = "binance";
        public const string IdBybit = "bybit_spot";

        private static readonly IEnumerable<ExchangeCoin> ExchangeCoins1 =
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

        public static readonly Result<IEnumerable<ExchangeCoin>> ExchangeCoins1Result = Result.Ok(
            ExchangeCoins1
        );

        private static readonly IEnumerable<ExchangeCoin> ExchangeCoins2 =
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

        public static readonly Result<IEnumerable<ExchangeCoin>> ExchangeCoins2Result = Result.Ok(
            ExchangeCoins2
        );

        private static readonly IEnumerable<CoinCoinGecko> CoinGeckoCoins =
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

        public static readonly Result<IEnumerable<CoinCoinGecko>> CoinGeckoCoinsResult = Result.Ok(
            CoinGeckoCoins
        );

        public static readonly FrozenDictionary<string, string?> SymbolToIdMap = new Dictionary<
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

        private static readonly IEnumerable<Coin> ExpectedCoins =
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

        public static readonly Result<IEnumerable<Coin>> ExpectedCoinsResult = Result.Ok(
            ExpectedCoins
        );

        // Test data for grouping scenarios
        private static readonly IEnumerable<ExchangeCoin> ExchangeCoinsWithSameCoinGeckoId =
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
                ],
            },
            new()
            {
                Symbol = "WBTC", // Different symbol but same CoinGecko ID
                Name = null,
                TradingPairs =
                [
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
                            Status = ExchangeTradingPairStatus.Available,
                        },
                    },
                ],
            },
        ];

        public static readonly Result<
            IEnumerable<ExchangeCoin>
        > ExchangeCoinsWithSameCoinGeckoIdResult = Result.Ok(ExchangeCoinsWithSameCoinGeckoId);

        private static readonly IEnumerable<ExchangeCoin> ExchangeCoinsForCombinedGroupingScenario =
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
                ],
            },
            new()
            {
                Symbol = "WBTC", // Same CoinGecko ID as BTC
                Name = null,
                TradingPairs =
                [
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
                            Status = ExchangeTradingPairStatus.Available,
                        },
                    },
                ],
            },
            new()
            {
                Symbol = "BTC", // Same symbol-name as representative from CoinGecko grouping but no CoinGecko ID
                Name = "Bitcoin", // This will match the name from CoinGecko after processing
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
                            Exchange = Exchange.Bybit,
                            Status = ExchangeTradingPairStatus.Available,
                        },
                    },
                ],
            },
        ];

        public static readonly Result<
            IEnumerable<ExchangeCoin>
        > ExchangeCoinsForCombinedGroupingScenarioResult = Result.Ok(
            ExchangeCoinsForCombinedGroupingScenario
        );

        public static readonly FrozenDictionary<
            string,
            string?
        > SymbolToIdMapForSameCoinGeckoIdScenario = new Dictionary<string, string?>
        {
            { "BTC", "bitcoin" },
            { "WBTC", "bitcoin" }, // Same CoinGecko ID
            { "USDT", "usdt" },
            { "ETH", "ethereum" },
        }.ToFrozenDictionary();

        public static readonly FrozenDictionary<
            string,
            string?
        > SymbolToIdMapForCombinedGroupingScenario = new Dictionary<string, string?>
        {
            { "BTC", "bitcoin" },
            { "WBTC", "bitcoin" }, // Same CoinGecko ID as BTC
            { "USDT", "usdt" },
            { "ETH", "ethereum" },
            { "EUR", null }, // No CoinGecko ID for EUR
        }.ToFrozenDictionary();

        private static readonly IEnumerable<CoinCoinGecko> CoinGeckoCoinsForGroupingTests =
        [
            new()
            {
                Id = "bitcoin",
                Symbol = "BTC",
                Name = "Bitcoin",
            },
            new()
            {
                Id = "usdt",
                Symbol = "USDT",
                Name = "Tether",
            },
            new()
            {
                Id = "ethereum",
                Symbol = "ETH",
                Name = "Ethereum",
            },
        ];

        public static readonly Result<
            IEnumerable<CoinCoinGecko>
        > CoinGeckoCoinsForGroupingTestsResult = Result.Ok(CoinGeckoCoinsForGroupingTests);
    }
}
