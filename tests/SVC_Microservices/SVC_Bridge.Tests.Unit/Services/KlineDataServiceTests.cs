using FluentResults;
using SharedLibrary.Enums;
using SharedLibrary.Errors;
using SVC_Bridge.MicroserviceClients.SvcCoins;
using SVC_Bridge.MicroserviceClients.SvcCoins.Contracts.Responses;
using SVC_Bridge.MicroserviceClients.SvcExternal;
using SVC_Bridge.MicroserviceClients.SvcExternal.Contracts.Requests;
using SVC_Bridge.MicroserviceClients.SvcKline;
using SVC_Bridge.MicroserviceClients.SvcKline.Contracts.Requests;
using SVC_Bridge.Services;
using SvcExternal = SVC_Bridge.MicroserviceClients.SvcExternal.Contracts.Responses.KlineData;
using SvcKline = SVC_Bridge.MicroserviceClients.SvcKline.Contracts.Responses;

namespace SVC_Bridge.Tests.Unit.Services;

public class KlineDataServiceTests
{
    private readonly Mock<ISvcCoinsClient> _mockSvcCoinsClient;
    private readonly Mock<ISvcExternalClient> _mockSvcExternalClient;
    private readonly Mock<ISvcKlineClient> _mockSvcKlineClient;
    private readonly KlineDataService _klineDataService;

    public KlineDataServiceTests()
    {
        _mockSvcCoinsClient = new Mock<ISvcCoinsClient>();
        _mockSvcExternalClient = new Mock<ISvcExternalClient>();
        _mockSvcKlineClient = new Mock<ISvcKlineClient>();
        _klineDataService = new KlineDataService(
            _mockSvcCoinsClient.Object,
            _mockSvcExternalClient.Object,
            _mockSvcKlineClient.Object
        );
    }

    [Fact]
    public async Task UpdateKlineData_WhenSuccessfulFlow_ShouldReturnUpdatedKlineData()
    {
        // Arrange
        var tradingPairId1 = 1;
        var tradingPairId2 = 2;

        _mockSvcCoinsClient
            .Setup(client => client.GetAllCoins())
            .ReturnsAsync(Result.Ok(TestData.BitcoinAndEthereumCoinsWithTradingPairs));

        _mockSvcExternalClient
            .Setup(client => client.GetKlineData(It.IsAny<KlineDataBatchRequest>()))
            .ReturnsAsync(Result.Ok(TestData.BitcoinAndEthereumKlineDataResponses));

        _mockSvcKlineClient
            .Setup(client =>
                client.ReplaceKlineData(It.IsAny<IEnumerable<KlineDataCreationRequest>>())
            )
            .ReturnsAsync(Result.Ok(TestData.ReplacedBitcoinAndEthereumKlineDataResponses));

        // Act
        var result = await _klineDataService.UpdateKlineData();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);

        var klineDataList = result.Value.ToList();

        klineDataList[0].IdTradingPair.Should().Be(tradingPairId1);
        klineDataList[0].KlineData.Should().HaveCount(2);

        klineDataList[1].IdTradingPair.Should().Be(tradingPairId2);
        klineDataList[1].KlineData.Should().HaveCount(2);

