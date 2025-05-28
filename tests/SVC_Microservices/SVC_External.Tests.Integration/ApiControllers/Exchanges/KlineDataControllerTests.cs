using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SharedLibrary.Enums;
using SharedLibrary.Extensions.Testing;
using SVC_External.ApiContracts.Requests;
using SVC_External.ApiContracts.Responses.Exchanges.KlineData;
using SVC_External.Tests.Integration.Factories;
using WireMock.Admin.Mappings;

namespace SVC_External.Tests.Integration.ApiControllers.Exchanges;

public class KlineDataControllerTests(CustomWebApplicationFactory factory)
    : BaseIntegrationTest(factory),
        IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task GetKlineDataForTradingPair_ShouldReturnOkWithData()
    {
        // Arrange
        await Factory.BinanceServerMock.PostMappingAsync(WireMockMappings.Binance.BtcKlines);

        // Act
        var response = await Client.PostAsJsonAsync(
            "/exchanges/kline/query",
            TestData.KlineRequest
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<KlineDataResponse>();

        result.Should().NotBeNull();
        result!.KlineData.Should().NotBeNull();
        result.KlineData.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetFirstSuccessfulKlineDataPerCoin_ShouldReturnOkWithData()
    {
        // Arrange
        await Factory.BinanceServerMock.PostMappingsAsync(
            [WireMockMappings.Binance.BtcKlines, WireMockMappings.Binance.EthKlines]
        );

        // Act
        var response = await Client.PostAsJsonAsync(
            "/exchanges/kline/query/bulk",
            TestData.KlineBatchRequest
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<IEnumerable<KlineDataResponse>>();

        result.Should().NotBeNull();
        result!.Should().HaveCount(2);
        result.Should().Contain(r => r.IdTradingPair == 1); // Trading pair ID for BTC/USDT
        result.Should().Contain(r => r.IdTradingPair == 2); // Trading pair ID for ETH/USDT
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
            "/exchanges/kline/query/bulk",
            TestData.KlineBatchRequestWithInvalidCoin
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<IEnumerable<KlineDataResponse>>();

        result.Should().NotBeNull();
        result!.Should().HaveCount(1);
        result.Should().Contain(r => r.IdTradingPair == 1); // Trading pair ID for BTC/USDT
    }

    [Fact]
    public async Task GetFirstSuccessfulKlineDataPerCoin_WithNoResults_ShouldReturnEmptyDictionary()
    {
        // Act
        var response = await Client.PostAsJsonAsync(
            "/exchanges/kline/query/bulk",
            TestData.KlineBatchRequestWithInvalidCoins
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<IEnumerable<KlineDataResponse>>();

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    private static class WireMockMappings
    {
        public static class Binance
        {
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
    }

    private static class TestData
    {
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
            StartTime = DateTime.Parse("2021-01-01"),
            EndTime = DateTime.Parse("2021-02-01"),
        };

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
            StartTime = DateTime.Parse("2021-01-01"),
            EndTime = DateTime.Parse("2021-02-01"),
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
            StartTime = DateTime.Parse("2021-01-01"),
            EndTime = DateTime.Parse("2021-02-01"),
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
            StartTime = DateTime.Parse("2021-01-01"),
            EndTime = DateTime.Parse("2021-02-01"),
        };

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
    }
}
