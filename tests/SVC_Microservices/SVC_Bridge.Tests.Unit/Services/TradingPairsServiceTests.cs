using FluentResults;
using SharedLibrary.Enums;
using SharedLibrary.Errors;
using SVC_Bridge.MicroserviceClients.SvcCoins;
using SVC_Bridge.MicroserviceClients.SvcCoins.Contracts.Requests;
using SVC_Bridge.MicroserviceClients.SvcExternal;
using SVC_Bridge.Services;
using SvcCoins = SVC_Bridge.MicroserviceClients.SvcCoins.Contracts.Responses;
using SvcExternal = SVC_Bridge.MicroserviceClients.SvcExternal.Contracts.Responses.Coins;

namespace SVC_Bridge.Tests.Unit.Services;

public class TradingPairsServiceTests
{
    private readonly Mock<ISvcCoinsClient> _mockSvcCoinsClient;
    private readonly Mock<ISvcExternalClient> _mockSvcExternalClient;
    private readonly TradingPairsService _tradingPairsService;

    public TradingPairsServiceTests()
    {
        _mockSvcCoinsClient = new Mock<ISvcCoinsClient>();
        _mockSvcExternalClient = new Mock<ISvcExternalClient>();
        _tradingPairsService = new TradingPairsService(
            _mockSvcCoinsClient.Object,
            _mockSvcExternalClient.Object
        );
    }

    [Fact]
    public async Task UpdateTradingPairs_WhenSuccessfulFlowWithNewQuoteCoins_ShouldReturnUpdatedCoins()
    {
        // Arrange
        var coinId1 = 1;

        _mockSvcCoinsClient
            .Setup(client => client.GetAllCoins())
            .ReturnsAsync(Result.Ok(TestData.BitcoinMainCoinOnly));

        _mockSvcExternalClient
            .Setup(client => client.GetAllSpotCoins())
            .ReturnsAsync(Result.Ok(TestData.BitcoinSpotCoinWithUsdtPair));

        _mockSvcCoinsClient
            .Setup(client =>
                client.CreateQuoteCoins(It.IsAny<IEnumerable<QuoteCoinCreationRequest>>())
            )
            .ReturnsAsync(Result.Ok(TestData.CreatedUsdtQuoteCoin));

        _mockSvcCoinsClient
            .Setup(client =>
                client.ReplaceTradingPairs(It.IsAny<IEnumerable<TradingPairCreationRequest>>())
            )
            .ReturnsAsync(Result.Ok(TestData.UpdatedBitcoinWithTradingPair));

        _mockSvcCoinsClient
            .Setup(client => client.DeleteUnreferencedCoins())
            .ReturnsAsync(Result.Ok());

        // Act
        var result = await _tradingPairsService.UpdateTradingPairs();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);

        var coinsList = result.Value.ToList();
        coinsList[0].Id.Should().Be(coinId1);
        coinsList[0].Symbol.Should().Be("BTC");
        coinsList[0].TradingPairs.Should().HaveCount(1);

