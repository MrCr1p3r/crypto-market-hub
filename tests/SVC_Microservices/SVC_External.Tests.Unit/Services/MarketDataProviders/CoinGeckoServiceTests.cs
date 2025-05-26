using FluentResults;
using SVC_External.ExternalClients.MarketDataProviders.CoinGecko;
using SVC_External.ExternalClients.MarketDataProviders.CoinGecko.Contracts.Responses;
using SVC_External.Services.MarketDataProviders;

namespace SVC_External.Tests.Unit.Services.MarketDataProviders;

public class CoinGeckoServiceTests
{
    private readonly IFixture _fixture;
    private readonly Mock<ICoinGeckoClient> _coinGeckoClientMock;
    private readonly CoinGeckoService _coinGeckoService;

    public CoinGeckoServiceTests()
    {
        _fixture = new Fixture();
        _coinGeckoClientMock = new Mock<ICoinGeckoClient>();

        _coinGeckoService = new CoinGeckoService(_coinGeckoClientMock.Object);
    }

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
        var result = await _coinGeckoService.GetCoinGeckoAssetsInfo(coinIds);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);

        // Verify each returned item has correct properties
        result.Value.Should().ContainSingle(coin => coin.Id == "bitcoin");
        result.Value.Should().ContainSingle(coin => coin.Id == "ethereum");
        result.Value.Should().ContainSingle(coin => coin.Id == "tether");

        // Verify stablecoin flag is correctly set
        result.Value.Should().Contain(coin => coin.Id == "bitcoin" && !coin.IsStablecoin);
        result.Value.Should().Contain(coin => coin.Id == "ethereum" && !coin.IsStablecoin);
        result.Value.Should().Contain(coin => coin.Id == "tether" && coin.IsStablecoin);

        // Verify other properties are mapped correctly
        foreach (var item in result.Value)
        {
            var sourceAsset = assetInfos.First(asset => asset.Id == item.Id);
            item.MarketCapUsd.Should().Be(Convert.ToInt64(sourceAsset.MarketCapUsd));
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
        var result = await _coinGeckoService.GetCoinGeckoAssetsInfo(coinIds);

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
        var result = await _coinGeckoService.GetCoinGeckoAssetsInfo(coinIds);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result
            .Errors[0]
            .Reasons.Any(reason => reason.Message.Contains(expectedErrorMessage))
            .Should()
            .BeTrue();
    }
}
