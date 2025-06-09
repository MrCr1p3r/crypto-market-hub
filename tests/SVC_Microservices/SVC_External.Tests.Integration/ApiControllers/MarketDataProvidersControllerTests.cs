using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SharedLibrary.Extensions.Testing;
using SVC_External.ApiContracts.Responses.MarketDataProviders;
using SVC_External.Tests.Integration.Factories;
using WireMock.Admin.Mappings;

namespace SVC_External.Tests.Integration.ApiControllers;

public class MarketDataProvidersControllerTests(CustomWebApplicationFactory factory)
    : BaseIntegrationTest(factory),
        IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task GetCoinGeckoAssetsInfo_ShouldReturnOkWithData()
    {
        // Arrange
        var coinIds = new List<string> { "bitcoin", "ethereum", "tether", "usd-coin" };
        await Factory.CoinGeckoServerMock.PostMappingsAsync(
            [WireMockMappings.CoinGecko.Markets, WireMockMappings.CoinGecko.MarketsStablecoins]
        );

        // Act
        var response = await Client.PostAsJsonAsync(
            "/market-data-providers/coingecko/assets-info/query",
            coinIds
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<CoinGeckoAssetInfo>>();

        result.Should().NotBeNull();
        result!.Should().HaveCount(4);

        // Check each coin is present
        result.Should().Contain(c => c.Id == "bitcoin");
        result.Should().Contain(c => c.Id == "ethereum");
        result.Should().Contain(c => c.Id == "tether");
        result.Should().Contain(c => c.Id == "usd-coin");

        // Verify stablecoin flags
        var btcAsset = result!.FirstOrDefault(c => c.Id == "bitcoin");
        var ethAsset = result!.FirstOrDefault(c => c.Id == "ethereum");
        var tetherAsset = result!.FirstOrDefault(c => c.Id == "tether");
        var usdcAsset = result!.FirstOrDefault(c => c.Id == "usd-coin");

        btcAsset.Should().NotBeNull();
        ethAsset.Should().NotBeNull();
        tetherAsset.Should().NotBeNull();
        usdcAsset.Should().NotBeNull();

        btcAsset!.IsStablecoin.Should().BeFalse();
        ethAsset!.IsStablecoin.Should().BeFalse();
        tetherAsset!.IsStablecoin.Should().BeTrue();
        usdcAsset!.IsStablecoin.Should().BeTrue();
    }

    [Fact]
    public async Task GetCoinGeckoAssetsInfo_CoinGeckoReturnsError_ShouldReturnErrorResponse()
    {
        // Arrange
        await Factory.CoinGeckoServerMock.PostMappingAsync(
            WireMockMappings.CoinGecko.MarketsStatus503
        );

        var ids = new List<string> { "unknown-coin-id", "unknown-coin-id-2" };

        // Act
        var response = await Client.PostAsJsonAsync(
            "/market-data-providers/coingecko/assets-info/query",
            ids
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GetCoinGeckoAssetsInfo_WithNoIds_ShouldReturnBadRequest()
    {
        // Act
        var response = await Client.PostAsJsonAsync(
            "/market-data-providers/coingecko/assets-info/query",
            new List<string>()
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static class WireMockMappings
    {
        public static class CoinGecko
        {
            public static MappingModel Markets =>
                new()
                {
                    Request = new RequestModel
                    {
                        Methods = ["GET"],
                        Path = "/api/v3/coins/markets",
                        Params = [WireMockParamBuilder.WithNotNullOrEmptyMatch("ids")],
                    },
                    Response = new ResponseModel
                    {
                        StatusCode = 200,
                        Headers = new Dictionary<string, object>
                        {
                            ["Content-Type"] = "application/json",
                        },
                        Body = JsonSerializer.Serialize(TestData.CoinGeckoMarketData),
                    },
                };

            public static MappingModel MarketsStablecoins =>
                new()
                {
                    Request = new RequestModel
                    {
                        Methods = ["GET"],
                        Path = "/api/v3/coins/markets",
                        Params = [WireMockParamBuilder.WithExactMatch("category", "stablecoins")],
                    },
                    Response = new ResponseModel
                    {
                        StatusCode = 200,
                        Headers = new Dictionary<string, object>
                        {
                            ["Content-Type"] = "application/json",
                        },
                        Body = JsonSerializer.Serialize(TestData.CoinGeckoStablecoins),
                    },
                };

            public static MappingModel MarketsStatus503 =>
                new()
                {
                    Request = new RequestModel
                    {
                        Methods = ["GET"],
                        Path = "/api/v3/coins/markets",
                    },
                    Response = new ResponseModel { StatusCode = 503 },
                };
        }
    }

    private static class TestData
    {
        public static readonly List<dynamic> CoinGeckoMarketData =
        [
            new
            {
                id = "bitcoin",
                symbol = "btc",
                name = "Bitcoin",
                current_price = 50000.00m,
                market_cap = 950000000000.00m,
                price_change_percentage_24h = 2.5m,
            },
            new
            {
                id = "ethereum",
                symbol = "eth",
                name = "Ethereum",
                current_price = 2500.00m,
                market_cap = 300000000000.00m,
                price_change_percentage_24h = 1.2m,
            },
            new
            {
                id = "tether",
                symbol = "usdt",
                name = "Tether",
                current_price = 1.00m,
                market_cap = 80000000000.00m,
                price_change_percentage_24h = 0.05m,
            },
            new
            {
                id = "usd-coin",
                symbol = "usdc",
                name = "USD Coin",
                current_price = 1.00m,
                market_cap = 50000000000.00m,
                price_change_percentage_24h = 0.01m,
            },
        ];

        public static readonly List<dynamic> CoinGeckoStablecoins =
        [
            new
            {
                id = "tether",
                symbol = "usdt",
                name = "Tether",
                current_price = 1.00m,
                market_cap = 80000000000.00m,
                price_change_percentage_24h = 0.05m,
            },
            new
            {
                id = "usd-coin",
                symbol = "usdc",
                name = "USD Coin",
                current_price = 1.00m,
                market_cap = 50000000000.00m,
                price_change_percentage_24h = 0.01m,
            },
            new
            {
                id = "dai",
                symbol = "dai",
                name = "Dai",
                current_price = 1.00m,
                market_cap = 8000000000.00m,
                price_change_percentage_24h = 0.02m,
            },
        ];
    }
}
