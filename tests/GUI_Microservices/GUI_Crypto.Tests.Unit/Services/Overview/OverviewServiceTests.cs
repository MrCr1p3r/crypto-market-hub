using FluentAssertions.ArgumentMatchers.Moq;
using FluentResults;
using GUI_Crypto.ApiContracts.Requests.CoinCreation;
using GUI_Crypto.MicroserviceClients.SvcCoins;
using GUI_Crypto.MicroserviceClients.SvcExternal;
using GUI_Crypto.MicroserviceClients.SvcKline;
using GUI_Crypto.Services.Overview;
using SharedLibrary.Enums;
using SharedLibrary.Errors;
using SharedLibrary.Models;
using SvcCoins = GUI_Crypto.MicroserviceClients.SvcCoins.Contracts;
using SvcExternal = GUI_Crypto.MicroserviceClients.SvcExternal.Contracts;
using SvcKline = GUI_Crypto.MicroserviceClients.SvcKline.Contracts;

namespace GUI_Crypto.Tests.Unit.Services.Overview;

public class OverviewServiceTests
{
    private readonly Mock<ISvcCoinsClient> _mockCoinsClient;
    private readonly Mock<ISvcKlineClient> _mockKlineClient;
    private readonly Mock<ISvcExternalClient> _mockExternalClient;
    private readonly OverviewService _overviewService;

    public OverviewServiceTests()
    {
        _mockCoinsClient = new Mock<ISvcCoinsClient>();
        _mockKlineClient = new Mock<ISvcKlineClient>();
        _mockExternalClient = new Mock<ISvcExternalClient>();
        _overviewService = new OverviewService(
            _mockCoinsClient.Object,
            _mockKlineClient.Object,
            _mockExternalClient.Object
        );
    }

    #region GetOverviewCoins Tests

    [Fact]
    public async Task GetOverviewCoins_WhenSuccessfulFlow_ShouldReturnMappedOverviewCoins()
    {
        // Arrange
        _mockCoinsClient
            .Setup(client => client.GetAllCoins())
            .ReturnsAsync(Result.Ok(TestData.BitcoinAndEthereumCoins));

        _mockKlineClient
            .Setup(client => client.GetAllKlineData())
            .ReturnsAsync(Result.Ok(TestData.BitcoinAndEthereumKlineData));

        // Act
        var result = await _overviewService.GetOverviewCoins();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);

        var overviewCoins = result.Value.ToList();
        overviewCoins[0].Id.Should().Be(1);
        overviewCoins[0].Symbol.Should().Be("BTC");
        overviewCoins[0].Name.Should().Be("Bitcoin");
        overviewCoins[0].Category.Should().BeNull();
        overviewCoins[0].MarketCapUsd.Should().Be(1_200_000_000_000);
        overviewCoins[0].PriceUsd.Should().Be("50000.00");
        overviewCoins[0].PriceChangePercentage24h.Should().Be(3.5m);
        overviewCoins[0].TradingPairIds.Should().NotBeEmpty();
        overviewCoins[0].KlineData.Should().NotBeNull();
        overviewCoins[0].KlineData!.TradingPairId.Should().Be(1);
        overviewCoins[0].KlineData!.Klines.Should().HaveCount(2);

        overviewCoins[1].Id.Should().Be(2);
        overviewCoins[1].Symbol.Should().Be("ETH");
        overviewCoins[1].Name.Should().Be("Ethereum");
        overviewCoins[1].Category.Should().BeNull();
        overviewCoins[1].MarketCapUsd.Should().Be(400_000_000_000);
        overviewCoins[1].PriceUsd.Should().Be("3000.00");
        overviewCoins[1].PriceChangePercentage24h.Should().Be(-1.2m);
        overviewCoins[1].TradingPairIds.Should().NotBeEmpty();
        overviewCoins[1].KlineData.Should().NotBeNull();
        overviewCoins[1].KlineData!.TradingPairId.Should().Be(2);
        overviewCoins[1].KlineData!.Klines.Should().HaveCount(2);

