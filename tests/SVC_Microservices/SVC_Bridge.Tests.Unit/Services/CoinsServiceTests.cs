using FluentResults;
using SharedLibrary.Errors;
using SVC_Bridge.MicroserviceClients.SvcCoins;
using SVC_Bridge.MicroserviceClients.SvcCoins.Contracts.Requests;
using SVC_Bridge.MicroserviceClients.SvcCoins.Contracts.Responses;
using SVC_Bridge.MicroserviceClients.SvcExternal;
using SVC_Bridge.MicroserviceClients.SvcExternal.Contracts.Responses;
using SVC_Bridge.Services;

namespace SVC_Bridge.Tests.Unit.Services;

public class CoinsServiceTests
{
    private readonly Mock<ISvcCoinsClient> _mockSvcCoinsClient;
    private readonly Mock<ISvcExternalClient> _mockSvcExternalClient;
    private readonly CoinsService _coinsService;

    public CoinsServiceTests()
    {
        _mockSvcCoinsClient = new Mock<ISvcCoinsClient>();
        _mockSvcExternalClient = new Mock<ISvcExternalClient>();
        _coinsService = new CoinsService(_mockSvcCoinsClient.Object, _mockSvcExternalClient.Object);
    }

    [Fact]
    public async Task UpdateCoinsMarketData_WhenSuccessfulFlow_ShouldReturnUpdatedCoinMarketData()
    {
        // Arrange
        var coinId1 = 1;
        var coinId2 = 2;

        _mockSvcCoinsClient
            .Setup(client => client.GetAllCoins())
            .ReturnsAsync(TestData.BitcoinAndEthereumCoins);

        _mockSvcExternalClient
            .Setup(client => client.GetCoinGeckoAssetsInfo(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(Result.Ok(TestData.BitcoinAndEthereumMarketData));

        _mockSvcCoinsClient
            .Setup(client =>
                client.UpdateCoinsMarketData(It.IsAny<IEnumerable<CoinMarketDataUpdateRequest>>())
            )
            .ReturnsAsync(Result.Ok(TestData.UpdatedBitcoinAndEthereumCoins));

        // Act
        var result = await _coinsService.UpdateCoinsMarketData();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);

        var coinMarketDataList = result.Value.ToList();

        coinMarketDataList[0].Id.Should().Be(coinId1);
        coinMarketDataList[0].MarketCapUsd.Should().Be(1000000);
        coinMarketDataList[0].PriceUsd.Should().Be("50000");
        coinMarketDataList[0].PriceChangePercentage24h.Should().Be(2.5m);

        coinMarketDataList[1].Id.Should().Be(coinId2);
        coinMarketDataList[1].MarketCapUsd.Should().Be(500000);
        coinMarketDataList[1].PriceUsd.Should().Be("3000");
        coinMarketDataList[1].PriceChangePercentage24h.Should().Be(-1.2m);

        // Verify client calls
        _mockSvcCoinsClient.Verify(client => client.GetAllCoins(), Times.Once);
        _mockSvcExternalClient.Verify(
            client =>
                client.GetCoinGeckoAssetsInfo(TestData.ExpectedBitcoinAndEthereumCoinGeckoIds),
            Times.Once
        );
        _mockSvcCoinsClient.Verify(
            client =>
                client.UpdateCoinsMarketData(TestData.ExpectedBitcoinAndEthereumUpdateRequests),
            Times.Once
        );
        _mockSvcCoinsClient.VerifyNoOtherCalls();
        _mockSvcExternalClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateCoinsMarketData_WhenNoCoinsExist_ShouldReturnEmptyResult()
    {
        // Arrange
        _mockSvcCoinsClient.Setup(client => client.GetAllCoins()).ReturnsAsync([]);

        // Act
        var result = await _coinsService.UpdateCoinsMarketData();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();

        _mockSvcCoinsClient.Verify(client => client.GetAllCoins(), Times.Once);
        _mockSvcCoinsClient.VerifyNoOtherCalls();
        _mockSvcExternalClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateCoinsMarketData_WhenNoCoinsHaveCoinGeckoIds_ShouldReturnEmptyResult()
    {
        // Arrange
        _mockSvcCoinsClient
            .Setup(client => client.GetAllCoins())
            .ReturnsAsync(TestData.CoinsWithNoCoinGeckoIds);

        // Act
        var result = await _coinsService.UpdateCoinsMarketData();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();

        // Verify only GetAllCoins was called, no other calls should be made
        _mockSvcCoinsClient.Verify(client => client.GetAllCoins(), Times.Once);
        _mockSvcCoinsClient.VerifyNoOtherCalls();
        _mockSvcExternalClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateCoinsMarketData_WhenExternalServiceFails_ShouldReturnFailureResult()
    {
        // Arrange
        var externalServiceError = new GenericErrors.InternalError("External service unavailable");

        _mockSvcCoinsClient
            .Setup(client => client.GetAllCoins())
            .ReturnsAsync(TestData.SingleBitcoinCoin);

        _mockSvcExternalClient
            .Setup(client => client.GetCoinGeckoAssetsInfo(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(Result.Fail(externalServiceError));

        // Act
        var result = await _coinsService.UpdateCoinsMarketData();

        // Assert
        result.IsFailed.Should().BeTrue();
        result
            .Errors.Should()
            .Contain(error => error.Message.Contains("Failed to retrieve CoinGecko assets info"));

        // Check that the original error is nested as a reason within the InternalError
        var internalError = result.Errors.OfType<GenericErrors.InternalError>().First();
        internalError.Reasons.Should().Contain(externalServiceError);

        // Verify client calls - should stop after external service fails
        _mockSvcCoinsClient.Verify(client => client.GetAllCoins(), Times.Once);
        _mockSvcExternalClient.Verify(
            client => client.GetCoinGeckoAssetsInfo(TestData.ExpectedSingleBitcoinCoinGeckoIds),
            Times.Once
        );

        _mockSvcCoinsClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateCoinsMarketData_WhenUpdateCoinsMarketDataFails_ShouldReturnFailureResult()
    {
        // Arrange
        var updateError = new GenericErrors.InternalError("Database update failed");

        _mockSvcCoinsClient
            .Setup(client => client.GetAllCoins())
            .ReturnsAsync(TestData.SingleBitcoinCoin);

        _mockSvcExternalClient
            .Setup(client => client.GetCoinGeckoAssetsInfo(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(Result.Ok(TestData.BitcoinMarketData));

        _mockSvcCoinsClient
            .Setup(client =>
                client.UpdateCoinsMarketData(It.IsAny<IEnumerable<CoinMarketDataUpdateRequest>>())
            )
            .ReturnsAsync(Result.Fail(updateError));

        // Act
        var result = await _coinsService.UpdateCoinsMarketData();

        // Assert
        result.IsFailed.Should().BeTrue();
        result
            .Errors.Should()
            .Contain(error => error.Message.Contains("Failed to update coins market data"));

        // Check that the original error is nested as a reason within the InternalError
        var internalError = result.Errors.OfType<GenericErrors.InternalError>().First();
        internalError.Reasons.Should().Contain(updateError);

        // Verify all client calls were made up to the failure point
        _mockSvcCoinsClient.Verify(client => client.GetAllCoins(), Times.Once);
        _mockSvcExternalClient.Verify(
            client => client.GetCoinGeckoAssetsInfo(TestData.ExpectedSingleBitcoinCoinGeckoIds),
            Times.Once
        );
        _mockSvcCoinsClient.Verify(
            client => client.UpdateCoinsMarketData(TestData.ExpectedSingleBitcoinUpdateRequest),
            Times.Once
        );
    }

    [Fact]
    public async Task UpdateCoinsMarketData_WhenMixedCoinsWithAndWithoutCoinGeckoIds_ShouldProcessOnlyValidOnes()
    {
        // Arrange
        var coinId1 = 1;
        var coinId2 = 2;
        var coinId3 = 3;

        _mockSvcCoinsClient
            .Setup(client => client.GetAllCoins())
            .ReturnsAsync(TestData.MixedCoinsWithAndWithoutCoinGeckoIds);

        _mockSvcExternalClient
            .Setup(client => client.GetCoinGeckoAssetsInfo(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(Result.Ok(TestData.BitcoinAndCardanoMarketData));

        _mockSvcCoinsClient
            .Setup(client =>
                client.UpdateCoinsMarketData(It.IsAny<IEnumerable<CoinMarketDataUpdateRequest>>())
            )
            .ReturnsAsync(Result.Ok(TestData.UpdatedBitcoinAndCardanoCoins));

        // Act
        var result = await _coinsService.UpdateCoinsMarketData();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(coinData => coinData.Id == coinId1);
        result.Value.Should().Contain(coinData => coinData.Id == coinId3);
        result.Value.Should().NotContain(coinData => coinData.Id == coinId2);

        // Verify client calls - only coins with CoinGecko IDs should be processed
        _mockSvcCoinsClient.Verify(client => client.GetAllCoins(), Times.Once);
        _mockSvcExternalClient.Verify(
            client => client.GetCoinGeckoAssetsInfo(TestData.ExpectedBitcoinAndCardanoCoinGeckoIds),
            Times.Once
        );
        _mockSvcCoinsClient.Verify(
            client =>
                client.UpdateCoinsMarketData(TestData.ExpectedBitcoinAndCardanoUpdateRequests),
            Times.Once
        );
    }

    [Fact]
    public async Task UpdateCoinsMarketData_WhenPartialMarketDataAvailable_ShouldProcessOnlyMatchingCoins()
    {
        // Arrange
        var coinId1 = 1;

        _mockSvcCoinsClient
            .Setup(client => client.GetAllCoins())
            .ReturnsAsync(TestData.BitcoinAndUnknownCoins);

        _mockSvcExternalClient
            .Setup(client => client.GetCoinGeckoAssetsInfo(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(Result.Ok(TestData.BitcoinMarketData));

        _mockSvcCoinsClient
            .Setup(client =>
                client.UpdateCoinsMarketData(It.IsAny<IEnumerable<CoinMarketDataUpdateRequest>>())
            )
            .ReturnsAsync(Result.Ok(TestData.UpdatedBitcoinCoin));

        // Act
        var result = await _coinsService.UpdateCoinsMarketData();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value.First().Id.Should().Be(coinId1);

        // Verify client calls - only matching coins should be processed
        _mockSvcCoinsClient.Verify(client => client.GetAllCoins(), Times.Once);
        _mockSvcExternalClient.Verify(
            client => client.GetCoinGeckoAssetsInfo(TestData.ExpectedBitcoinAndUnknownCoinGeckoIds),
            Times.Once
        );
        _mockSvcCoinsClient.Verify(
            client => client.UpdateCoinsMarketData(TestData.ExpectedSingleBitcoinUpdateRequest),
            Times.Once
        );
    }

    [Fact]
    public async Task UpdateCoinsMarketData_WhenNoMarketDataMatches_ShouldReturnEmptyResult()
    {
        // Arrange
        var unknownCoins = TestData.UnknownCoins;
        var coinGeckoIds = unknownCoins.Select(coin => coin.IdCoinGecko!);

        _mockSvcCoinsClient.Setup(client => client.GetAllCoins()).ReturnsAsync(unknownCoins);

        _mockSvcExternalClient
            .Setup(client => client.GetCoinGeckoAssetsInfo(coinGeckoIds))
            .ReturnsAsync(Result.Ok(TestData.DifferentCoinMarketData));

        // Act
        var result = await _coinsService.UpdateCoinsMarketData();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();

        // Verify client calls - should get market data but no updates due to no matches
        _mockSvcCoinsClient.Verify(client => client.GetAllCoins(), Times.Once);
        _mockSvcCoinsClient.VerifyNoOtherCalls();
        _mockSvcExternalClient.Verify(
            client => client.GetCoinGeckoAssetsInfo(TestData.ExpectedUnknownCoinGeckoIds),
            Times.Once
        );
    }

    private static class TestData
    {
        public static readonly string[] ExpectedBitcoinAndEthereumCoinGeckoIds =
        [
            "bitcoin",
            "ethereum",
        ];

        public static readonly string[] ExpectedBitcoinAndCardanoCoinGeckoIds =
        [
            "bitcoin",
            "cardano",
        ];

        public static readonly string[] ExpectedSingleBitcoinCoinGeckoIds = ["bitcoin"];

        public static readonly string[] ExpectedBitcoinAndUnknownCoinGeckoIds =
        [
            "bitcoin",
            "unknown-coin",
        ];

        public static readonly string[] ExpectedUnknownCoinGeckoIds =
        [
            "unknown-coin-1",
            "unknown-coin-2",
        ];

        public static readonly CoinMarketDataUpdateRequest[] ExpectedBitcoinAndEthereumUpdateRequests =
        [
            new()
            {
                Id = 1,
                MarketCapUsd = 1000000L,
                PriceUsd = 50000m,
                PriceChangePercentage24h = 2.5m,
            },
            new()
            {
                Id = 2,
                MarketCapUsd = 500000L,
                PriceUsd = 3000m,
                PriceChangePercentage24h = -1.2m,
            },
        ];

        public static readonly CoinMarketDataUpdateRequest[] ExpectedSingleBitcoinUpdateRequest =
        [
            new()
            {
                Id = 1,
                MarketCapUsd = 1000000L,
                PriceUsd = 50000m,
                PriceChangePercentage24h = 2.5m,
            },
        ];

        public static readonly CoinMarketDataUpdateRequest[] ExpectedBitcoinAndCardanoUpdateRequests =
        [
            new()
            {
                Id = 1,
                MarketCapUsd = 1000000L,
                PriceUsd = 50000m,
                PriceChangePercentage24h = 2.5m,
            },
            new()
            {
                Id = 3,
                MarketCapUsd = 100000L,
                PriceUsd = 1m,
                PriceChangePercentage24h = 5.0m,
            },
        ];

        public static readonly IEnumerable<Coin> CoinsWithNoCoinGeckoIds =
        [
            new()
            {
                Id = 1,
                Symbol = "BTC",
                Name = "Bitcoin",
                IdCoinGecko = null,
            },
            new()
            {
                Id = 2,
                Symbol = "ETH",
                Name = "Ethereum",
                IdCoinGecko = string.Empty,
            },
            new()
            {
                Id = 3,
                Symbol = "ADA",
                Name = "Cardano",
                IdCoinGecko = "   ",
            },
        ];

        public static readonly IEnumerable<Coin> SingleBitcoinCoin =
        [
            new()
            {
                Id = 1,
                Symbol = "BTC",
                Name = "Bitcoin",
                IdCoinGecko = "bitcoin",
            },
        ];

        public static readonly IEnumerable<Coin> BitcoinAndEthereumCoins =
        [
            new()
            {
                Id = 1,
                Symbol = "BTC",
                Name = "Bitcoin",
                IdCoinGecko = "bitcoin",
            },
            new()
            {
                Id = 2,
                Symbol = "ETH",
                Name = "Ethereum",
                IdCoinGecko = "ethereum",
            },
        ];

        public static readonly IEnumerable<Coin> MixedCoinsWithAndWithoutCoinGeckoIds =
        [
            new()
            {
                Id = 1,
                Symbol = "BTC",
                Name = "Bitcoin",
                IdCoinGecko = "bitcoin",
            },
            new()
            {
                Id = 2,
                Symbol = "ETH",
                Name = "Ethereum",
                IdCoinGecko = null,
            },
            new()
            {
                Id = 3,
                Symbol = "ADA",
                Name = "Cardano",
                IdCoinGecko = "cardano",
            },
        ];

        public static readonly IEnumerable<Coin> BitcoinAndUnknownCoins =
        [
            new()
            {
                Id = 1,
                Symbol = "BTC",
                Name = "Bitcoin",
                IdCoinGecko = "bitcoin",
            },
            new()
            {
                Id = 2,
                Symbol = "UNKNOWN",
                Name = "Unknown Coin",
                IdCoinGecko = "unknown-coin",
            },
        ];

        public static readonly IEnumerable<Coin> UnknownCoins =
        [
            new()
            {
                Id = 1,
                Symbol = "UNKNOWN1",
                Name = "Unknown Coin 1",
                IdCoinGecko = "unknown-coin-1",
            },
            new()
            {
                Id = 2,
                Symbol = "UNKNOWN2",
                Name = "Unknown Coin 2",
                IdCoinGecko = "unknown-coin-2",
            },
        ];

        public static readonly IEnumerable<CoinGeckoAssetInfo> BitcoinMarketData =
        [
            new()
            {
                Id = "bitcoin",
                MarketCapUsd = 1000000L,
                PriceUsd = 50000m,
                PriceChangePercentage24h = 2.5m,
            },
        ];

        public static readonly IEnumerable<CoinGeckoAssetInfo> BitcoinAndEthereumMarketData =
        [
            new()
            {
                Id = "bitcoin",
                MarketCapUsd = 1000000L,
                PriceUsd = 50000m,
                PriceChangePercentage24h = 2.5m,
            },
            new()
            {
                Id = "ethereum",
                MarketCapUsd = 500000L,
                PriceUsd = 3000m,
                PriceChangePercentage24h = -1.2m,
            },
        ];

        public static readonly IEnumerable<CoinGeckoAssetInfo> BitcoinAndCardanoMarketData =
        [
            new()
            {
                Id = "bitcoin",
                MarketCapUsd = 1000000L,
                PriceUsd = 50000m,
                PriceChangePercentage24h = 2.5m,
            },
            new()
            {
                Id = "cardano",
                MarketCapUsd = 100000L,
                PriceUsd = 1m,
                PriceChangePercentage24h = 5.0m,
            },
        ];

        public static readonly IEnumerable<CoinGeckoAssetInfo> DifferentCoinMarketData =
        [
            new()
            {
                Id = "different-coin",
                MarketCapUsd = 1000000L,
                PriceUsd = 50000m,
                PriceChangePercentage24h = 2.5m,
            },
        ];

        public static readonly IEnumerable<Coin> UpdatedBitcoinCoin =
        [
            new()
            {
                Id = 1,
                Symbol = "BTC",
                Name = "Bitcoin",
                IdCoinGecko = "bitcoin",
                MarketCapUsd = 1000000,
                PriceUsd = "50000",
                PriceChangePercentage24h = 2.5m,
            },
        ];

        public static readonly IEnumerable<Coin> UpdatedBitcoinAndEthereumCoins =
        [
            new()
            {
                Id = 1,
                Symbol = "BTC",
                Name = "Bitcoin",
                IdCoinGecko = "bitcoin",
                MarketCapUsd = 1000000,
                PriceUsd = "50000",
                PriceChangePercentage24h = 2.5m,
            },
            new()
            {
                Id = 2,
                Symbol = "ETH",
                Name = "Ethereum",
                IdCoinGecko = "ethereum",
                MarketCapUsd = 500000,
                PriceUsd = "3000",
                PriceChangePercentage24h = -1.2m,
            },
        ];

        public static readonly IEnumerable<Coin> UpdatedBitcoinAndCardanoCoins =
        [
            new()
            {
                Id = 1,
                Symbol = "BTC",
                Name = "Bitcoin",
                IdCoinGecko = "bitcoin",
                MarketCapUsd = 1000000,
                PriceUsd = "50000",
                PriceChangePercentage24h = 2.5m,
            },
            new()
            {
                Id = 3,
                Symbol = "ADA",
                Name = "Cardano",
                IdCoinGecko = "cardano",
                MarketCapUsd = 100000,
                PriceUsd = "1",
                PriceChangePercentage24h = 5.0m,
            },
        ];
    }
}