        // Verify client calls
        _mockSvcCoinsClient.Verify(client => client.GetAllCoins(), Times.Once);
        _mockSvcExternalClient.Verify(client => client.GetAllSpotCoins(), Times.Once);
        _mockSvcCoinsClient.Verify(
            client => client.CreateQuoteCoins(TestData.ExpectedUsdtQuoteCoinCreationRequest),
            Times.Once
        );
        _mockSvcCoinsClient.Verify(
            client =>
                client.ReplaceTradingPairs(
                    It.Is<IEnumerable<TradingPairCreationRequest>>(requests =>
                        requests.Count()
                            == TestData.ExpectedBitcoinUsdtTradingPairCreationRequest.Length
                        && requests.First().IdCoinMain
                            == TestData.ExpectedBitcoinUsdtTradingPairCreationRequest[0].IdCoinMain
                        && requests.First().IdCoinQuote
                            == TestData.ExpectedBitcoinUsdtTradingPairCreationRequest[0].IdCoinQuote
                        && requests
                            .First()
                            .Exchanges.SequenceEqual(
                                TestData.ExpectedBitcoinUsdtTradingPairCreationRequest[0].Exchanges
                            )
                    )
                ),
            Times.Once
        );
        _mockSvcCoinsClient.Verify(client => client.DeleteUnreferencedCoins(), Times.Once);
        _mockSvcCoinsClient.VerifyNoOtherCalls();
        _mockSvcExternalClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateTradingPairs_WhenSuccessfulFlowWithoutNewQuoteCoins_ShouldReturnUpdatedCoins()
    {
        // Arrange
        _mockSvcCoinsClient
            .Setup(client => client.GetAllCoins())
            .ReturnsAsync(Result.Ok(TestData.BitcoinAndUsdtCoins));

        _mockSvcExternalClient
            .Setup(client => client.GetAllSpotCoins())
            .ReturnsAsync(Result.Ok(TestData.BitcoinSpotCoinWithUsdtPair));

        _mockSvcCoinsClient
            .Setup(client =>
                client.ReplaceTradingPairs(It.IsAny<IEnumerable<TradingPairCreationRequest>>())
            )
            .ReturnsAsync(Result.Ok(TestData.UpdatedBitcoinWithTradingPair));

        _mockSvcCoinsClient
            .Setup(client => client.DeleteUnreferencedCoins())
            .ReturnsAsync(Result.Ok());

        // Act
        var result = await _tradingPairsService.UpdateTradingPairs();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);

