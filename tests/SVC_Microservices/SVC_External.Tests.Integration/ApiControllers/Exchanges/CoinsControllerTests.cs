using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using SharedLibrary.Extensions.Testing;
using SVC_External.ApiContracts.Responses.Exchanges.Coins;
using SVC_External.Tests.Integration.Factories;
using WireMock.Admin.Mappings;

namespace SVC_External.Tests.Integration.ApiControllers.Exchanges;

public class CoinsControllerTests(CustomWebApplicationFactory factory)
    : BaseIntegrationTest(factory),
        IClassFixture<CustomWebApplicationFactory>
{
    private const string CacheKey = "all_current_active_spot_coins";

    // Setup method to clear specific cache keys before each test
    private async Task ClearCacheAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var cache =
            scope.ServiceProvider.GetRequiredService<ZiggyCreatures.Caching.Fusion.IFusionCache>();
        await cache.RemoveAsync(CacheKey);
    }

    [Fact]
    public async Task GetAllSpotCoins_ShouldReturnOkWithData()
    {
        // Clear cache to ensure clean test
        await ClearCacheAsync();

        // Arrange
        var binanceTask = Factory.BinanceServerMock.PostMappingAsync(
            WireMockMappings.Binance.ExchangeInfo
        );
        var bybitTask = Factory.BybitServerMock.PostMappingAsync(
            WireMockMappings.Bybit.InstrumentsInfo
        );
        var mexcTask = Factory.MexcServerMock.PostMappingAsync(WireMockMappings.Mexc.ExchangeInfo);
        var coinGeckoTask = Factory.CoinGeckoServerMock.PostMappingsAsync(
            WireMockMappings.CoinGecko.AllMappings
        );
        await Task.WhenAll(binanceTask, bybitTask, mexcTask, coinGeckoTask);

        // Act
        var response = await Client.GetAsync("/exchanges/coins/spot");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<Coin>>();

        result.Should().NotBeNull();
        result.Should().HaveCountGreaterThanOrEqualTo(4); // At least 4 unique coins from the 3 exchanges

        // Verify some specific coins are present
        result.Should().Contain(c => c.Symbol == "BTC");
        result.Should().Contain(c => c.Symbol == "ETH");
        result.Should().Contain(c => c.Symbol == "ADA");
        result.Should().Contain(c => c.Symbol == "SOL");
    }

    [Fact]
    public async Task GetAllSpotCoins_ExchangesUnavailable_ShouldReturnError()
    {
        // Clear cache to ensure clean test
        await ClearCacheAsync();

        // Act
        var response = await Client.GetAsync("/exchanges/coins/spot");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    private static class WireMockMappings
    {
        public static class Binance
        {
            public static MappingModel ExchangeInfo =>
                new()
                {
                    Request = new RequestModel { Methods = ["GET"], Path = "/api/v3/exchangeInfo" },
                    Response = new ResponseModel
                    {
                        StatusCode = 200,
                        Body = JsonSerializer.Serialize(new { symbols = TestData.BinanceCoins }),
                    },
                };
        }

        public static class Bybit
        {
            public static MappingModel InstrumentsInfo =>
                new()
                {
                    Request = new RequestModel
                    {
                        Methods = ["GET"],
                        Path = "/v5/market/instruments-info",
                        Params = [WireMockParamBuilder.WithExactMatch("category", "spot")],
                    },
                    Response = new ResponseModel
                    {
                        StatusCode = 200,
                        Headers = new Dictionary<string, object>
                        {
                            ["Content-Type"] = "application/json",
                        },
                        BodyAsJson = new { result = new { list = TestData.BybitCoins } },
                    },
                };
        }

        public static class Mexc
        {
            public static MappingModel ExchangeInfo =>
                new()
                {
                    Request = new RequestModel { Methods = ["GET"], Path = "/api/v3/exchangeInfo" },
                    Response = new ResponseModel
                    {
                        StatusCode = 200,
                        Headers = new Dictionary<string, object>
                        {
                            ["Content-Type"] = "application/json",
                        },
                        BodyAsJson = new { symbols = TestData.MexcCoins },
                    },
                };
        }

        public static class CoinGecko
        {
            public static MappingModel[] AllMappings =>
                [CoinsList, BinanceTickers, BybitTickers, MexcTickers];

            public static MappingModel CoinsList =>
                new()
                {
                    Request = new RequestModel { Methods = ["GET"], Path = "/api/v3/coins/list" },
                    Response = new ResponseModel
                    {
                        StatusCode = 200,
                        Headers = new Dictionary<string, object>
                        {
                            ["Content-Type"] = "application/json",
                        },
                        BodyAsJson = TestData.CoinGeckoCoins,
                    },
                };

            public static MappingModel BinanceTickers =>
                new()
                {
                    Request = new RequestModel
                    {
                        Methods = ["GET"],
                        Path = "/api/v3/exchanges/binance/tickers",
                        Params = CreateTickerParams(),
                    },
                    Response = new ResponseModel
                    {
                        StatusCode = 200,
                        Headers = new Dictionary<string, object>
                        {
                            ["Content-Type"] = "application/json",
                        },
                        BodyAsJson = TestData.BinanceTickers,
                    },
                };

            public static MappingModel BybitTickers =>
                new()
                {
                    Request = new RequestModel
                    {
                        Methods = ["GET"],
                        Path = "/api/v3/exchanges/bybit_spot/tickers",
                        Params = CreateTickerParams(),
                    },
                    Response = new ResponseModel
                    {
                        StatusCode = 200,
                        Headers = new Dictionary<string, object>
                        {
                            ["Content-Type"] = "application/json",
                        },
                        BodyAsJson = TestData.BybitTickers,
                    },
                };

            public static MappingModel MexcTickers =>
                new()
                {
                    Request = new RequestModel
                    {
                        Methods = ["GET"],
                        Path = "/api/v3/exchanges/mxc/tickers",
                        Params = CreateTickerParams(),
                    },
                    Response = new ResponseModel
                    {
                        StatusCode = 200,
                        Headers = new Dictionary<string, object>
                        {
                            ["Content-Type"] = "application/json",
                        },
                        BodyAsJson = TestData.MexcTickers,
                    },
                };

            private static ParamModel[] CreateTickerParams() =>
                [
                    WireMockParamBuilder.WithExactMatch("depth", "false"),
                    WireMockParamBuilder.WithExactMatch("order", "volume_desc"),
                    WireMockParamBuilder.WithExactMatch("page", "1"),
                ];
        }
    }

    private static class TestData
    {
        public static readonly List<object> BinanceCoins =
        [
            new
            {
                symbol = "BTCUSDT",
                baseAsset = "BTC",
                quoteAsset = "USDT",
                status = "TRADING",
            },
            new
            {
                symbol = "ETHUSDT",
                baseAsset = "ETH",
                quoteAsset = "USDT",
                status = "TRADING",
            },
        ];

        public static readonly List<object> BybitCoins =
        [
            new
            {
                symbol = "BTCUSDT",
                baseCoin = "BTC",
                quoteCoin = "USDT",
                status = "Trading",
            },
            new
            {
                symbol = "ADAUSDT",
                baseCoin = "ADA",
                quoteCoin = "USDT",
                status = "Trading",
            },
        ];

        public static readonly object[] MexcCoins =
        [
            new
            {
                symbol = "BTCUSDT",
                baseAsset = "BTC",
                quoteAsset = "USDT",
                status = "1",
                fullName = "Bitcoin",
            },
            new
            {
                symbol = "SOLUSDT",
                baseAsset = "SOL",
                quoteAsset = "USDT",
                status = "1",
                fullName = "Solana",
            },
        ];

        public static readonly object[] CoinGeckoCoins =
        [
            new
            {
                id = "bitcoin",
                symbol = "btc",
                name = "Bitcoin",
            },
            new
            {
                id = "ethereum",
                symbol = "eth",
                name = "Ethereum",
            },
            new
            {
                id = "tether",
                symbol = "usdt",
                name = "Tether",
            },
            new
            {
                id = "solana",
                symbol = "sol",
                name = "Solana",
            },
            new
            {
                id = "cardano",
                symbol = "ada",
                name = "Cardano",
            },
        ];

        public static readonly object BinanceTickers = new
        {
            tickers = new[]
            {
                new
                {
                    @base = "BTC",
                    target = "USDT",
                    coin_id = "bitcoin",
                    target_coin_id = "tether",
                },
                new
                {
                    @base = "ETH",
                    target = "USDT",
                    coin_id = "ethereum",
                    target_coin_id = "tether",
                },
            },
        };

        public static readonly object BybitTickers = new
        {
            tickers = new[]
            {
                new
                {
                    @base = "ADA",
                    target = "USDT",
                    coin_id = "cardano",
                    target_coin_id = "tether",
                },
                new
                {
                    @base = "BTC",
                    target = "USDT",
                    coin_id = "bitcoin",
                    target_coin_id = "tether",
                },
            },
        };

        public static readonly object MexcTickers = new
        {
            tickers = new[]
            {
                new
                {
                    @base = "SOL",
                    target = "USDT",
                    coin_id = "solana",
                    target_coin_id = "tether",
                },
                new
                {
                    @base = "BTC",
                    target = "USDT",
                    coin_id = "bitcoin",
                    target_coin_id = "tether",
                },
            },
        };
    }
}