        // Verify client calls
        _mockSvcCoinsClient.Verify(client => client.GetAllCoins(), Times.Once);
        _mockSvcExternalClient.Verify(
            client =>
                client.GetKlineData(
                    It.Is<KlineDataBatchRequest>(request =>
                        request.Interval == ExchangeKlineInterval.OneDay
                        && request.Limit == 1000
                        && request.MainCoins.Count() == 2
                    )
                ),
            Times.Once
        );
        _mockSvcKlineClient.Verify(
            client =>
                client.ReplaceKlineData(
                    TestData.ExpectedBitcoinAndEthereumKlineDataCreationRequests
                ),
            Times.Once
        );
        _mockSvcCoinsClient.VerifyNoOtherCalls();
        _mockSvcExternalClient.VerifyNoOtherCalls();
        _mockSvcKlineClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateKlineData_WhenGetAllCoinsFails_ShouldReturnFailureResult()
    {
        // Arrange
        var coinsServiceError = new GenericErrors.InternalError("Coins service unavailable");

        _mockSvcCoinsClient
            .Setup(client => client.GetAllCoins())
            .ReturnsAsync(Result.Fail(coinsServiceError));

        // Act
        var result = await _klineDataService.UpdateKlineData();

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

        // Verify only GetAllCoins was called
        _mockSvcCoinsClient.Verify(client => client.GetAllCoins(), Times.Once);
        _mockSvcCoinsClient.VerifyNoOtherCalls();
        _mockSvcExternalClient.VerifyNoOtherCalls();
        _mockSvcKlineClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateKlineData_WhenNoCoinsExist_ShouldReturnEmptyResult()
    {
        // Arrange
        _mockSvcCoinsClient
            .Setup(client => client.GetAllCoins())
            .ReturnsAsync(Result.Ok(Enumerable.Empty<Coin>()));

        // Act
        var result = await _klineDataService.UpdateKlineData();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();

        _mockSvcCoinsClient.Verify(client => client.GetAllCoins(), Times.Once);
        _mockSvcCoinsClient.VerifyNoOtherCalls();
        _mockSvcExternalClient.VerifyNoOtherCalls();
        _mockSvcKlineClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateKlineData_WhenNoCoinsHaveTradingPairs_ShouldReturnEmptyResult()
    {
        // Arrange
        _mockSvcCoinsClient
            .Setup(client => client.GetAllCoins())
            .ReturnsAsync(Result.Ok(TestData.CoinsWithNoTradingPairs));

        // Act
        var result = await _klineDataService.UpdateKlineData();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();

        // Verify only GetAllCoins was called, no other calls should be made
        _mockSvcCoinsClient.Verify(client => client.GetAllCoins(), Times.Once);
        _mockSvcCoinsClient.VerifyNoOtherCalls();
        _mockSvcExternalClient.VerifyNoOtherCalls();
        _mockSvcKlineClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateKlineData_WhenExternalServiceFails_ShouldReturnFailureResult()
    {
        // Arrange
        var externalServiceError = new GenericErrors.InternalError("External service unavailable");

        _mockSvcCoinsClient
            .Setup(client => client.GetAllCoins())
            .ReturnsAsync(Result.Ok(TestData.SingleBitcoinCoinWithTradingPair));

        _mockSvcExternalClient
            .Setup(client => client.GetKlineData(It.IsAny<KlineDataBatchRequest>()))
            .ReturnsAsync(Result.Fail(externalServiceError));

        // Act
        var result = await _klineDataService.UpdateKlineData();

        // Assert
        result.IsFailed.Should().BeTrue();
        result
            .Errors.Should()
            .Contain(error =>
                error.Message.Contains("Failed to retrieve kline data from external service")
            );

        // Check that the original error is nested as a reason within the InternalError
        var internalError = result.Errors.OfType<GenericErrors.InternalError>().First();
        internalError.Reasons.Should().Contain(externalServiceError);

        // Verify client calls - should stop after external service fails
        _mockSvcCoinsClient.Verify(client => client.GetAllCoins(), Times.Once);
        _mockSvcExternalClient.Verify(
            client => client.GetKlineData(It.IsAny<KlineDataBatchRequest>()),
            Times.Once
        );

        _mockSvcCoinsClient.VerifyNoOtherCalls();
        _mockSvcKlineClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateKlineData_WhenKlineServiceFails_ShouldReturnFailureResult()
    {
        // Arrange
        var klineServiceError = new GenericErrors.InternalError("Database update failed");

        _mockSvcCoinsClient
            .Setup(client => client.GetAllCoins())
            .ReturnsAsync(Result.Ok(TestData.SingleBitcoinCoinWithTradingPair));

        _mockSvcExternalClient
            .Setup(client => client.GetKlineData(It.IsAny<KlineDataBatchRequest>()))
            .ReturnsAsync(Result.Ok(TestData.SingleBitcoinKlineDataResponse));

        _mockSvcKlineClient
            .Setup(client =>
                client.ReplaceKlineData(It.IsAny<IEnumerable<KlineDataCreationRequest>>())
            )
            .ReturnsAsync(Result.Fail(klineServiceError));

        // Act
        var result = await _klineDataService.UpdateKlineData();

        // Assert
        result.IsFailed.Should().BeTrue();
        result
            .Errors.Should()
            .Contain(error => error.Message.Contains("Failed to replace kline data"));

        // Check that the original error is nested as a reason within the InternalError
        var internalError = result.Errors.OfType<GenericErrors.InternalError>().First();
        internalError.Reasons.Should().Contain(klineServiceError);

        // Verify all client calls were made up to the failure point
        _mockSvcCoinsClient.Verify(client => client.GetAllCoins(), Times.Once);
        _mockSvcExternalClient.Verify(
            client => client.GetKlineData(It.IsAny<KlineDataBatchRequest>()),
            Times.Once
        );
        _mockSvcKlineClient.Verify(
            client =>
                client.ReplaceKlineData(TestData.ExpectedSingleBitcoinKlineDataCreationRequest),
            Times.Once
        );
    }

    [Fact]
    public async Task UpdateKlineData_WhenMixedCoinsWithAndWithoutTradingPairs_ShouldProcessOnlyValidOnes()
    {
        // Arrange
        var tradingPairId1 = 1;
        var tradingPairId3 = 3;

        _mockSvcCoinsClient
            .Setup(client => client.GetAllCoins())
            .ReturnsAsync(Result.Ok(TestData.MixedCoinsWithAndWithoutTradingPairs));

        _mockSvcExternalClient
            .Setup(client => client.GetKlineData(It.IsAny<KlineDataBatchRequest>()))
            .ReturnsAsync(Result.Ok(TestData.BitcoinAndCardanoKlineDataResponses));

        _mockSvcKlineClient
            .Setup(client =>
                client.ReplaceKlineData(It.IsAny<IEnumerable<KlineDataCreationRequest>>())
            )
            .ReturnsAsync(Result.Ok(TestData.ReplacedBitcoinAndCardanoKlineDataResponses));

        // Act
        var result = await _klineDataService.UpdateKlineData();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(klineData => klineData.IdTradingPair == tradingPairId1);
        result.Value.Should().Contain(klineData => klineData.IdTradingPair == tradingPairId3);

        // Verify client calls - only coins with trading pairs should be processed
        _mockSvcCoinsClient.Verify(client => client.GetAllCoins(), Times.Once);
        _mockSvcExternalClient.Verify(
            client =>
                client.GetKlineData(
                    It.Is<KlineDataBatchRequest>(request =>
                        request.MainCoins.Count() == 2 // Only Bitcoin and Cardano have trading pairs
                    )
                ),
            Times.Once
        );
        _mockSvcKlineClient.Verify(
            client =>
                client.ReplaceKlineData(
                    TestData.ExpectedBitcoinAndCardanoKlineDataCreationRequests
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task UpdateKlineData_WhenNoKlineDataReturned_ShouldReturnEmptyResult()
    {
        // Arrange
        _mockSvcCoinsClient
            .Setup(client => client.GetAllCoins())
            .ReturnsAsync(Result.Ok(TestData.SingleBitcoinCoinWithTradingPair));

        _mockSvcExternalClient
            .Setup(client => client.GetKlineData(It.IsAny<KlineDataBatchRequest>()))
            .ReturnsAsync(Result.Ok(TestData.EmptyKlineDataResponses));

        // Act
        var result = await _klineDataService.UpdateKlineData();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();

        // Verify client calls - should get kline data but no replacement due to empty data
        _mockSvcCoinsClient.Verify(client => client.GetAllCoins(), Times.Once);
        _mockSvcExternalClient.Verify(
            client => client.GetKlineData(It.IsAny<KlineDataBatchRequest>()),
            Times.Once
        );
        _mockSvcCoinsClient.VerifyNoOtherCalls();
        _mockSvcKlineClient.VerifyNoOtherCalls();
    }

    private static class TestData
    {
        public static readonly KlineDataCreationRequest[] ExpectedSingleBitcoinKlineDataCreationRequest =
        [
            new()
            {
                IdTradingPair = 1,
                OpenTime = 1640995200000, // 2022-01-01 00:00:00 UTC
                OpenPrice = 47000m,
                HighPrice = 48000m,
                LowPrice = 46000m,
                ClosePrice = 47500m,
                Volume = 1000m,
                CloseTime = 1641081599999, // 2022-01-01 23:59:59 UTC
            },
            new()
            {
                IdTradingPair = 1,
                OpenTime = 1641081600000, // 2022-01-02 00:00:00 UTC
                OpenPrice = 47500m,
                HighPrice = 49000m,
                LowPrice = 47000m,
                ClosePrice = 48500m,
                Volume = 1200m,
                CloseTime = 1641167999999, // 2022-01-02 23:59:59 UTC
            },
        ];

        public static readonly KlineDataCreationRequest[] ExpectedBitcoinAndEthereumKlineDataCreationRequests =
        [
            new()
            {
                IdTradingPair = 1,
                OpenTime = 1640995200000,
                OpenPrice = 47000m,
                HighPrice = 48000m,
                LowPrice = 46000m,
                ClosePrice = 47500m,
                Volume = 1000m,
                CloseTime = 1641081599999,
            },
            new()
            {
                IdTradingPair = 1,
                OpenTime = 1641081600000,
                OpenPrice = 47500m,
                HighPrice = 49000m,
                LowPrice = 47000m,
                ClosePrice = 48500m,
                Volume = 1200m,
                CloseTime = 1641167999999,
            },
            new()
            {
                IdTradingPair = 2,
                OpenTime = 1640995200000,
                OpenPrice = 3700m,
                HighPrice = 3800m,
                LowPrice = 3600m,
                ClosePrice = 3750m,
                Volume = 500m,
                CloseTime = 1641081599999,
            },
            new()
            {
                IdTradingPair = 2,
                OpenTime = 1641081600000,
                OpenPrice = 3750m,
                HighPrice = 3900m,
                LowPrice = 3700m,
                ClosePrice = 3850m,
                Volume = 600m,
                CloseTime = 1641167999999,
            },
        ];

        public static readonly KlineDataCreationRequest[] ExpectedBitcoinAndCardanoKlineDataCreationRequests =
        [
            new()
            {
                IdTradingPair = 1,
                OpenTime = 1640995200000,
                OpenPrice = 47000m,
                HighPrice = 48000m,
                LowPrice = 46000m,
                ClosePrice = 47500m,
                Volume = 1000m,
                CloseTime = 1641081599999,
            },
            new()
            {
                IdTradingPair = 1,
                OpenTime = 1641081600000,
                OpenPrice = 47500m,
                HighPrice = 49000m,
                LowPrice = 47000m,
                ClosePrice = 48500m,
                Volume = 1200m,
                CloseTime = 1641167999999,
            },
            new()
            {
                IdTradingPair = 3,
                OpenTime = 1640995200000,
                OpenPrice = 1.2m,
                HighPrice = 1.3m,
                LowPrice = 1.1m,
                ClosePrice = 1.25m,
                Volume = 10000m,
                CloseTime = 1641081599999,
            },
            new()
            {
                IdTradingPair = 3,
                OpenTime = 1641081600000,
                OpenPrice = 1.25m,
                HighPrice = 1.35m,
                LowPrice = 1.2m,
                ClosePrice = 1.3m,
                Volume = 12000m,
                CloseTime = 1641167999999,
            },
        ];

        public static readonly IEnumerable<Coin> CoinsWithNoTradingPairs =
        [
            new()
            {
                Id = 1,
                Symbol = "BTC",
                Name = "Bitcoin",
                TradingPairs = [],
            },
            new()
            {
                Id = 2,
                Symbol = "ETH",
                Name = "Ethereum",
                TradingPairs = [],
            },
        ];

        public static readonly IEnumerable<Coin> SingleBitcoinCoinWithTradingPair =
        [
            new()
            {
                Id = 1,
                Symbol = "BTC",
                Name = "Bitcoin",
                TradingPairs =
                [
                    new()
                    {
                        Id = 1,
                        CoinQuote = new()
                        {
                            Id = 10,
                            Symbol = "USDT",
                            Name = "Tether",
                        },
                        Exchanges = [Exchange.Binance],
                    },
                ],
            },
        ];

        public static readonly IEnumerable<Coin> BitcoinAndEthereumCoinsWithTradingPairs =
        [
            new()
            {
                Id = 1,
                Symbol = "BTC",
                Name = "Bitcoin",
                TradingPairs =
                [
                    new()
                    {
                        Id = 1,
                        CoinQuote = new()
                        {
                            Id = 10,
                            Symbol = "USDT",
                            Name = "Tether",
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
                TradingPairs =
                [
                    new()
                    {
                        Id = 2,
                        CoinQuote = new()
                        {
                            Id = 10,
                            Symbol = "USDT",
                            Name = "Tether",
                        },
                        Exchanges = [Exchange.Binance, Exchange.Bybit],
                    },
                ],
            },
        ];

        public static readonly IEnumerable<Coin> MixedCoinsWithAndWithoutTradingPairs =
        [
            new()
            {
                Id = 1,
                Symbol = "BTC",
                Name = "Bitcoin",
                TradingPairs =
                [
                    new()
                    {
                        Id = 1,
                        CoinQuote = new()
                        {
                            Id = 10,
                            Symbol = "USDT",
                            Name = "Tether",
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
                TradingPairs = [], // No trading pairs
            },
            new()
            {
                Id = 3,
                Symbol = "ADA",
                Name = "Cardano",
                TradingPairs =
                [
                    new()
                    {
                        Id = 3,
                        CoinQuote = new()
                        {
                            Id = 10,
                            Symbol = "USDT",
                            Name = "Tether",
                        },
                        Exchanges = [Exchange.Mexc],
                    },
                ],
            },
        ];

        public static readonly IEnumerable<SvcExternal.KlineDataResponse> EmptyKlineDataResponses =
        [];

        public static readonly IEnumerable<SvcExternal.KlineDataResponse> SingleBitcoinKlineDataResponse =
        [
            new()
            {
                IdTradingPair = 1,
                KlineData =
                [
                    new()
                    {
                        OpenTime = 1640995200000,
                        OpenPrice = 47000m,
                        HighPrice = 48000m,
                        LowPrice = 46000m,
                        ClosePrice = 47500m,
                        Volume = 1000m,
                        CloseTime = 1641081599999,
                    },
                    new()
                    {
                        OpenTime = 1641081600000,
                        OpenPrice = 47500m,
                        HighPrice = 49000m,
                        LowPrice = 47000m,
                        ClosePrice = 48500m,
                        Volume = 1200m,
                        CloseTime = 1641167999999,
                    },
                ],
            },
        ];

        public static readonly IEnumerable<SvcExternal.KlineDataResponse> BitcoinAndEthereumKlineDataResponses =
        [
            new()
            {
                IdTradingPair = 1,
                KlineData =
                [
                    new()
                    {
                        OpenTime = 1640995200000,
                        OpenPrice = 47000m,
                        HighPrice = 48000m,
                        LowPrice = 46000m,
                        ClosePrice = 47500m,
                        Volume = 1000m,
                        CloseTime = 1641081599999,
                    },
                    new()
                    {
                        OpenTime = 1641081600000,
                        OpenPrice = 47500m,
                        HighPrice = 49000m,
                        LowPrice = 47000m,
                        ClosePrice = 48500m,
                        Volume = 1200m,
                        CloseTime = 1641167999999,
                    },
                ],
            },
            new()
            {
                IdTradingPair = 2,
                KlineData =
                [
                    new()
                    {
                        OpenTime = 1640995200000,
                        OpenPrice = 3700m,
                        HighPrice = 3800m,
                        LowPrice = 3600m,
                        ClosePrice = 3750m,
                        Volume = 500m,
                        CloseTime = 1641081599999,
                    },
                    new()
                    {
                        OpenTime = 1641081600000,
                        OpenPrice = 3750m,
                        HighPrice = 3900m,
                        LowPrice = 3700m,
                        ClosePrice = 3850m,
                        Volume = 600m,
                        CloseTime = 1641167999999,
                    },
                ],
            },
        ];

        public static readonly IEnumerable<SvcExternal.KlineDataResponse> BitcoinAndCardanoKlineDataResponses =
        [
            new()
            {
                IdTradingPair = 1,
                KlineData =
                [
                    new()
                    {
                        OpenTime = 1640995200000,
                        OpenPrice = 47000m,
                        HighPrice = 48000m,
                        LowPrice = 46000m,
                        ClosePrice = 47500m,
                        Volume = 1000m,
                        CloseTime = 1641081599999,
                    },
                    new()
                    {
                        OpenTime = 1641081600000,
                        OpenPrice = 47500m,
                        HighPrice = 49000m,
                        LowPrice = 47000m,
                        ClosePrice = 48500m,
                        Volume = 1200m,
                        CloseTime = 1641167999999,
                    },
                ],
            },
            new()
            {
                IdTradingPair = 3,
                KlineData =
                [
                    new()
                    {
                        OpenTime = 1640995200000,
                        OpenPrice = 1.2m,
                        HighPrice = 1.3m,
                        LowPrice = 1.1m,
                        ClosePrice = 1.25m,
                        Volume = 10000m,
                        CloseTime = 1641081599999,
                    },
                    new()
                    {
                        OpenTime = 1641081600000,
                        OpenPrice = 1.25m,
                        HighPrice = 1.35m,
                        LowPrice = 1.2m,
                        ClosePrice = 1.3m,
                        Volume = 12000m,
                        CloseTime = 1641167999999,
                    },
                ],
            },
        ];

        public static readonly IEnumerable<SvcKline.KlineDataResponse> ReplacedBitcoinAndEthereumKlineDataResponses =
        [
            new()
            {
                IdTradingPair = 1,
                KlineData =
                [
                    new()
                    {
                        OpenTime = 1640995200000,
                        OpenPrice = 47000m,
                        HighPrice = 48000m,
                        LowPrice = 46000m,
                        ClosePrice = 47500m,
                        Volume = 1000m,
                        CloseTime = 1641081599999,
                    },
                    new()
                    {
                        OpenTime = 1641081600000,
                        OpenPrice = 47500m,
                        HighPrice = 49000m,
                        LowPrice = 47000m,
                        ClosePrice = 48500m,
                        Volume = 1200m,
                        CloseTime = 1641167999999,
                    },
                ],
            },
            new()
            {
                IdTradingPair = 2,
                KlineData =
                [
                    new()
                    {
                        OpenTime = 1640995200000,
                        OpenPrice = 3700m,
                        HighPrice = 3800m,
                        LowPrice = 3600m,
                        ClosePrice = 3750m,
                        Volume = 500m,
                        CloseTime = 1641081599999,
                    },
                    new()
                    {
                        OpenTime = 1641081600000,
                        OpenPrice = 3750m,
                        HighPrice = 3900m,
                        LowPrice = 3700m,
                        ClosePrice = 3850m,
                        Volume = 600m,
                        CloseTime = 1641167999999,
                    },
                ],
            },
        ];

        public static readonly IEnumerable<SvcKline.KlineDataResponse> ReplacedBitcoinAndCardanoKlineDataResponses =
        [
            new()
            {
                IdTradingPair = 1,
                KlineData =
                [
                    new()
                    {
                        OpenTime = 1640995200000,
                        OpenPrice = 47000m,
                        HighPrice = 48000m,
                        LowPrice = 46000m,
                        ClosePrice = 47500m,
                        Volume = 1000m,
                        CloseTime = 1641081599999,
                    },
                    new()
                    {
                        OpenTime = 1641081600000,
                        OpenPrice = 47500m,
                        HighPrice = 49000m,
                        LowPrice = 47000m,
                        ClosePrice = 48500m,
                        Volume = 1200m,
                        CloseTime = 1641167999999,
                    },
                ],
            },
            new()
            {
                IdTradingPair = 3,
                KlineData =
                [
                    new()
                    {
                        OpenTime = 1640995200000,
                        OpenPrice = 1.2m,
                        HighPrice = 1.3m,
                        LowPrice = 1.1m,
                        ClosePrice = 1.25m,
                        Volume = 10000m,
                        CloseTime = 1641081599999,
                    },
                    new()
                    {
                        OpenTime = 1641081600000,
                        OpenPrice = 1.25m,
                        HighPrice = 1.35m,
                        LowPrice = 1.2m,
                        ClosePrice = 1.3m,
                        Volume = 12000m,
                        CloseTime = 1641167999999,
                    },
                ],
            },
        ];
    }
}
