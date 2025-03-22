using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SharedLibrary.Enums;
using SharedLibrary.Extensions.Testing;
using SVC_External.Models.Input;
using SVC_External.Models.Output;
using SVC_External.Tests.Integration.Factories;
using WireMock.Admin.Mappings;

namespace SVC_External.Tests.Integration.Controllers;

public class ExchangesControllerTests(CustomWebApplicationFactory factory)
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

    #region GetAllSpotCoins Tests

    [Fact]
    public async Task GetAllListedCoins_ShouldReturnOkWithData()
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
        var response = await Client.GetAsync("/exchanges/spot/coins");

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
    public async Task GetAllListedCoins_ExchangesUnavailable_ShouldReturnError()
    {
        // Clear cache to ensure clean test
        await ClearCacheAsync();

        // Act
        var response = await Client.GetAsync("/exchanges/spot/coins");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }
    #endregion

    #region GetKlineDataForTradingPair Tests

    [Fact]
    public async Task GetKlineDataForTradingPair_ShouldReturnOkWithData()
    {
        // Arrange
        await Factory.BinanceServerMock.PostMappingAsync(WireMockMappings.Binance.BtcKlines);

        // Act
        var response = await Client.PostAsJsonAsync(
            "/exchanges/klineData/query",
            TestData.KlineRequest
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<KlineDataRequestResponse>();

        result.Should().NotBeNull();
        result!.KlineData.Should().NotBeNull();
        result.KlineData.Should().HaveCount(2);
    }
    #endregion

    #region GetFirstSuccessfulKlineDataPerCoin

    [Fact]
    public async Task GetFirstSuccessfulKlineDataPerCoin_ShouldReturnOkWithData()
    {
        // Arrange
        await Factory.BinanceServerMock.PostMappingsAsync(
            [WireMockMappings.Binance.BtcKlines, WireMockMappings.Binance.EthKlines]
        );

        // Act
        var response = await Client.PostAsJsonAsync(
            "/exchanges/klineData/batchQuery",
            TestData.KlineBatchRequest
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<
            Dictionary<int, IEnumerable<KlineData>>
        >();

        result.Should().NotBeNull();
        result!.Should().HaveCount(2);
        result.Should().ContainKey(1); // Trading pair ID for BTC/USDT
        result.Should().ContainKey(2); // Trading pair ID for ETH/USDT
    }

    [Fact]
    public async Task GetFirstSuccessfulKlineDataPerCoin_WithPartialResults_ShouldReturnPartialData()
    {
        // Arrange
        var binanceTask = Factory.BinanceServerMock.PostMappingsAsync(
            [WireMockMappings.Binance.BtcKlines, WireMockMappings.Binance.KlinesUnknownSymbol]
        );
        var bybitTask = Factory.BybitServerMock.PostMappingAsync(
            WireMockMappings.Bybit.KlinesUnknownSymbol
        );
        await Task.WhenAll(binanceTask, bybitTask);

        // Act
        var response = await Client.PostAsJsonAsync(
            "/exchanges/klineData/batchQuery",
            TestData.KlineBatchRequestWithInvalidCoin
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<
            Dictionary<int, IEnumerable<KlineData>>
        >();

        result.Should().NotBeNull();
        result!.Should().HaveCount(1);
        result.Should().ContainKey(1); // Trading pair ID for BTC/USDT
    }

    [Fact]
    public async Task GetFirstSuccessfulKlineDataPerCoin_WithNoResults_ShouldReturnEmptyDictionary()
    {
        // Act
        var response = await Client.PostAsJsonAsync(
            "/exchanges/klineData/batchQuery",
            TestData.KlineBatchRequestWithInvalidCoins
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<
            Dictionary<int, IEnumerable<KlineData>>
        >();

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }
    #endregion

    #region GetCoinGeckoAssetsInfo Tests
    [Fact]
    public async Task GetCoinGeckoAssetsInfo_ShouldReturnOkWithData()
    {
        // Arrange
        var coinIds = new List<string> { "bitcoin", "ethereum", "tether", "usd-coin" };
        await Factory.CoinGeckoServerMock.PostMappingsAsync(
            [WireMockMappings.CoinGecko.Markets, WireMockMappings.CoinGecko.MarketsStablecoins]
        );

        // Act
        var queryParams = string.Join("&", coinIds.Select(id => $"coinGeckoIds={id}"));
        var response = await Client.GetAsync($"exchanges/coins/marketInfo?{queryParams}");

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
        var idsQueryParam = string.Join("&", ids.Select(id => $"coinGeckoIds={id}"));

        // Act
        var response = await Client.GetAsync($"exchanges/coins/marketInfo?{idsQueryParam}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task GetCoinGeckoAssetsInfo_WithNoIds_ShouldReturnBadRequest()
    {
        // Act
        var response = await Client.GetAsync("exchanges/coins/marketInfo");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
    #endregion

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

            public static MappingModel BtcKlines =>
                new()
                {
                    Request = new RequestModel
                    {
                        Methods = ["GET"],
                        Path = "/api/v3/klines",
                        Params =
                        [
                            WireMockParamBuilder.WithExactMatch("symbol", "BTCUSDT"),
                            WireMockParamBuilder.WithExactMatch("interval", "1d"),
                        ],
                    },
                    Response = new ResponseModel
                    {
                        StatusCode = 200,
                        Headers = new Dictionary<string, object>
                        {
                            ["Content-Type"] = "application/json",
                        },
                        Body = JsonSerializer.Serialize(TestData.BtcKlineResponseData),
                    },
                };

            public static MappingModel EthKlines =>
                new()
                {
                    Request = new RequestModel
                    {
                        Methods = ["GET"],
                        Path = "/api/v3/klines",
                        Params =
                        [
                            WireMockParamBuilder.WithExactMatch("symbol", "ETHUSDT"),
                            WireMockParamBuilder.WithExactMatch("interval", "1d"),
                        ],
                    },
                    Response = new ResponseModel
                    {
                        StatusCode = 200,
                        Headers = new Dictionary<string, object>
                        {
                            ["Content-Type"] = "application/json",
                        },
                        Body = JsonSerializer.Serialize(TestData.EthKlineResponseData),
                    },
                };

            public static MappingModel KlinesUnknownSymbol =>
                new()
                {
                    Request = new RequestModel
                    {
                        Methods = ["GET"],
                        Path = "/api/v3/klines",
                        Params =
                        [
                            WireMockParamBuilder.WithExactMatch("symbol", "UNKNOWNUSDT"),
                            WireMockParamBuilder.WithExactMatch("interval", "1d"),
                        ],
                    },
                    Response = new ResponseModel
                    {
                        StatusCode = 400,
                        Headers = new Dictionary<string, object>
                        {
                            ["Content-Type"] = "application/json",
                        },
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

            public static MappingModel KlinesUnknownSymbol =>
                new()
                {
                    Request = new RequestModel
                    {
                        Methods = ["GET"],
                        Path = "/v5/market/kline",
                        Params =
                        [
                            WireMockParamBuilder.WithExactMatch("symbol", "UNKNOWNUSDT"),
                            WireMockParamBuilder.WithExactMatch("interval", "D"),
                        ],
                    },
                    Response = new ResponseModel
                    {
                        StatusCode = 200,
                        Headers = new Dictionary<string, object>
                        {
                            ["Content-Type"] = "application/json",
                        },
                        Body = JsonSerializer.Serialize(
                            new
                            {
                                retCode = 10001,
                                retMsg = "params error: Symbol Is Invalid",
                                result = new { },
                            }
                        ),
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

            // Helper method moved inside CoinGecko class
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
        #region Exchange Coins
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

        // CoinGecko mock data
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
        #endregion

        #region Kline Requests
        public static readonly KlineDataRequest KlineRequest = new()
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
                Exchanges = [Exchange.Binance],
            },
            Interval = ExchangeKlineInterval.OneDay,
            StartTime = DateTime.Parse("2021-01-01", CultureInfo.InvariantCulture),
            EndTime = DateTime.Parse("2021-02-01", CultureInfo.InvariantCulture),
        };
        #endregion

        #region Kline Batch Requests
        public static readonly KlineDataBatchRequest KlineBatchRequest = new()
        {
            MainCoins =
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
                            CoinQuote = new KlineDataRequestCoinQuote
                            {
                                Id = 2,
                                Symbol = "USDT",
                                Name = "Tether",
                            },
                            Exchanges = [Exchange.Binance],
                        },
                    ],
                },
                new()
                {
                    Id = 3,
                    Symbol = "ETH",
                    Name = "Ethereum",
                    TradingPairs =
                    [
                        new()
                        {
                            Id = 2,
                            CoinQuote = new KlineDataRequestCoinQuote
                            {
                                Id = 2,
                                Symbol = "USDT",
                                Name = "Tether",
                            },
                            Exchanges = [Exchange.Binance],
                        },
                    ],
                },
            ],
            Interval = ExchangeKlineInterval.OneDay,
            StartTime = DateTime.Parse("2021-01-01", CultureInfo.InvariantCulture),
            EndTime = DateTime.Parse("2021-02-01", CultureInfo.InvariantCulture),
        };

        public static readonly KlineDataBatchRequest KlineBatchRequestWithInvalidCoin = new()
        {
            MainCoins =
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
                            CoinQuote = new KlineDataRequestCoinQuote
                            {
                                Id = 2,
                                Symbol = "USDT",
                                Name = "Tether",
                            },
                            Exchanges = [Exchange.Binance],
                        },
                    ],
                },
                new()
                {
                    Id = 999,
                    Symbol = "UNKNOWN",
                    Name = "Unknown",
                    TradingPairs =
                    [
                        new()
                        {
                            Id = 999,
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
            StartTime = DateTime.Parse("2021-01-01", CultureInfo.InvariantCulture),
            EndTime = DateTime.Parse("2021-02-01", CultureInfo.InvariantCulture),
        };

        public static readonly KlineDataBatchRequest KlineBatchRequestWithInvalidCoins = new()
        {
            MainCoins =
            [
                new()
                {
                    Id = 998,
                    Symbol = "INVALID1",
                    Name = "Invalid1",
                    TradingPairs =
                    [
                        new()
                        {
                            Id = 998,
                            CoinQuote = new KlineDataRequestCoinQuote
                            {
                                Id = 2,
                                Symbol = "USDT",
                                Name = "Tether",
                            },
                            Exchanges = [Exchange.Binance],
                        },
                    ],
                },
                new()
                {
                    Id = 999,
                    Symbol = "INVALID2",
                    Name = "Invalid2",
                    TradingPairs =
                    [
                        new()
                        {
                            Id = 999,
                            CoinQuote = new KlineDataRequestCoinQuote
                            {
                                Id = 2,
                                Symbol = "USDT",
                                Name = "Tether",
                            },
                            Exchanges = [Exchange.Binance],
                        },
                    ],
                },
            ],
            Interval = ExchangeKlineInterval.OneDay,
            StartTime = DateTime.Parse("2021-01-01", CultureInfo.InvariantCulture),
            EndTime = DateTime.Parse("2021-02-01", CultureInfo.InvariantCulture),
        };
        #endregion

        #region Kline Response Data
        public static readonly List<object[]> BtcKlineResponseData =
        [
            [
                1609459200000,
                "28000.0",
                "29000.0",
                "27000.0",
                "28500.0",
                "1000.0",
                1609545600000,
                "28500000.0",
                1000,
                "500.0",
                "14250000.0",
                "0.0",
            ],
            [
                1609545600000,
                "28500.0",
                "30000.0",
                "28000.0",
                "29500.0",
                "1200.0",
                1609632000000,
                "35400000.0",
                1200,
                "600.0",
                "17700000.0",
                "0.0",
            ],
        ];

        public static readonly List<object[]> EthKlineResponseData =
        [
            [
                1609459200000,
                "700.0",
                "750.0",
                "680.0",
                "720.0",
                "5000.0",
                1609545600000,
                "3600000.0",
                5000,
                "2500.0",
                "1800000.0",
                "0.0",
            ],
            [
                1609545600000,
                "720.0",
                "780.0",
                "700.0",
                "760.0",
                "6000.0",
                1609632000000,
                "4560000.0",
                6000,
                "3000.0",
                "2280000.0",
                "0.0",
            ],
        ];
        #endregion

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
