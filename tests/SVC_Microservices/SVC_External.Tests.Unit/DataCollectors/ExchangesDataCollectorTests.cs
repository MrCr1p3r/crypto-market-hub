using System.Collections.Frozen;
using AutoFixture;
using FluentAssertions;
using FluentResults;
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
        _firstClientMock
            .Setup(c => c.GetAllSpotCoins())
            .ReturnsAsync(TestData.exchangeCoins1Result);
        _firstClientMock.Setup(c => c.CurrentExchange).Returns(Exchange.Binance);

        _secondClientMock = new Mock<IExchangesClient>();
        _secondClientMock
            .Setup(c => c.GetAllSpotCoins())
            .ReturnsAsync(TestData.exchangeCoins2Result);
        _secondClientMock.Setup(c => c.CurrentExchange).Returns(Exchange.Bybit);

        _coinGeckoClientMock = new Mock<ICoinGeckoClient>();
        _coinGeckoClientMock
            .Setup(c => c.GetCoinsList())
            .ReturnsAsync(TestData.coinGeckoCoinsResult);
        _coinGeckoClientMock
            .Setup(c => c.GetSymbolToIdMapForExchange(It.IsAny<string>()))
            .ReturnsAsync(TestData.symbolToIdMap);

        _hybridCacheMock = new Mock<HybridCache>();
        _hybridCacheMock.SetupGetOrCreateAsyncToExecuteFactory<
            Func<CancellationToken, ValueTask<Result<IEnumerable<Coin>>>>,
            Result<IEnumerable<Coin>>
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
        await _dataCollector.GetAllCurrentActiveSpotCoins();

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
        var result = await _dataCollector.GetAllCurrentActiveSpotCoins();

        // Assert
        result.Should().BeEquivalentTo(expectedResult);
    }

    [Fact]
    public async Task GetAllCurrentActiveSpotCoins_WhenInternalOperationFails_ReturnsFail()
    {
        // Arrange
        var expectedErrorMessage = "Internal operation failed";
        _hybridCacheMock.SetupGetOrCreateAsyncToThrow<
            Func<CancellationToken, ValueTask<Result<IEnumerable<Coin>>>>,
            Result<IEnumerable<Coin>>
        >("all_current_active_spot_coins", new InvalidOperationException(expectedErrorMessage));

        // Act
        var result = await _dataCollector.GetAllCurrentActiveSpotCoins();

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Message.Should().Be(expectedErrorMessage);
    }

    [Fact]
    public async Task GetAllCurrentActiveSpotCoins_CallsSpotCoinsFromClients()
    {
        // Act
        await _dataCollector.GetAllCurrentActiveSpotCoins();

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
        var result = await _dataCollector.GetAllCurrentActiveSpotCoins();

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Message.Should().Contain(expectedErrorMessage);
    }

    [Fact]
    public async Task GetAllCurrentActiveSpotCoins_CallsCoinsFromCoinGecko()
    {
        // Arrange
        var expectedCoins = _fixture.CreateMany<CoinCoinGecko>();
        var expectedResult = Result.Ok(expectedCoins);
        _coinGeckoClientMock.Setup(client => client.GetCoinsList()).ReturnsAsync(expectedResult);

        // Act
        await _dataCollector.GetAllCurrentActiveSpotCoins();

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
        var result = await _dataCollector.GetAllCurrentActiveSpotCoins();

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Message.Should().Contain(expectedErrorMessage);
    }

    [Fact]
    public async Task GetAllCurrentActiveSpotCoins_CallsSymbolToIdMap()
    {
        // Act
        await _dataCollector.GetAllCurrentActiveSpotCoins();

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
        var result = await _dataCollector.GetAllCurrentActiveSpotCoins();

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Message.Should().Contain(expectedErrorMessage);
    }

    [Fact]
    public async Task GetAllCurrentActiveSpotCoins_ReturnsExpectedCoins()
    {
        // Act
        var result = await _dataCollector.GetAllCurrentActiveSpotCoins();

        // Assert
        result.Should().BeEquivalentTo(TestData.expectedCoinsResult);
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
        await _dataCollector.GetAllCurrentActiveSpotCoins();

        // Assert
        _logger.VerifyWasCalled(LogLevel.Warning, "inactive");
        _logger.VerifyWasCalled(LogLevel.Warning, "binance");
        _logger.VerifyWasCalled(LogLevel.Warning, "SOL");
    }

    #region GetKlineDataForTradingPairTests
    [Fact]
    public async Task GetKlineDataForTradingPair_TriesAllClients_UntilDataIsFound()
    {
        // Arrange
        var request = TestData.KlineDataRequest();
        var exchangeKlineData = _fixture.CreateMany<ExchangeKlineData>();
        var expectedResult = Result.Ok(exchangeKlineData);

        _firstClientMock
            .Setup(c => c.GetKlineData(It.IsAny<ExchangeKlineDataRequest>()))
            .ReturnsAsync(Result.Ok<IEnumerable<ExchangeKlineData>>([]));
        _secondClientMock
            .Setup(c => c.GetKlineData(It.IsAny<ExchangeKlineDataRequest>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _dataCollector.GetKlineDataForTradingPair(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IdTradingPair.Should().Be(request.TradingPair.Id);
        result.Value.KlineData.Should().HaveCount(exchangeKlineData.Count());
        result.Value.KlineData.Should().BeEquivalentTo(exchangeKlineData);
        _firstClientMock.Verify(
            c => c.GetKlineData(It.IsAny<ExchangeKlineDataRequest>()),
            Times.Once
        );
        _secondClientMock.Verify(
            c => c.GetKlineData(It.IsAny<ExchangeKlineDataRequest>()),
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
            .Setup(c => c.GetKlineData(It.IsAny<ExchangeKlineDataRequest>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _dataCollector.GetKlineDataForTradingPair(request);

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
            .Setup(c => c.GetKlineData(It.IsAny<ExchangeKlineDataRequest>()))
            .ReturnsAsync(Result.Ok<IEnumerable<ExchangeKlineData>>([]));
        _secondClientMock
            .Setup(c => c.GetKlineData(It.IsAny<ExchangeKlineDataRequest>()))
            .ReturnsAsync(Result.Ok<IEnumerable<ExchangeKlineData>>([]));

        // Act
        var result = await _dataCollector.GetKlineDataForTradingPair(request);

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
            .Setup(c => c.GetKlineData(It.IsAny<ExchangeKlineDataRequest>()))
            .ReturnsAsync(Result.Ok<IEnumerable<ExchangeKlineData>>([]));
        _secondClientMock
            .Setup(c => c.GetKlineData(It.IsAny<ExchangeKlineDataRequest>()))
            .ReturnsAsync(Result.Ok<IEnumerable<ExchangeKlineData>>([]));

        // Act
        await _dataCollector.GetKlineDataForTradingPair(request);

        // Assert
        _logger.VerifyWasCalled(LogLevel.Warning, request.CoinMain.Symbol);
    }
    #endregion

    #region GetFirstSuccessfulKlineDataPerCoinTests
    [Fact]
    public async Task GetFirstSuccessfulKlineDataPerCoin_ReturnsExpectedResults_WhenDataIsFound()
    {
        // Arrange
        var request = TestData.CreateKlineDataBatchRequest();
        var exchangeKlineData = _fixture.CreateMany<ExchangeKlineData>();
        var expectedResult = Result.Ok(exchangeKlineData);

        _firstClientMock
            .Setup(c => c.GetKlineData(It.IsAny<ExchangeKlineDataRequest>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _dataCollector.GetFirstSuccessfulKlineDataPerCoin(request);

        // Assert
        result.Should().HaveCount(request.MainCoins.Count());
        result.All(r => r.KlineData.Any()).Should().BeTrue();
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
            .Setup(c =>
                c.GetKlineData(
                    It.Is<ExchangeKlineDataRequest>(r =>
                        r.CoinMainSymbol == request.MainCoins.First().Symbol
                    )
                )
            )
            .ReturnsAsync(expectedResult);

        _firstClientMock
            .Setup(c =>
                c.GetKlineData(
                    It.Is<ExchangeKlineDataRequest>(r =>
                        r.CoinMainSymbol == request.MainCoins.Last().Symbol
                    )
                )
            )
            .ReturnsAsync(Result.Ok<IEnumerable<ExchangeKlineData>>([]));

        // Setup second client to fail for both coins
        _secondClientMock
            .Setup(c => c.GetKlineData(It.IsAny<ExchangeKlineDataRequest>()))
            .ReturnsAsync(Result.Ok<IEnumerable<ExchangeKlineData>>([]));

        // Act
        var result = await _dataCollector.GetFirstSuccessfulKlineDataPerCoin(request);

        // Assert
        result.Should().HaveCount(1);
        result.First().KlineData.Should().HaveCount(exchangeKlineData.Count());
        result.First().KlineData.Should().BeEquivalentTo(exchangeKlineData);
    }

    [Fact]
    public async Task GetFirstSuccessfulKlineDataPerCoin_LogsWarning_WhenNoDataFoundForCoin()
    {
        // Arrange
        var request = TestData.CreateKlineDataBatchRequest(1);

        _firstClientMock
            .Setup(c => c.GetKlineData(It.IsAny<ExchangeKlineDataRequest>()))
            .ReturnsAsync(Result.Ok<IEnumerable<ExchangeKlineData>>([]));
        _secondClientMock
            .Setup(c => c.GetKlineData(It.IsAny<ExchangeKlineDataRequest>()))
            .ReturnsAsync(Result.Ok<IEnumerable<ExchangeKlineData>>([]));

        // Act
        await _dataCollector.GetFirstSuccessfulKlineDataPerCoin(request);

        // Assert
        _logger.VerifyWasCalled(LogLevel.Warning, request.MainCoins.First().Symbol);
    }

    [Fact]
    public async Task GetFirstSuccessfulKlineDataPerCoin_ReturnsEmptyCollection_WhenNoCoinsHaveData()
    {
        // Arrange
        var request = TestData.CreateKlineDataBatchRequest(2);

        _firstClientMock
            .Setup(c => c.GetKlineData(It.IsAny<ExchangeKlineDataRequest>()))
            .ReturnsAsync(Result.Ok<IEnumerable<ExchangeKlineData>>([]));
        _secondClientMock
            .Setup(c => c.GetKlineData(It.IsAny<ExchangeKlineDataRequest>()))
            .ReturnsAsync(Result.Ok<IEnumerable<ExchangeKlineData>>([]));

        // Act
        var result = await _dataCollector.GetFirstSuccessfulKlineDataPerCoin(request);

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
            .Setup(c =>
                c.GetKlineData(
                    It.Is<ExchangeKlineDataRequest>(r =>
                        r.CoinQuoteSymbol
                        == request.MainCoins.First().TradingPairs.First().CoinQuote.Symbol
                    )
                )
            )
            .ReturnsAsync(Result.Ok<IEnumerable<ExchangeKlineData>>([]));

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
        var result = await _dataCollector.GetFirstSuccessfulKlineDataPerCoin(request);

        // Assert
        result.Should().HaveCount(1);
        result.First().IdTradingPair.Should().Be(request.MainCoins.First().TradingPairs.Last().Id);
    }

    [Fact]
    public async Task GetFirstSuccessfulKlineDataPerCoin_TriesDifferentExchanges_ForEachTradingPair()
    {
        // Arrange
        var request = TestData.KlineDataBatchRequestWithMultipleExchanges();
        var exchangeKlineData = _fixture.CreateMany<ExchangeKlineData>().ToList();

        _firstClientMock
            .Setup(c => c.GetKlineData(It.IsAny<ExchangeKlineDataRequest>()))
            .ReturnsAsync(Result.Ok<IEnumerable<ExchangeKlineData>>([]));
        _secondClientMock
            .Setup(c => c.GetKlineData(It.IsAny<ExchangeKlineDataRequest>()))
            .ReturnsAsync(exchangeKlineData);

        // Act
        await _dataCollector.GetFirstSuccessfulKlineDataPerCoin(request);

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

    #region GetCoinGeckoAssetsInfo tests
    [Fact]
    public async Task GetCoinGeckoAssetsInfo_ReturnsExpectedData_WhenBothApisReturnData()
    {
        // Arrange
        var coinIds = new[] { "bitcoin", "ethereum", "tether" };
        var assetInfos = _fixture.CreateMany<AssetCoinGecko>(3).ToList();
        assetInfos[0].Id = "bitcoin";
        assetInfos[1].Id = "ethereum";
        assetInfos[2].Id = "tether";

        var stablecoinIds = new[] { "tether", "usdc", "dai" };

        _coinGeckoClientMock
            .Setup(client =>
                client.GetMarketDataForCoins(
                    It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(coinIds))
                )
            )
            .ReturnsAsync(assetInfos);

        _coinGeckoClientMock
            .Setup(client => client.GetStablecoinsIds())
            .ReturnsAsync(stablecoinIds);

        // Act
        var result = await _dataCollector.GetCoinGeckoAssetsInfo(coinIds);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);

        // Verify each returned item has correct properties
        result.Value.Should().ContainSingle(c => c.Id == "bitcoin");
        result.Value.Should().ContainSingle(c => c.Id == "ethereum");
        result.Value.Should().ContainSingle(c => c.Id == "tether");

        // Verify stablecoin flag is correctly set
        result.Value.Should().Contain(c => c.Id == "bitcoin" && !c.IsStablecoin);
        result.Value.Should().Contain(c => c.Id == "ethereum" && !c.IsStablecoin);
        result.Value.Should().Contain(c => c.Id == "tether" && c.IsStablecoin);

        // Verify other properties are mapped correctly
        foreach (var item in result.Value)
        {
            var sourceAsset = assetInfos.First(a => a.Id == item.Id);
            item.MarketCapUsd.Should().Be(sourceAsset.MarketCapUsd);
            item.PriceUsd.Should().Be(sourceAsset.PriceUsd);
            item.PriceChangePercentage24h.Should().Be(sourceAsset.PriceChangePercentage24h);
        }
    }

    [Fact]
    public async Task GetCoinGeckoAssetsInfo_ReturnsFailedResult_WhenGetCoinsMarketsReturnsNoData()
    {
        // Arrange
        var coinIds = new[] { "bitcoin", "ethereum" };
        var stablecoinIds = new[] { "tether", "usdc" };
        var expectedErrorMessage = "Failed to get market data for coins";

        _coinGeckoClientMock
            .Setup(client =>
                client.GetMarketDataForCoins(
                    It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(coinIds))
                )
            )
            .ReturnsAsync(Result.Fail(new Error(expectedErrorMessage)));

        _coinGeckoClientMock
            .Setup(client => client.GetStablecoinsIds())
            .ReturnsAsync(stablecoinIds);

        // Act
        var result = await _dataCollector.GetCoinGeckoAssetsInfo(coinIds);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Message.Should().Be(expectedErrorMessage);
    }

    [Fact]
    public async Task GetCoinGeckoAssetsInfo_ReturnsFailedResult_WhenGetStablecoinsIdsReturnsNoData()
    {
        // Arrange
        var coinIds = new[] { "bitcoin", "ethereum" };
        var assetInfos = _fixture.CreateMany<AssetCoinGecko>(2).ToList();
        assetInfos[0].Id = "bitcoin";
        assetInfos[1].Id = "ethereum";
        var expectedResult = Result.Ok<IEnumerable<AssetCoinGecko>>(assetInfos);
        var expectedErrorMessage = "Failed to get stablecoins ids";

        _coinGeckoClientMock
            .Setup(client =>
                client.GetMarketDataForCoins(
                    It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(coinIds))
                )
            )
            .ReturnsAsync(expectedResult);

        _coinGeckoClientMock
            .Setup(client => client.GetStablecoinsIds())
            .ReturnsAsync(Result.Fail(new Error(expectedErrorMessage)));

        // Act
        var result = await _dataCollector.GetCoinGeckoAssetsInfo(coinIds);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Message.Should().Be(expectedErrorMessage);
    }
    #endregion

    private static class TestData
    {
        public const string IdBinance = "binance";
        public const string IdBybit = "bybit_spot";

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
        public static Result<IEnumerable<ExchangeCoin>> exchangeCoins1Result = Result.Ok(
            exchangeCoins1
        );

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
        public static Result<IEnumerable<ExchangeCoin>> exchangeCoins2Result = Result.Ok(
            exchangeCoins2
        );

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
        public static Result<IEnumerable<CoinCoinGecko>> coinGeckoCoinsResult = Result.Ok(
            coinGeckoCoins
        );

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
        public static Result<IEnumerable<Coin>> expectedCoinsResult = Result.Ok(expectedCoins);

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