        // Verify client calls - CreateQuoteCoins should not be called
        _mockSvcCoinsClient.Verify(client => client.GetAllCoins(), Times.Once);
        _mockSvcExternalClient.Verify(client => client.GetAllSpotCoins(), Times.Once);
        _mockSvcCoinsClient.Verify(
            client =>
                client.ReplaceTradingPairs(
                    It.Is<IEnumerable<TradingPairCreationRequest>>(requests =>
                        requests.Count()
                            == TestData.ExpectedBitcoinUsdtTradingPairCreationRequest.Length
                        && requests.First().IdCoinMain
                            == TestData.ExpectedBitcoinUsdtTradingPairCreationRequest[0].IdCoinMain
                        && requests.First().IdCoinQuote
                            == TestData.ExpectedBitcoinUsdtTradingPairCreationRequest[0].IdCoinQuote
                        && requests
                            .First()
                            .Exchanges.SequenceEqual(
                                TestData.ExpectedBitcoinUsdtTradingPairCreationRequest[0].Exchanges
                            )
                    )
                ),
            Times.Once
        );
        _mockSvcCoinsClient.Verify(client => client.DeleteUnreferencedCoins(), Times.Once);
        _mockSvcCoinsClient.Verify(
            client => client.CreateQuoteCoins(It.IsAny<IEnumerable<QuoteCoinCreationRequest>>()),
            Times.Never
        );
        _mockSvcCoinsClient.VerifyNoOtherCalls();
        _mockSvcExternalClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateTradingPairs_WhenGetAllCoinsFails_ShouldReturnFailureResult()
    {
        // Arrange
        var coinsServiceError = new GenericErrors.InternalError("Coins service unavailable");

        _mockSvcCoinsClient
            .Setup(client => client.GetAllCoins())
            .ReturnsAsync(Result.Fail(coinsServiceError));

        // Set up external service to return success (since both are called in parallel)
        _mockSvcExternalClient
            .Setup(client => client.GetAllSpotCoins())
            .ReturnsAsync(Result.Ok(TestData.BitcoinSpotCoinWithUsdtPair));

        // Act
        var result = await _tradingPairsService.UpdateTradingPairs();

        // Assert
        result.IsFailed.Should().BeTrue();
        result
            .Errors.Should()
            .Contain(error =>
                error.Message.Contains("Failed to retrieve coins from coins service")
            );

        // Check that the original error is nested as a reason within the InternalError
        var internalError = result.Errors.OfType<GenericErrors.InternalError>().First();
        internalError.Reasons.Should().Contain(coinsServiceError);

        // Verify both services were called (they run in parallel)
        _mockSvcCoinsClient.Verify(client => client.GetAllCoins(), Times.Once);
        _mockSvcExternalClient.Verify(client => client.GetAllSpotCoins(), Times.Once);
        _mockSvcCoinsClient.VerifyNoOtherCalls();
        _mockSvcExternalClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateTradingPairs_WhenGetAllSpotCoinsFails_ShouldReturnFailureResult()
    {
        // Arrange
        var externalServiceError = new GenericErrors.InternalError("External service unavailable");

        _mockSvcCoinsClient
            .Setup(client => client.GetAllCoins())
            .ReturnsAsync(Result.Ok(TestData.BitcoinMainCoinOnly));

        _mockSvcExternalClient
            .Setup(client => client.GetAllSpotCoins())
            .ReturnsAsync(Result.Fail(externalServiceError));

        // Act
        var result = await _tradingPairsService.UpdateTradingPairs();

        // Assert
        result.IsFailed.Should().BeTrue();
        result
            .Errors.Should()
            .Contain(error =>
                error.Message.Contains("Failed to retrieve spot coins from external service")
            );

        // Check that the original error is nested as a reason within the InternalError
        var internalError = result.Errors.OfType<GenericErrors.InternalError>().First();
        internalError.Reasons.Should().Contain(externalServiceError);

        // Verify client calls - should stop after external service fails
        _mockSvcCoinsClient.Verify(client => client.GetAllCoins(), Times.Once);
        _mockSvcExternalClient.Verify(client => client.GetAllSpotCoins(), Times.Once);
        _mockSvcCoinsClient.VerifyNoOtherCalls();
        _mockSvcExternalClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateTradingPairs_WhenNoValidSpotCoins_ShouldReturnEmptyResult()
    {
        // Arrange
        _mockSvcCoinsClient
            .Setup(client => client.GetAllCoins())
            .ReturnsAsync(Result.Ok(TestData.BitcoinMainCoinOnly));

        _mockSvcExternalClient
            .Setup(client => client.GetAllSpotCoins())
            .ReturnsAsync(Result.Ok(TestData.EthereumSpotCoinWithUsdtPair)); // No matching main coin

        // Act
        var result = await _tradingPairsService.UpdateTradingPairs();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();

        // Verify no further processing should occur
        _mockSvcCoinsClient.Verify(client => client.GetAllCoins(), Times.Once);
        _mockSvcExternalClient.Verify(client => client.GetAllSpotCoins(), Times.Once);
        _mockSvcCoinsClient.VerifyNoOtherCalls();
        _mockSvcExternalClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateTradingPairs_WhenCreateQuoteCoinsFails_ShouldReturnFailureResult()
    {
        // Arrange
        var createCoinsError = new GenericErrors.InternalError("Failed to create quote coins");

        _mockSvcCoinsClient
            .Setup(client => client.GetAllCoins())
            .ReturnsAsync(Result.Ok(TestData.BitcoinMainCoinOnly));

        _mockSvcExternalClient
            .Setup(client => client.GetAllSpotCoins())
            .ReturnsAsync(Result.Ok(TestData.BitcoinSpotCoinWithUsdtPair));

        _mockSvcCoinsClient
            .Setup(client =>
                client.CreateQuoteCoins(It.IsAny<IEnumerable<QuoteCoinCreationRequest>>())
            )
            .ReturnsAsync(Result.Fail(createCoinsError));

        // Act
        var result = await _tradingPairsService.UpdateTradingPairs();

        // Assert
        result.IsFailed.Should().BeTrue();
        result
            .Errors.Should()
            .Contain(error => error.Message.Contains("Failed to create new quote coins"));

        // Check that the original error is nested as a reason within the InternalError
        var internalError = result.Errors.OfType<GenericErrors.InternalError>().First();
        internalError.Reasons.Should().Contain(createCoinsError);

        // Verify calls up to the failure point
        _mockSvcCoinsClient.Verify(client => client.GetAllCoins(), Times.Once);
        _mockSvcExternalClient.Verify(client => client.GetAllSpotCoins(), Times.Once);
        _mockSvcCoinsClient.Verify(
            client => client.CreateQuoteCoins(TestData.ExpectedUsdtQuoteCoinCreationRequest),
            Times.Once
        );
        _mockSvcCoinsClient.VerifyNoOtherCalls();
        _mockSvcExternalClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateTradingPairs_WhenReplaceTradingPairsFails_ShouldReturnFailureResult()
    {
        // Arrange
        var replaceTradingPairsError = new GenericErrors.InternalError(
            "Failed to replace trading pairs"
        );

        _mockSvcCoinsClient
            .Setup(client => client.GetAllCoins())
            .ReturnsAsync(Result.Ok(TestData.BitcoinAndUsdtCoins));

        _mockSvcExternalClient
            .Setup(client => client.GetAllSpotCoins())
            .ReturnsAsync(Result.Ok(TestData.BitcoinSpotCoinWithUsdtPair));

        _mockSvcCoinsClient
            .Setup(client =>
                client.ReplaceTradingPairs(It.IsAny<IEnumerable<TradingPairCreationRequest>>())
            )
            .ReturnsAsync(Result.Fail(replaceTradingPairsError));

        // Act
        var result = await _tradingPairsService.UpdateTradingPairs();

        // Assert
        result.IsFailed.Should().BeTrue();
        result
            .Errors.Should()
            .Contain(error => error.Message.Contains("Failed to replace trading pairs"));

        // Check that the original error is nested as a reason within the InternalError
        var internalError = result.Errors.OfType<GenericErrors.InternalError>().First();
        internalError.Reasons.Should().Contain(replaceTradingPairsError);

        // Verify calls up to the failure point
        _mockSvcCoinsClient.Verify(client => client.GetAllCoins(), Times.Once);
        _mockSvcExternalClient.Verify(client => client.GetAllSpotCoins(), Times.Once);
        _mockSvcCoinsClient.Verify(
            client =>
                client.ReplaceTradingPairs(
                    It.Is<IEnumerable<TradingPairCreationRequest>>(requests =>
                        requests.Count()
                            == TestData.ExpectedBitcoinUsdtTradingPairCreationRequest.Length
                        && requests.First().IdCoinMain
                            == TestData.ExpectedBitcoinUsdtTradingPairCreationRequest[0].IdCoinMain
                        && requests.First().IdCoinQuote
                            == TestData.ExpectedBitcoinUsdtTradingPairCreationRequest[0].IdCoinQuote
                        && requests
                            .First()
                            .Exchanges.SequenceEqual(
                                TestData.ExpectedBitcoinUsdtTradingPairCreationRequest[0].Exchanges
                            )
                    )
                ),
            Times.Once
        );
        _mockSvcCoinsClient.VerifyNoOtherCalls();
        _mockSvcExternalClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateTradingPairs_WhenDeleteUnreferencedCoinsFails_ShouldReturnFailureResult()
    {
        // Arrange
        var deleteCoinsError = new GenericErrors.InternalError(
            "Failed to delete unreferenced coins"
        );

        _mockSvcCoinsClient
            .Setup(client => client.GetAllCoins())
            .ReturnsAsync(Result.Ok(TestData.BitcoinAndUsdtCoins));

        _mockSvcExternalClient
            .Setup(client => client.GetAllSpotCoins())
            .ReturnsAsync(Result.Ok(TestData.BitcoinSpotCoinWithUsdtPair));

        _mockSvcCoinsClient
            .Setup(client =>
                client.ReplaceTradingPairs(It.IsAny<IEnumerable<TradingPairCreationRequest>>())
            )
            .ReturnsAsync(Result.Ok(TestData.UpdatedBitcoinWithTradingPair));

        _mockSvcCoinsClient
            .Setup(client => client.DeleteUnreferencedCoins())
            .ReturnsAsync(Result.Fail(deleteCoinsError));

        // Act
        var result = await _tradingPairsService.UpdateTradingPairs();

        // Assert
        result.IsFailed.Should().BeTrue();
        result
            .Errors.Should()
            .Contain(error => error.Message.Contains("Failed to delete unreferenced coins"));

        // Check that the original error is nested as a reason within the InternalError
        var internalError = result.Errors.OfType<GenericErrors.InternalError>().First();
        internalError.Reasons.Should().Contain(deleteCoinsError);

        // Verify all calls were made up to the failure point
        _mockSvcCoinsClient.Verify(client => client.GetAllCoins(), Times.Once);
        _mockSvcExternalClient.Verify(client => client.GetAllSpotCoins(), Times.Once);
        _mockSvcCoinsClient.Verify(
            client =>
                client.ReplaceTradingPairs(
                    It.Is<IEnumerable<TradingPairCreationRequest>>(requests =>
                        requests.Count()
                            == TestData.ExpectedBitcoinUsdtTradingPairCreationRequest.Length
                        && requests.First().IdCoinMain
                            == TestData.ExpectedBitcoinUsdtTradingPairCreationRequest[0].IdCoinMain
                        && requests.First().IdCoinQuote
                            == TestData.ExpectedBitcoinUsdtTradingPairCreationRequest[0].IdCoinQuote
                        && requests
                            .First()
                            .Exchanges.SequenceEqual(
                                TestData.ExpectedBitcoinUsdtTradingPairCreationRequest[0].Exchanges
                            )
                    )
                ),
            Times.Once
        );
        _mockSvcCoinsClient.Verify(client => client.DeleteUnreferencedCoins(), Times.Once);
        _mockSvcCoinsClient.VerifyNoOtherCalls();
        _mockSvcExternalClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateTradingPairs_WhenNoTradingPairsToCreate_ShouldReturnEmptyResult()
    {
        // Arrange
        _mockSvcCoinsClient
            .Setup(client => client.GetAllCoins())
            .ReturnsAsync(Result.Ok(TestData.BitcoinAndUsdtCoins));

        _mockSvcExternalClient
            .Setup(client => client.GetAllSpotCoins())
            .ReturnsAsync(Result.Ok(TestData.BitcoinSpotCoinWithoutTradingPairs));

        // Act
        var result = await _tradingPairsService.UpdateTradingPairs();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();

        // Verify no trading pair operations should occur
        _mockSvcCoinsClient.Verify(client => client.GetAllCoins(), Times.Once);
        _mockSvcExternalClient.Verify(client => client.GetAllSpotCoins(), Times.Once);
        _mockSvcCoinsClient.VerifyNoOtherCalls();
        _mockSvcExternalClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateTradingPairs_WhenNoCoinsExist_ShouldReturnEmptyResult()
    {
        // Arrange
        _mockSvcCoinsClient
            .Setup(client => client.GetAllCoins())
            .ReturnsAsync(Result.Ok(Enumerable.Empty<SvcCoins.Coin>()));

        _mockSvcExternalClient
            .Setup(client => client.GetAllSpotCoins())
            .ReturnsAsync(Result.Ok(TestData.BitcoinSpotCoinWithUsdtPair));

        // Act
        var result = await _tradingPairsService.UpdateTradingPairs();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();

        _mockSvcCoinsClient.Verify(client => client.GetAllCoins(), Times.Once);
        _mockSvcExternalClient.Verify(client => client.GetAllSpotCoins(), Times.Once);
        _mockSvcCoinsClient.VerifyNoOtherCalls();
        _mockSvcExternalClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateTradingPairs_WhenMultipleCoinsAndComplexScenario_ShouldHandleCorrectly()
    {
        // Arrange
        _mockSvcCoinsClient
            .Setup(client => client.GetAllCoins())
            .ReturnsAsync(Result.Ok(TestData.BitcoinEthereumAndUsdtCoins));

        _mockSvcExternalClient
            .Setup(client => client.GetAllSpotCoins())
            .ReturnsAsync(Result.Ok(TestData.BitcoinAndEthereumSpotCoinsWithMultiplePairs));

        _mockSvcCoinsClient
            .Setup(client =>
                client.CreateQuoteCoins(It.IsAny<IEnumerable<QuoteCoinCreationRequest>>())
            )
            .ReturnsAsync(Result.Ok(TestData.CreatedBusdQuoteCoin));

        _mockSvcCoinsClient
            .Setup(client =>
                client.ReplaceTradingPairs(It.IsAny<IEnumerable<TradingPairCreationRequest>>())
            )
            .ReturnsAsync(Result.Ok(TestData.UpdatedBitcoinAndEthereumWithTradingPairs));

        _mockSvcCoinsClient
            .Setup(client => client.DeleteUnreferencedCoins())
            .ReturnsAsync(Result.Ok());

        // Act
        var result = await _tradingPairsService.UpdateTradingPairs();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);

        var coinsList = result.Value.ToList();
        coinsList.Should().Contain(coin => coin.Symbol == "BTC");
        coinsList.Should().Contain(coin => coin.Symbol == "ETH");

        // Verify client calls
        _mockSvcCoinsClient.Verify(client => client.GetAllCoins(), Times.Once);
        _mockSvcExternalClient.Verify(client => client.GetAllSpotCoins(), Times.Once);
        _mockSvcCoinsClient.Verify(
            client => client.CreateQuoteCoins(TestData.ExpectedBusdQuoteCoinCreationRequest),
            Times.Once
        );
        _mockSvcCoinsClient.Verify(
            client =>
                client.ReplaceTradingPairs(
                    It.Is<IEnumerable<TradingPairCreationRequest>>(requests =>
                        requests.Count()
                            == TestData.ExpectedMultipleTradingPairCreationRequests.Length
                        && requests.First().IdCoinMain
                            == TestData.ExpectedMultipleTradingPairCreationRequests[0].IdCoinMain
                        && requests.First().IdCoinQuote
                            == TestData.ExpectedMultipleTradingPairCreationRequests[0].IdCoinQuote
                        && requests
                            .First()
                            .Exchanges.SequenceEqual(
                                TestData.ExpectedMultipleTradingPairCreationRequests[0].Exchanges
                            )
                    )
                ),
            Times.Once
        );
        _mockSvcCoinsClient.Verify(client => client.DeleteUnreferencedCoins(), Times.Once);
        _mockSvcCoinsClient.VerifyNoOtherCalls();
        _mockSvcExternalClient.VerifyNoOtherCalls();
    }

    private static class TestData
    {
        public static readonly QuoteCoinCreationRequest[] ExpectedUsdtQuoteCoinCreationRequest =
        [
            new()
            {
                Symbol = "USDT",
                Name = "Tether",
                Category = CoinCategory.Stablecoin,
                IdCoinGecko = "tether",
            },
        ];

        public static readonly QuoteCoinCreationRequest[] ExpectedBusdQuoteCoinCreationRequest =
        [
            new()
            {
                Symbol = "BUSD",
                Name = "Binance USD",
                Category = CoinCategory.Stablecoin,
                IdCoinGecko = "binance-usd",
            },
        ];

        public static readonly TradingPairCreationRequest[] ExpectedBitcoinUsdtTradingPairCreationRequest =
        [
            new()
            {
                IdCoinMain = 1,
                IdCoinQuote = 2,
                Exchanges = [Exchange.Binance],
            },
        ];

        public static readonly TradingPairCreationRequest[] ExpectedMultipleTradingPairCreationRequests =
        [
            new()
            {
                IdCoinMain = 1, // Bitcoin
                IdCoinQuote = 3, // USDT
                Exchanges = [Exchange.Binance],
            },
            new()
            {
                IdCoinMain = 1, // Bitcoin
                IdCoinQuote = 4, // BUSD (newly created)
                Exchanges = [Exchange.Binance],
            },
            new()
            {
                IdCoinMain = 2, // Ethereum
                IdCoinQuote = 3, // USDT
                Exchanges = [Exchange.Binance, Exchange.Bybit],
            },
        ];

        public static readonly IEnumerable<SvcCoins.Coin> BitcoinMainCoinOnly =
        [
            new()
            {
                Id = 1,
                Symbol = "BTC",
                Name = "Bitcoin",
                Category = null,
                IdCoinGecko = "bitcoin",
                TradingPairs =
                [
                    new()
                    {
                        Id = 1,
                        CoinQuote = new()
                        {
                            Id = 10,
                            Symbol = "EXISTING",
                            Name = "Existing Coin",
                        },
                        Exchanges = [Exchange.Binance],
                    },
                ],
            },
        ];

        public static readonly IEnumerable<SvcCoins.Coin> BitcoinAndUsdtCoins =
        [
            new()
            {
                Id = 1,
                Symbol = "BTC",
                Name = "Bitcoin",
                Category = null,
                IdCoinGecko = "bitcoin",
                TradingPairs =
                [
                    new()
                    {
                        Id = 1,
                        CoinQuote = new()
                        {
                            Id = 10,
                            Symbol = "EXISTING",
                            Name = "Existing Coin",
                        },
                        Exchanges = [Exchange.Binance],
                    },
                ],
            },
            new()
            {
                Id = 2,
                Symbol = "USDT",
                Name = "Tether",
                Category = CoinCategory.Stablecoin,
                IdCoinGecko = "tether",
                TradingPairs = [],
            },
        ];

        public static readonly IEnumerable<SvcCoins.Coin> BitcoinEthereumAndUsdtCoins =
        [
            new()
            {
                Id = 1,
                Symbol = "BTC",
                Name = "Bitcoin",
                Category = null,
                IdCoinGecko = "bitcoin",
                TradingPairs =
                [
                    new()
                    {
                        Id = 1,
                        CoinQuote = new()
                        {
                            Id = 10,
                            Symbol = "EXISTING",
                            Name = "Existing Coin",
                        },
                        Exchanges = [Exchange.Binance],
                    },
                ],
            },
            new()
            {
                Id = 2,
                Symbol = "ETH",
                Name = "Ethereum",
                Category = null,
                IdCoinGecko = "ethereum",
                TradingPairs =
                [
                    new()
                    {
                        Id = 2,
                        CoinQuote = new()
                        {
                            Id = 11,
                            Symbol = "EXISTING2",
                            Name = "Existing Coin 2",
                        },
                        Exchanges = [Exchange.Bybit],
                    },
                ],
            },
            new()
            {
                Id = 3,
                Symbol = "USDT",
                Name = "Tether",
                Category = CoinCategory.Stablecoin,
                IdCoinGecko = "tether",
                TradingPairs = [],
            },
        ];

        public static readonly IEnumerable<SvcExternal.Coin> BitcoinSpotCoinWithUsdtPair =
        [
            new()
            {
                Symbol = "BTC",
                Name = "Bitcoin",
                TradingPairs =
                [
                    new()
                    {
                        CoinQuote = new()
                        {
                            Symbol = "USDT",
                            Name = "Tether",
                            Category = CoinCategory.Stablecoin,
                            IdCoinGecko = "tether",
                        },
                        ExchangeInfos = [new() { Exchange = Exchange.Binance }],
                    },
                ],
            },
        ];

        public static readonly IEnumerable<SvcExternal.Coin> BitcoinSpotCoinWithoutTradingPairs =
        [
            new()
            {
                Symbol = "BTC",
                Name = "Bitcoin",
                TradingPairs = [],
            },
        ];

        public static readonly IEnumerable<SvcExternal.Coin> EthereumSpotCoinWithUsdtPair =
        [
            new()
            {
                Symbol = "ETH",
                Name = "Ethereum",
                TradingPairs =
                [
                    new()
                    {
                        CoinQuote = new()
                        {
                            Symbol = "USDT",
                            Name = "Tether",
                            Category = CoinCategory.Stablecoin,
                            IdCoinGecko = "tether",
                        },
                        ExchangeInfos = [new() { Exchange = Exchange.Binance }],
                    },
                ],
            },
        ];

        public static readonly IEnumerable<SvcExternal.Coin> BitcoinAndEthereumSpotCoinsWithMultiplePairs =
        [
            new()
            {
                Symbol = "BTC",
                Name = "Bitcoin",
                TradingPairs =
                [
                    new()
                    {
                        CoinQuote = new()
                        {
                            Symbol = "USDT",
                            Name = "Tether",
                            Category = CoinCategory.Stablecoin,
                            IdCoinGecko = "tether",
                        },
                        ExchangeInfos = [new() { Exchange = Exchange.Binance }],
                    },
                    new()
                    {
                        CoinQuote = new()
                        {
                            Symbol = "BUSD",
                            Name = "Binance USD",
                            Category = CoinCategory.Stablecoin,
                            IdCoinGecko = "binance-usd",
                        },
                        ExchangeInfos = [new() { Exchange = Exchange.Binance }],
                    },
                ],
            },
            new()
            {
                Symbol = "ETH",
                Name = "Ethereum",
                TradingPairs =
                [
                    new()
                    {
                        CoinQuote = new()
                        {
                            Symbol = "USDT",
                            Name = "Tether",
                            Category = CoinCategory.Stablecoin,
                            IdCoinGecko = "tether",
                        },
                        ExchangeInfos =
                        [
                            new() { Exchange = Exchange.Binance },
                            new() { Exchange = Exchange.Bybit },
                        ],
                    },
                ],
            },
        ];

        public static readonly IEnumerable<SvcCoins.TradingPairCoinQuote> CreatedUsdtQuoteCoin =
        [
            new()
            {
                Id = 2,
                Symbol = "USDT",
                Name = "Tether",
                Category = CoinCategory.Stablecoin,
                IdCoinGecko = "tether",
                MarketCapUsd = 50000000,
                PriceUsd = "1.0",
                PriceChangePercentage24h = 0.01m,
            },
        ];

        public static readonly IEnumerable<SvcCoins.TradingPairCoinQuote> CreatedBusdQuoteCoin =
        [
            new()
            {
                Id = 4,
                Symbol = "BUSD",
                Name = "Binance USD",
                Category = CoinCategory.Stablecoin,
                IdCoinGecko = "binance-usd",
                MarketCapUsd = 10000000,
                PriceUsd = "1.0",
                PriceChangePercentage24h = 0.02m,
            },
        ];

        public static readonly IEnumerable<SvcCoins.Coin> UpdatedBitcoinWithTradingPair =
        [
            new()
            {
                Id = 1,
                Symbol = "BTC",
                Name = "Bitcoin",
                Category = null,
                IdCoinGecko = "bitcoin",
                MarketCapUsd = 1000000000,
                PriceUsd = "50000",
                PriceChangePercentage24h = 2.5m,
                TradingPairs =
                [
                    new()
                    {
                        Id = 1,
                        CoinQuote = new()
                        {
                            Id = 2,
                            Symbol = "USDT",
                            Name = "Tether",
                            Category = CoinCategory.Stablecoin,
                            IdCoinGecko = "tether",
                            MarketCapUsd = 50000000,
                            PriceUsd = "1.0",
                            PriceChangePercentage24h = 0.01m,
                        },
                        Exchanges = [Exchange.Binance],
                    },
                ],
            },
        ];

        public static readonly IEnumerable<SvcCoins.Coin> UpdatedBitcoinAndEthereumWithTradingPairs =
        [
            new()
            {
                Id = 1,
                Symbol = "BTC",
                Name = "Bitcoin",
                Category = null,
                IdCoinGecko = "bitcoin",
                MarketCapUsd = 1000000000,
                PriceUsd = "50000",
                PriceChangePercentage24h = 2.5m,
                TradingPairs =
                [
                    new()
                    {
                        Id = 1,
                        CoinQuote = new()
                        {
                            Id = 3,
                            Symbol = "USDT",
                            Name = "Tether",
                            Category = CoinCategory.Stablecoin,
                            IdCoinGecko = "tether",
                            MarketCapUsd = 50000000,
                            PriceUsd = "1.0",
                            PriceChangePercentage24h = 0.01m,
                        },
                        Exchanges = [Exchange.Binance],
                    },
                    new()
                    {
                        Id = 2,
                        CoinQuote = new()
                        {
                            Id = 4,
                            Symbol = "BUSD",
                            Name = "Binance USD",
                            Category = CoinCategory.Stablecoin,
                            IdCoinGecko = "binance-usd",
                            MarketCapUsd = 10000000,
                            PriceUsd = "1.0",
                            PriceChangePercentage24h = 0.02m,
                        },
                        Exchanges = [Exchange.Binance],
                    },
                ],
            },
            new()
            {
                Id = 2,
                Symbol = "ETH",
                Name = "Ethereum",
                Category = null,
                IdCoinGecko = "ethereum",
                MarketCapUsd = 500000000,
                PriceUsd = "3000",
                PriceChangePercentage24h = 1.8m,
                TradingPairs =
                [
                    new()
                    {
                        Id = 3,
                        CoinQuote = new()
                        {
                            Id = 3,
                            Symbol = "USDT",
                            Name = "Tether",
                            Category = CoinCategory.Stablecoin,
                            IdCoinGecko = "tether",
                            MarketCapUsd = 50000000,
                            PriceUsd = "1.0",
                            PriceChangePercentage24h = 0.01m,
                        },
                        Exchanges = [Exchange.Binance, Exchange.Bybit],
                    },
                ],
            },
        ];
    }
}