        // Verify client calls
        _mockCoinsClient.Verify(client => client.GetAllCoins(), Times.Once);
        _mockKlineClient.Verify(client => client.GetAllKlineData(), Times.Once);
    }

    [Fact]
    public async Task GetOverviewCoins_WhenGetAllCoinsFails_ShouldReturnFailureResult()
    {
        // Arrange
        var coinsServiceError = new GenericErrors.InternalError("Coins service unavailable");

        _mockCoinsClient
            .Setup(client => client.GetAllCoins())
            .ReturnsAsync(Result.Fail(coinsServiceError));

        _mockKlineClient
            .Setup(client => client.GetAllKlineData())
            .ReturnsAsync(Result.Ok(TestData.BitcoinAndEthereumKlineData));

        // Act
        var result = await _overviewService.GetOverviewCoins();

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(coinsServiceError);

        // Verify both clients were called (they run in parallel)
        _mockCoinsClient.Verify(client => client.GetAllCoins(), Times.Once);
        _mockKlineClient.Verify(client => client.GetAllKlineData(), Times.Once);
    }

    [Fact]
    public async Task GetOverviewCoins_WhenGetAllKlineDataFails_ShouldReturnFailureResult()
    {
        // Arrange
        var klineServiceError = new GenericErrors.InternalError("Kline service unavailable");

        _mockCoinsClient
            .Setup(client => client.GetAllCoins())
            .ReturnsAsync(Result.Ok(TestData.BitcoinAndEthereumCoins));

        _mockKlineClient
            .Setup(client => client.GetAllKlineData())
            .ReturnsAsync(Result.Fail(klineServiceError));

        // Act
        var result = await _overviewService.GetOverviewCoins();

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(klineServiceError);

        // Verify both clients were called (they run in parallel)
        _mockCoinsClient.Verify(client => client.GetAllCoins(), Times.Once);
        _mockKlineClient.Verify(client => client.GetAllKlineData(), Times.Once);
    }

    [Fact]
    public async Task GetOverviewCoins_WhenNoKlineDataMatches_ShouldReturnCoinsWithEmptyKlineData()
    {
        // Arrange
        _mockCoinsClient
            .Setup(client => client.GetAllCoins())
            .ReturnsAsync(Result.Ok(TestData.BitcoinAndEthereumCoins));

        _mockKlineClient
            .Setup(client => client.GetAllKlineData())
            .ReturnsAsync(Result.Ok(TestData.UnmatchedKlineData));

        // Act
        var result = await _overviewService.GetOverviewCoins();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);

        var overviewCoins = result.Value.ToList();
        overviewCoins[0].KlineData.Should().BeNull();
        overviewCoins[1].KlineData.Should().BeNull();
    }

    [Fact]
    public async Task GetOverviewCoins_WhenMixedCoinsWithAndWithoutTradingPairs_ShouldReturnOnlyMainCoins()
    {
        // Arrange
        _mockCoinsClient
            .Setup(client => client.GetAllCoins())
            .ReturnsAsync(Result.Ok(TestData.MixedCoinsWithAndWithoutTradingPairs));

        _mockKlineClient
            .Setup(client => client.GetAllKlineData())
            .ReturnsAsync(Result.Ok(TestData.BitcoinKlineDataOnly));

        // Act
        var result = await _overviewService.GetOverviewCoins();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1); // Only Bitcoin should be returned (has trading pairs)

        var overviewCoin = result.Value.First();
        overviewCoin.Id.Should().Be(1);
        overviewCoin.Symbol.Should().Be("BTC");
        overviewCoin.Name.Should().Be("Bitcoin");
        overviewCoin.TradingPairIds.Should().NotBeEmpty();
        overviewCoin.KlineData.Should().NotBeNull();
        overviewCoin.KlineData!.TradingPairId.Should().Be(1);
        overviewCoin.KlineData!.Klines.Should().HaveCount(2);

        _mockCoinsClient.Verify(client => client.GetAllCoins(), Times.Once);
        _mockKlineClient.Verify(client => client.GetAllKlineData(), Times.Once);
    }

    #endregion

    #region GetCandidateCoins Tests

    [Fact]
    public async Task GetCandidateCoins_WhenSuccessfulFlow_ShouldReturnFilteredCandidateCoins()
    {
        // Arrange
        _mockExternalClient
            .Setup(client => client.GetAllSpotCoins())
            .ReturnsAsync(Result.Ok(TestData.AllSpotCoins));

        _mockCoinsClient
            .Setup(client => client.GetAllCoins())
            .ReturnsAsync(Result.Ok(TestData.ExistingDbCoins));

        // Act
        var result = await _overviewService.GetCandidateCoins();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2); // ADA and DOT are candidates

        var candidateCoins = result.Value.ToList();
        candidateCoins.Should().Contain(coin => coin.Symbol == "ADA");
        candidateCoins.Should().Contain(coin => coin.Symbol == "DOT");
        candidateCoins.Should().NotContain(coin => coin.Symbol == "BTC"); // Already exists as main coin
        candidateCoins.Should().NotContain(coin => coin.Symbol == "ETH"); // Already exists as main coin

        // Verify client calls
        _mockExternalClient.Verify(client => client.GetAllSpotCoins(), Times.Once);
        _mockCoinsClient.Verify(client => client.GetAllCoins(), Times.Once);
    }

    [Fact]
    public async Task GetCandidateCoins_WhenGetAllSpotCoinsFails_ShouldReturnFailureResult()
    {
        // Arrange
        var externalServiceError = new GenericErrors.InternalError("External service unavailable");

        _mockExternalClient
            .Setup(client => client.GetAllSpotCoins())
            .ReturnsAsync(Result.Fail(externalServiceError));

        _mockCoinsClient
            .Setup(client => client.GetAllCoins())
            .ReturnsAsync(Result.Ok(TestData.ExistingDbCoins));

        // Act
        var result = await _overviewService.GetCandidateCoins();

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(externalServiceError);

        // Verify both clients were called (they run in parallel)
        _mockExternalClient.Verify(client => client.GetAllSpotCoins(), Times.Once);
        _mockCoinsClient.Verify(client => client.GetAllCoins(), Times.Once);
    }

    [Fact]
    public async Task GetCandidateCoins_WhenGetAllDbCoinsFails_ShouldReturnFailureResult()
    {
        // Arrange
        var coinsServiceError = new GenericErrors.InternalError("Coins service unavailable");

        _mockExternalClient
            .Setup(client => client.GetAllSpotCoins())
            .ReturnsAsync(Result.Ok(TestData.AllSpotCoins));

        _mockCoinsClient
            .Setup(client => client.GetAllCoins())
            .ReturnsAsync(Result.Fail(coinsServiceError));

        // Act
        var result = await _overviewService.GetCandidateCoins();

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(coinsServiceError);

        // Verify both clients were called (they run in parallel)
        _mockExternalClient.Verify(client => client.GetAllSpotCoins(), Times.Once);
        _mockCoinsClient.Verify(client => client.GetAllCoins(), Times.Once);
    }

    [Fact]
    public async Task GetCandidateCoins_WhenAllSpotCoinsAlreadyExist_ShouldReturnEmptyResult()
    {
        // Arrange
        _mockExternalClient
            .Setup(client => client.GetAllSpotCoins())
            .ReturnsAsync(Result.Ok(TestData.ExistingSpotCoinsOnly));

        _mockCoinsClient
            .Setup(client => client.GetAllCoins())
            .ReturnsAsync(Result.Ok(TestData.ExistingDbCoins));

        // Act
        var result = await _overviewService.GetCandidateCoins();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCandidateCoins_WhenNoDbCoinsExist_ShouldReturnAllSpotCoinsAsCandidates()
    {
        // Arrange
        _mockExternalClient
            .Setup(client => client.GetAllSpotCoins())
            .ReturnsAsync(Result.Ok(TestData.AllSpotCoins));

        _mockCoinsClient
            .Setup(client => client.GetAllCoins())
            .ReturnsAsync(Result.Ok(Enumerable.Empty<SvcCoins.Responses.Coin>()));

        // Act
        var result = await _overviewService.GetCandidateCoins();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(4); // All spot coins become candidates
    }

    [Fact]
    public async Task GetCandidateCoins_WhenSpotCoinsHaveDuplicateCoinGeckoIdsWithDbCoins_ShouldExcludeDuplicates()
    {
        // Arrange
        _mockExternalClient
            .Setup(client => client.GetAllSpotCoins())
            .ReturnsAsync(Result.Ok(TestData.SpotCoinsWithDuplicateCoinGeckoIds));

        _mockCoinsClient
            .Setup(client => client.GetAllCoins())
            .ReturnsAsync(Result.Ok(TestData.DbCoinsWithMatchingCoinGeckoIds));

        // Act
        var result = await _overviewService.GetCandidateCoins();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty(); // Both spot coins should be excluded due to duplicate CoinGecko IDs

        // Verify client calls
        _mockExternalClient.Verify(client => client.GetAllSpotCoins(), Times.Once);
        _mockCoinsClient.Verify(client => client.GetAllCoins(), Times.Once);
    }

    [Fact]
    public async Task GetCandidateCoins_WhenSpotCoinsHaveDuplicateSymbolNamePairsWithDbCoins_ShouldExcludeDuplicates()
    {
        // Arrange
        _mockExternalClient
            .Setup(client => client.GetAllSpotCoins())
            .ReturnsAsync(Result.Ok(TestData.SpotCoinsWithDuplicateSymbolNamePairs));

        _mockCoinsClient
            .Setup(client => client.GetAllCoins())
            .ReturnsAsync(Result.Ok(TestData.DbCoinsWithMatchingSymbolNamePairs));

        // Act
        var result = await _overviewService.GetCandidateCoins();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty(); // Both spot coins should be excluded due to duplicate Symbol-Name pairs

        // Verify client calls
        _mockExternalClient.Verify(client => client.GetAllSpotCoins(), Times.Once);
        _mockCoinsClient.Verify(client => client.GetAllCoins(), Times.Once);
    }

    #endregion

    #region CreateCoins Tests

    [Fact]
    public async Task CreateCoins_WhenSuccessfulFlow_ShouldReturnCreatedOverviewCoins()
    {
        // Arrange
        var coinCreationRequests = TestData.CoinCreationRequests;

        _mockCoinsClient
            .Setup(client =>
                client.CreateCoins(
                    It.IsAny<IEnumerable<SvcCoins.Requests.CoinCreation.CoinCreationRequest>>()
                )
            )
            .ReturnsAsync(Result.Ok(TestData.CreatedCoins));

        // Act
        var result = await _overviewService.CreateCoins(coinCreationRequests);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);

        var createdCoins = result.Value.ToList();
        createdCoins[0].Id.Should().Be(3);
        createdCoins[0].Symbol.Should().Be("ADA");
        createdCoins[1].Id.Should().Be(4);
        createdCoins[1].Symbol.Should().Be("DOT");

        // Verify client call
        _mockCoinsClient.Verify(
            client =>
                client.CreateCoins(Its.EquivalentTo(TestData.ExpectedSvcCoinsCreationRequests)),
            Times.Once
        );
    }

    [Fact]
    public async Task CreateCoins_WhenCreateCoinsFails_ShouldReturnFailureResult()
    {
        // Arrange
        var coinCreationRequests = TestData.CoinCreationRequests;
        var coinsServiceError = new GenericErrors.InternalError("Failed to create coins");

        _mockCoinsClient
            .Setup(client =>
                client.CreateCoins(
                    It.IsAny<IEnumerable<SvcCoins.Requests.CoinCreation.CoinCreationRequest>>()
                )
            )
            .ReturnsAsync(Result.Fail(coinsServiceError));

        // Act
        var result = await _overviewService.CreateCoins(coinCreationRequests);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(coinsServiceError);

        _mockCoinsClient.Verify(
            client =>
                client.CreateCoins(Its.EquivalentTo(TestData.ExpectedSvcCoinsCreationRequests)),
            Times.Once
        );
    }

    [Fact]
    public async Task CreateCoins_WhenEmptyRequest_ShouldReturnEmptyResult()
    {
        // Arrange
        var emptyRequests = Enumerable.Empty<CoinCreationRequest>();

        _mockCoinsClient
            .Setup(client =>
                client.CreateCoins(
                    It.IsAny<IEnumerable<SvcCoins.Requests.CoinCreation.CoinCreationRequest>>()
                )
            )
            .ReturnsAsync(Result.Ok(Enumerable.Empty<SvcCoins.Responses.Coin>()));

        // Act
        var result = await _overviewService.CreateCoins(emptyRequests);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    #endregion

    #region DeleteMainCoin Tests

    [Fact]
    public async Task DeleteMainCoin_WhenSuccessfulFlow_ShouldReturnSuccessResult()
    {
        // Arrange
        var coinId = 1;

        _mockCoinsClient.Setup(client => client.DeleteMainCoin(coinId)).ReturnsAsync(Result.Ok());

        // Act
        var result = await _overviewService.DeleteMainCoin(coinId);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _mockCoinsClient.Verify(client => client.DeleteMainCoin(coinId), Times.Once);
    }

    [Fact]
    public async Task DeleteMainCoin_WhenDeleteFails_ShouldReturnFailureResult()
    {
        // Arrange
        var coinId = 1;
        var deleteError = new GenericErrors.InternalError("Failed to delete coin");

        _mockCoinsClient
            .Setup(client => client.DeleteMainCoin(coinId))
            .ReturnsAsync(Result.Fail(deleteError));

        // Act
        var result = await _overviewService.DeleteMainCoin(coinId);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(deleteError);

        _mockCoinsClient.Verify(client => client.DeleteMainCoin(coinId), Times.Once);
    }

    #endregion

    #region DeleteAllCoins Tests

    [Fact]
    public async Task DeleteAllCoins_WhenSuccessfulFlow_ShouldReturnSuccessResult()
    {
        // Arrange
        _mockCoinsClient.Setup(client => client.DeleteAllCoins()).ReturnsAsync(Result.Ok());

        // Act
        var result = await _overviewService.DeleteAllCoins();

        // Assert
        result.IsSuccess.Should().BeTrue();

        _mockCoinsClient.Verify(client => client.DeleteAllCoins(), Times.Once);
    }

    [Fact]
    public async Task DeleteAllCoins_WhenDeleteFails_ShouldReturnFailureResult()
    {
        // Arrange
        var deleteError = new GenericErrors.InternalError("Failed to delete all coins");

        _mockCoinsClient
            .Setup(client => client.DeleteAllCoins())
            .ReturnsAsync(Result.Fail(deleteError));

        // Act
        var result = await _overviewService.DeleteAllCoins();

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(deleteError);

        _mockCoinsClient.Verify(client => client.DeleteAllCoins(), Times.Once);
    }

    #endregion

    private static class TestData
    {
        public static readonly IEnumerable<SvcCoins.Responses.Coin> BitcoinAndEthereumCoins =
        [
            new SvcCoins.Responses.Coin
            {
                Id = 1,
                Symbol = "BTC",
                Name = "Bitcoin",
                Category = null,
                MarketCapUsd = 1_200_000_000_000,
                PriceUsd = "50000.00",
                PriceChangePercentage24h = 3.5m,
                IdCoinGecko = "bitcoin",
                TradingPairs =
                [
                    new SvcCoins.Responses.TradingPair
                    {
                        Id = 1,
                        CoinQuote = new SvcCoins.Responses.TradingPairCoinQuote
                        {
                            Id = 5,
                            Symbol = "USDT",
                            Name = "Tether",
                        },
                        Exchanges = [Exchange.Binance],
                    },
                ],
            },
            new SvcCoins.Responses.Coin
            {
                Id = 2,
                Symbol = "ETH",
                Name = "Ethereum",
                Category = null,
                MarketCapUsd = 400_000_000_000,
                PriceUsd = "3000.00",
                PriceChangePercentage24h = -1.2m,
                IdCoinGecko = "ethereum",
                TradingPairs =
                [
                    new SvcCoins.Responses.TradingPair
                    {
                        Id = 2,
                        CoinQuote = new SvcCoins.Responses.TradingPairCoinQuote
                        {
                            Id = 5,
                            Symbol = "USDT",
                            Name = "Tether",
                        },
                        Exchanges = [Exchange.Binance],
                    },
                ],
            },
        ];

        public static readonly IEnumerable<SvcKline.KlineDataResponse> BitcoinAndEthereumKlineData =
        [
            new SvcKline.KlineDataResponse
            {
                IdTradingPair = 1,
                Klines =
                [
                    new Kline
                    {
                        OpenTime = DateTimeOffset.UtcNow.AddHours(-2).ToUnixTimeMilliseconds(),
                        OpenPrice = "50000",
                        HighPrice = "51000",
                        LowPrice = "49000",
                        ClosePrice = "50500",
                        Volume = "100",
                        CloseTime = DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeMilliseconds(),
                    },
                    new Kline
                    {
                        OpenTime = DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeMilliseconds(),
                        OpenPrice = "50500",
                        HighPrice = "52000",
                        LowPrice = "50000",
                        ClosePrice = "51500",
                        Volume = "150",
                        CloseTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    },
                ],
            },
            new SvcKline.KlineDataResponse
            {
                IdTradingPair = 2,
                Klines =
                [
                    new Kline
                    {
                        OpenTime = DateTimeOffset.UtcNow.AddHours(-2).ToUnixTimeMilliseconds(),
                        OpenPrice = "3000",
                        HighPrice = "3100",
                        LowPrice = "2900",
                        ClosePrice = "3050",
                        Volume = "200",
                        CloseTime = DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeMilliseconds(),
                    },
                    new Kline
                    {
                        OpenTime = DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeMilliseconds(),
                        OpenPrice = "3050",
                        HighPrice = "3200",
                        LowPrice = "3000",
                        ClosePrice = "3150",
                        Volume = "250",
                        CloseTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    },
                ],
            },
        ];

        public static readonly IEnumerable<SvcKline.KlineDataResponse> UnmatchedKlineData =
        [
            new SvcKline.KlineDataResponse
            {
                IdTradingPair = 999, // Non-existent trading pair
                Klines = [],
            },
        ];

        public static readonly IEnumerable<SvcExternal.Responses.Coins.Coin> AllSpotCoins =
        [
            new SvcExternal.Responses.Coins.Coin
            {
                Symbol = "BTC",
                Name = "Bitcoin",
                Category = null,
                IdCoinGecko = "bitcoin",
                TradingPairs =
                [
                    new SvcExternal.Responses.Coins.TradingPair
                    {
                        CoinQuote = new SvcExternal.Responses.Coins.TradingPairCoinQuote
                        {
                            Symbol = "USDT",
                            Name = "Tether",
                            Category = CoinCategory.Stablecoin,
                            IdCoinGecko = "tether",
                        },
                        ExchangeInfos =
                        [
                            new SvcExternal.Responses.Coins.TradingPairExchangeInfo
                            {
                                Exchange = Exchange.Binance,
                            },
                            new SvcExternal.Responses.Coins.TradingPairExchangeInfo
                            {
                                Exchange = Exchange.Bybit,
                            },
                        ],
                    },
                ],
            },
            new SvcExternal.Responses.Coins.Coin
            {
                Symbol = "ETH",
                Name = "Ethereum",
                Category = null,
                IdCoinGecko = "ethereum",
                TradingPairs =
                [
                    new SvcExternal.Responses.Coins.TradingPair
                    {
                        CoinQuote = new SvcExternal.Responses.Coins.TradingPairCoinQuote
                        {
                            Symbol = "USDT",
                            Name = "Tether",
                            Category = CoinCategory.Stablecoin,
                            IdCoinGecko = "tether",
                        },
                        ExchangeInfos =
                        [
                            new SvcExternal.Responses.Coins.TradingPairExchangeInfo
                            {
                                Exchange = Exchange.Binance,
                            },
                        ],
                    },
                    new SvcExternal.Responses.Coins.TradingPair
                    {
                        CoinQuote = new SvcExternal.Responses.Coins.TradingPairCoinQuote
                        {
                            Symbol = "BTC",
                            Name = "Bitcoin",
                            Category = null,
                            IdCoinGecko = "bitcoin",
                        },
                        ExchangeInfos =
                        [
                            new SvcExternal.Responses.Coins.TradingPairExchangeInfo
                            {
                                Exchange = Exchange.Bybit,
                            },
                        ],
                    },
                ],
            },
            new SvcExternal.Responses.Coins.Coin
            {
                Symbol = "ADA",
                Name = "Cardano",
                Category = null,
                IdCoinGecko = "cardano",
                TradingPairs =
                [
                    new SvcExternal.Responses.Coins.TradingPair
                    {
                        CoinQuote = new SvcExternal.Responses.Coins.TradingPairCoinQuote
                        {
                            Symbol = "USDT",
                            Name = "Tether",
                            Category = CoinCategory.Stablecoin,
                            IdCoinGecko = "tether",
                        },
                        ExchangeInfos =
                        [
                            new SvcExternal.Responses.Coins.TradingPairExchangeInfo
                            {
                                Exchange = Exchange.Binance,
                            },
                            new SvcExternal.Responses.Coins.TradingPairExchangeInfo
                            {
                                Exchange = Exchange.Bybit,
                            },
                        ],
                    },
                ],
            },
            new SvcExternal.Responses.Coins.Coin
            {
                Symbol = "DOT",
                Name = "Polkadot",
                Category = null,
                IdCoinGecko = "polkadot",
                TradingPairs =
                [
                    new SvcExternal.Responses.Coins.TradingPair
                    {
                        CoinQuote = new SvcExternal.Responses.Coins.TradingPairCoinQuote
                        {
                            Symbol = "USDT",
                            Name = "Tether",
                            Category = CoinCategory.Stablecoin,
                            IdCoinGecko = "tether",
                        },
                        ExchangeInfos =
                        [
                            new SvcExternal.Responses.Coins.TradingPairExchangeInfo
                            {
                                Exchange = Exchange.Bybit,
                            },
                        ],
                    },
                ],
            },
        ];

        public static readonly IEnumerable<SvcCoins.Responses.Coin> ExistingDbCoins =
        [
            new SvcCoins.Responses.Coin
            {
                Id = 1,
                Symbol = "BTC",
                Name = "Bitcoin",
                Category = null, // Regular cryptocurrency
                MarketCapUsd = 1_200_000_000_000,
                PriceUsd = "50000.00",
                PriceChangePercentage24h = 3.5m,
                TradingPairs =
                [
                    new SvcCoins.Responses.TradingPair
                    {
                        Id = 1,
                        CoinQuote = new SvcCoins.Responses.TradingPairCoinQuote
                        {
                            Id = 5,
                            Symbol = "USDT",
                            Name = "Tether",
                        },
                        Exchanges = [Exchange.Binance],
                    },
                ],
            },
            new SvcCoins.Responses.Coin
            {
                Id = 2,
                Symbol = "ETH",
                Name = "Ethereum",
                Category = null, // Regular cryptocurrency
                MarketCapUsd = 400_000_000_000,
                PriceUsd = "3000.00",
                PriceChangePercentage24h = -1.2m,
                TradingPairs =
                [
                    new SvcCoins.Responses.TradingPair
                    {
                        Id = 2,
                        CoinQuote = new SvcCoins.Responses.TradingPairCoinQuote
                        {
                            Id = 5,
                            Symbol = "USDT",
                            Name = "Tether",
                        },
                        Exchanges = [Exchange.Binance],
                    },
                ],
            },
            new SvcCoins.Responses.Coin
            {
                Id = 5,
                Symbol = "USDT",
                Name = "Tether",
                Category = CoinCategory.Stablecoin,
                MarketCapUsd = 90_000_000_000,
                PriceUsd = "1.00",
                PriceChangePercentage24h = 0.1m,
                TradingPairs = [],
            },
        ];

        public static readonly IEnumerable<SvcExternal.Responses.Coins.Coin> ExistingSpotCoinsOnly =
        [
            new SvcExternal.Responses.Coins.Coin
            {
                Symbol = "BTC",
                Name = "Bitcoin",
                Category = null,
                TradingPairs = [],
            },
            new SvcExternal.Responses.Coins.Coin
            {
                Symbol = "ETH",
                Name = "Ethereum",
                Category = null,
                TradingPairs = [],
            },
        ];

        public static readonly IEnumerable<CoinCreationRequest> CoinCreationRequests =
        [
            new CoinCreationRequest
            {
                Id = null,
                Symbol = "ADA",
                Name = "Cardano",
                Category = null,
                IdCoinGecko = "cardano",
                TradingPairs =
                [
                    new CoinCreationTradingPair
                    {
                        CoinQuote = new CoinCreationCoinQuote
                        {
                            Id = 5,
                            Symbol = "USDT",
                            Name = "Tether",
                            Category = CoinCategory.Stablecoin,
                            IdCoinGecko = "tether",
                        },
                        Exchanges = [Exchange.Binance, Exchange.Bybit],
                    },
                    new CoinCreationTradingPair
                    {
                        CoinQuote = new CoinCreationCoinQuote
                        {
                            Id = null,
                            Symbol = "BTC",
                            Name = "Bitcoin",
                            Category = null,
                            IdCoinGecko = "bitcoin",
                        },
                        Exchanges = [Exchange.Binance],
                    },
                ],
            },
            new CoinCreationRequest
            {
                Id = null,
                Symbol = "DOT",
                Name = "Polkadot",
                Category = null,
                IdCoinGecko = "polkadot",
                TradingPairs =
                [
                    new CoinCreationTradingPair
                    {
                        CoinQuote = new CoinCreationCoinQuote
                        {
                            Id = 5,
                            Symbol = "USDT",
                            Name = "Tether",
                            Category = CoinCategory.Stablecoin,
                            IdCoinGecko = "tether",
                        },
                        Exchanges = [Exchange.Bybit],
                    },
                ],
            },
        ];

        public static readonly IEnumerable<SvcCoins.Responses.Coin> CreatedCoins =
        [
            new SvcCoins.Responses.Coin
            {
                Id = 3,
                Symbol = "ADA",
                Name = "Cardano",
                Category = null,
                MarketCapUsd = 15_000_000_000,
                PriceUsd = "0.45",
                PriceChangePercentage24h = 5.2m,
                TradingPairs = [],
            },
            new SvcCoins.Responses.Coin
            {
                Id = 4,
                Symbol = "DOT",
                Name = "Polkadot",
                Category = null,
                MarketCapUsd = 8_000_000_000,
                PriceUsd = "7.50",
                PriceChangePercentage24h = -2.1m,
                TradingPairs = [],
            },
        ];

        public static readonly IEnumerable<SvcCoins.Requests.CoinCreation.CoinCreationRequest> ExpectedSvcCoinsCreationRequests =
        [
            new SvcCoins.Requests.CoinCreation.CoinCreationRequest
            {
                Id = null,
                Symbol = "ADA",
                Name = "Cardano",
                Category = null,
                IdCoinGecko = "cardano",
                TradingPairs =
                [
                    new SvcCoins.Requests.CoinCreation.CoinCreationTradingPair
                    {
                        CoinQuote = new SvcCoins.Requests.CoinCreation.CoinCreationCoinQuote
                        {
                            Id = 5,
                            Symbol = "USDT",
                            Name = "Tether",
                            Category = CoinCategory.Stablecoin,
                            IdCoinGecko = "tether",
                        },
                        Exchanges = [Exchange.Binance, Exchange.Bybit],
                    },
                    new SvcCoins.Requests.CoinCreation.CoinCreationTradingPair
                    {
                        CoinQuote = new SvcCoins.Requests.CoinCreation.CoinCreationCoinQuote
                        {
                            Id = null,
                            Symbol = "BTC",
                            Name = "Bitcoin",
                            Category = null,
                            IdCoinGecko = "bitcoin",
                        },
                        Exchanges = [Exchange.Binance],
                    },
                ],
            },
            new SvcCoins.Requests.CoinCreation.CoinCreationRequest
            {
                Id = null,
                Symbol = "DOT",
                Name = "Polkadot",
                Category = null,
                IdCoinGecko = "polkadot",
                TradingPairs =
                [
                    new SvcCoins.Requests.CoinCreation.CoinCreationTradingPair
                    {
                        CoinQuote = new SvcCoins.Requests.CoinCreation.CoinCreationCoinQuote
                        {
                            Id = 5,
                            Symbol = "USDT",
                            Name = "Tether",
                            Category = CoinCategory.Stablecoin,
                            IdCoinGecko = "tether",
                        },
                        Exchanges = [Exchange.Bybit],
                    },
                ],
            },
        ];

        public static readonly IEnumerable<SvcCoins.Responses.Coin> MixedCoinsWithAndWithoutTradingPairs =
        [
            new SvcCoins.Responses.Coin
            {
                Id = 1,
                Symbol = "BTC",
                Name = "Bitcoin",
                Category = null,
                MarketCapUsd = 1_200_000_000_000,
                PriceUsd = "50000.00",
                PriceChangePercentage24h = 3.5m,
                TradingPairs =
                [
                    new SvcCoins.Responses.TradingPair
                    {
                        Id = 1,
                        CoinQuote = new SvcCoins.Responses.TradingPairCoinQuote
                        {
                            Id = 5,
                            Symbol = "USDT",
                            Name = "Tether",
                        },
                        Exchanges = [Exchange.Binance],
                    },
                ],
            },
            new SvcCoins.Responses.Coin
            {
                Id = 2,
                Symbol = "ETH",
                Name = "Ethereum",
                Category = null,
                MarketCapUsd = 400_000_000_000,
                PriceUsd = "3000.00",
                PriceChangePercentage24h = -1.2m,
                TradingPairs = [],
            },
        ];

        public static readonly IEnumerable<SvcKline.KlineDataResponse> BitcoinKlineDataOnly =
        [
            new SvcKline.KlineDataResponse
            {
                IdTradingPair = 1,
                Klines =
                [
                    new Kline
                    {
                        OpenTime = DateTimeOffset.UtcNow.AddHours(-2).ToUnixTimeMilliseconds(),
                        OpenPrice = "50000",
                        HighPrice = "51000",
                        LowPrice = "49000",
                        ClosePrice = "50500",
                        Volume = "100",
                        CloseTime = DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeMilliseconds(),
                    },
                    new Kline
                    {
                        OpenTime = DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeMilliseconds(),
                        OpenPrice = "50500",
                        HighPrice = "52000",
                        LowPrice = "50000",
                        ClosePrice = "51500",
                        Volume = "150",
                        CloseTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    },
                ],
            },
        ];

        public static readonly IEnumerable<SvcExternal.Responses.Coins.Coin> SpotCoinsWithDuplicateCoinGeckoIds =
        [
            new SvcExternal.Responses.Coins.Coin
            {
                Symbol = "ADA",
                Name = "Cardano",
                Category = null,
                IdCoinGecko = "cardano", // Same CoinGecko ID
                TradingPairs =
                [
                    new SvcExternal.Responses.Coins.TradingPair
                    {
                        CoinQuote = new SvcExternal.Responses.Coins.TradingPairCoinQuote
                        {
                            Symbol = "USDT",
                            Name = "Tether",
                            Category = CoinCategory.Stablecoin,
                            IdCoinGecko = "tether",
                        },
                        ExchangeInfos =
                        [
                            new SvcExternal.Responses.Coins.TradingPairExchangeInfo
                            {
                                Exchange = Exchange.Binance,
                            },
                        ],
                    },
                ],
            },
            new SvcExternal.Responses.Coins.Coin
            {
                Symbol = "ADA-FAKE",
                Name = "Cardano Fake",
                Category = null,
                IdCoinGecko = "cardano", // Same CoinGecko ID as above
                TradingPairs =
                [
                    new SvcExternal.Responses.Coins.TradingPair
                    {
                        CoinQuote = new SvcExternal.Responses.Coins.TradingPairCoinQuote
                        {
                            Symbol = "USDT",
                            Name = "Tether",
                            Category = CoinCategory.Stablecoin,
                            IdCoinGecko = "tether",
                        },
                        ExchangeInfos =
                        [
                            new SvcExternal.Responses.Coins.TradingPairExchangeInfo
                            {
                                Exchange = Exchange.Bybit,
                            },
                        ],
                    },
                ],
            },
        ];

        public static readonly IEnumerable<SvcExternal.Responses.Coins.Coin> SpotCoinsWithDuplicateSymbolNamePairs =
        [
            new SvcExternal.Responses.Coins.Coin
            {
                Symbol = "ADA",
                Name = "Cardano",
                Category = null,
                IdCoinGecko = "cardano",
                TradingPairs =
                [
                    new SvcExternal.Responses.Coins.TradingPair
                    {
                        CoinQuote = new SvcExternal.Responses.Coins.TradingPairCoinQuote
                        {
                            Symbol = "USDT",
                            Name = "Tether",
                            Category = CoinCategory.Stablecoin,
                            IdCoinGecko = "tether",
                        },
                        ExchangeInfos =
                        [
                            new SvcExternal.Responses.Coins.TradingPairExchangeInfo
                            {
                                Exchange = Exchange.Binance,
                            },
                        ],
                    },
                ],
            },
            new SvcExternal.Responses.Coins.Coin
            {
                Symbol = "ADA", // Same Symbol-Name pair
                Name = "Cardano", // Same Symbol-Name pair
                Category = null,
                IdCoinGecko = "cardano-duplicate", // Different CoinGecko ID
                TradingPairs =
                [
                    new SvcExternal.Responses.Coins.TradingPair
                    {
                        CoinQuote = new SvcExternal.Responses.Coins.TradingPairCoinQuote
                        {
                            Symbol = "USDT",
                            Name = "Tether",
                            Category = CoinCategory.Stablecoin,
                            IdCoinGecko = "tether",
                        },
                        ExchangeInfos =
                        [
                            new SvcExternal.Responses.Coins.TradingPairExchangeInfo
                            {
                                Exchange = Exchange.Bybit,
                            },
                        ],
                    },
                ],
            },
        ];

        public static readonly IEnumerable<SvcCoins.Responses.Coin> DbCoinsWithMatchingCoinGeckoIds =
        [
            new SvcCoins.Responses.Coin
            {
                Id = 1,
                Symbol = "BTC",
                Name = "Bitcoin",
                Category = null,
                MarketCapUsd = 1_200_000_000_000,
                PriceUsd = "50000.00",
                PriceChangePercentage24h = 3.5m,
                IdCoinGecko = "cardano", // Matches CoinGecko ID from spot coins
                TradingPairs = [],
            },
        ];

        public static readonly IEnumerable<SvcCoins.Responses.Coin> DbCoinsWithMatchingSymbolNamePairs =
        [
            new SvcCoins.Responses.Coin
            {
                Id = 1,
                Symbol = "ADA",
                Name = "Cardano",
                Category = null,
                MarketCapUsd = 15_000_000_000,
                PriceUsd = "0.45",
                PriceChangePercentage24h = 5.2m,
                IdCoinGecko = "cardano-existing",
                TradingPairs = // Main coin - has trading pairs
                [
                    new SvcCoins.Responses.TradingPair
                    {
                        Id = 1,
                        CoinQuote = new SvcCoins.Responses.TradingPairCoinQuote
                        {
                            Id = 5,
                            Symbol = "USDT",
                            Name = "Tether",
                        },
                        Exchanges = [Exchange.Binance],
                    },
                ],
            },
        ];
    }
}
