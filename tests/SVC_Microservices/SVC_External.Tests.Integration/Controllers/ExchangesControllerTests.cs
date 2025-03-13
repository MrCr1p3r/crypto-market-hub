using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SharedLibrary.Enums;
using SVC_External.Models.Input;
using SVC_External.Models.Output;
using SVC_External.Tests.Integration.Factories;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace SVC_External.Tests.Integration.Controllers;

public class ExchangesControllerIntegrationTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private readonly CustomWebApplicationFactory _factory = factory;
    private const string CacheKey = "all_current_active_spot_coins";

    // Setup method to clear specific cache keys before each test
    private async Task ClearCacheAsync()
    {
        using var scope = _factory.Services.CreateScope();
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
        // Setup Binance mock
        _factory
            .BinanceServerMock.Given(Request.Create().WithPath("/api/v3/exchangeInfo").UsingGet())
            .RespondWith(
                Response
                    .Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(JsonSerializer.Serialize(new { symbols = TestData.BinanceCoins }))
            );

        // Setup Bybit mock
        _factory
            .BybitServerMock.Given(
                Request
                    .Create()
                    .WithPath("/v5/market/instruments-info")
                    .WithParam("category", "spot")
                    .UsingGet()
            )
            .RespondWith(
                Response
                    .Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(
                        JsonSerializer.Serialize(
                            new { result = new { list = TestData.BybitCoins } }
                        )
                    )
            );

        // Setup MEXC mock
        _factory
            .MexcServerMock.Given(Request.Create().WithPath("/api/v3/exchangeInfo").UsingGet())
            .RespondWith(
                Response
                    .Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(JsonSerializer.Serialize(new { symbols = TestData.MexcCoins }))
            );

        // Setup CoinGecko coins list mock
        _factory
            .CoinGeckoServerMock.Given(Request.Create().WithPath("/api/v3/coins/list").UsingGet())
            .RespondWith(
                Response
                    .Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(JsonSerializer.Serialize(TestData.CoinGeckoCoins))
            );

        // Setup CoinGecko symbol-to-id map for Binance
        _factory
            .CoinGeckoServerMock.Given(
                Request
                    .Create()
                    .WithPath("/api/v3/exchanges/binance/tickers")
                    .WithParam("depth", "false")
                    .WithParam("order", "volume_desc")
                    .WithParam("page", "1")
                    .UsingGet()
            )
            .RespondWith(
                Response
                    .Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(JsonSerializer.Serialize(TestData.BinanceTickers))
            );

        // Setup CoinGecko symbol-to-id map for Bybit
        _factory
            .CoinGeckoServerMock.Given(
                Request
                    .Create()
                    .WithPath("/api/v3/exchanges/bybit_spot/tickers")
                    .WithParam("depth", "false")
                    .WithParam("order", "volume_desc")
                    .WithParam("page", "1")
                    .UsingGet()
            )
            .RespondWith(
                Response
                    .Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(JsonSerializer.Serialize(TestData.BybitTickers))
            );

        // Setup CoinGecko symbol-to-id map for MEXC
        _factory
            .CoinGeckoServerMock.Given(
                Request
                    .Create()
                    .WithPath("/api/v3/exchanges/mxc/tickers")
                    .WithParam("depth", "false")
                    .WithParam("order", "volume_desc")
                    .WithParam("page", "1")
                    .UsingGet()
            )
            .RespondWith(
                Response
                    .Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(JsonSerializer.Serialize(TestData.MexcTickers))
            );

        // Act
        var response = await _client.GetAsync("/exchanges/spot/coins");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<List<Coin>>(content, TestData.JsonOptions);

        result.Should().NotBeNull();
        result.Should().HaveCountGreaterThanOrEqualTo(4); // At least 4 unique coins from the 3 exchanges

        // Verify some specific coins are present
        result.Should().Contain(c => c.Symbol == "BTC");
        result.Should().Contain(c => c.Symbol == "ETH");
        result.Should().Contain(c => c.Symbol == "ADA");
        result.Should().Contain(c => c.Symbol == "SOL");
    }

    [Fact]
    public async Task GetAllListedCoins_WhenAllExchangesUnavailable_ShouldReturn500()
    {
        // Clear cache to ensure clean test
        await ClearCacheAsync();

        // Arrange
        // Set up all exchange mocks to return errors
        _factory
            .BinanceServerMock.Given(Request.Create().WithPath("/api/v3/exchangeInfo").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(500));

        _factory
            .BybitServerMock.Given(Request.Create().WithPath("/v5/market/tickers").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(500));

        _factory
            .MexcServerMock.Given(Request.Create().WithPath("/api/v3/exchangeInfo").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(500));

        // Setup CoinGecko coins list mock - this will still return valid data
        _factory
            .CoinGeckoServerMock.Given(Request.Create().WithPath("/api/v3/coins/list").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(500));

        // Act
        var response = await _client.GetAsync("/exchanges/spot/coins");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }
    #endregion

    #region GetKlineDataForTradingPair Tests

    [Fact]
    public async Task GetKlineDataForTradingPair_ShouldReturnOkWithData()
    {
        // Arrange
        var klineRequest = TestData.KlineRequest;
        var klineResponseData = TestData.BtcKlineResponseData;

        _factory
            .BinanceServerMock.Given(
                Request
                    .Create()
                    .WithPath("/api/v3/klines")
                    .UsingGet()
                    .WithParam("symbol", "BTCUSDT")
                    .WithParam("interval", "1d")
            )
            .RespondWith(
                Response
                    .Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(JsonSerializer.Serialize(klineResponseData))
            );

        // Act
        var requestContent = new StringContent(
            JsonSerializer.Serialize(klineRequest),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _client.PostAsync("/exchanges/klineData/query", requestContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<KlineDataRequestResponse>(
            content,
            TestData.JsonOptions
        );

        result.Should().NotBeNull();
        result!.KlineData.Should().NotBeNull();
        result.KlineData.Should().HaveCount(2);
    }
    #endregion

    #region GetFirstSuccessfulKlineDataPerCoin Tests

    [Fact]
    public async Task GetFirstSuccessfulKlineDataPerCoin_ShouldReturnOkWithData()
    {
        // Arrange
        var batchRequest = TestData.KlineBatchRequest;
        var btcKlineData = TestData.BtcKlineResponseData;
        var ethKlineData = TestData.EthKlineResponseData;

        _factory
            .BinanceServerMock.Given(
                Request
                    .Create()
                    .WithPath("/api/v3/klines")
                    .UsingGet()
                    .WithParam("symbol", "BTCUSDT")
                    .WithParam("interval", "1d")
            )
            .RespondWith(
                Response
                    .Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(JsonSerializer.Serialize(btcKlineData))
            );

        _factory
            .BinanceServerMock.Given(
                Request
                    .Create()
                    .WithPath("/api/v3/klines")
                    .UsingGet()
                    .WithParam("symbol", "ETHUSDT")
                    .WithParam("interval", "1d")
            )
            .RespondWith(
                Response
                    .Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(JsonSerializer.Serialize(ethKlineData))
            );

        // Act
        var requestContent = new StringContent(
            JsonSerializer.Serialize(batchRequest),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _client.PostAsync("/exchanges/klineData/batchQuery", requestContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<List<KlineDataRequestResponse>>(
            content,
            TestData.JsonOptions
        );

        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetFirstSuccessfulKlineDataPerCoin_WithPartialResults_ShouldReturnPartialData()
    {
        // Arrange
        var batchRequest = TestData.KlineBatchRequestWithInvalidCoin;
        var btcKlineData = TestData.BtcKlineDataShort;
        var ethKlineData = TestData.EthKlineDataShort;

        _factory
            .BinanceServerMock.Given(
                Request
                    .Create()
                    .WithPath("/api/v3/klines")
                    .UsingGet()
                    .WithParam("symbol", "BTCUSDT")
            )
            .RespondWith(
                Response
                    .Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(JsonSerializer.Serialize(btcKlineData))
            );

        _factory
            .BinanceServerMock.Given(
                Request
                    .Create()
                    .WithPath("/api/v3/klines")
                    .UsingGet()
                    .WithParam("symbol", "ETHUSDT")
            )
            .RespondWith(
                Response
                    .Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(JsonSerializer.Serialize(ethKlineData))
            );

        _factory
            .BinanceServerMock.Given(
                Request
                    .Create()
                    .WithPath("/api/v3/klines")
                    .UsingGet()
                    .WithParam("symbol", "UNKNOWNUSDT")
            )
            .RespondWith(Response.Create().WithStatusCode(400).WithBody("Invalid symbol"));

        _factory
            .BybitServerMock.Given(
                Request
                    .Create()
                    .WithPath("/v5/market/kline")
                    .UsingGet()
                    .WithParam("symbol", "UNKNOWNUSDT")
            )
            .RespondWith(Response.Create().WithStatusCode(400).WithBody("Invalid symbol"));

        // Act
        var requestContent = new StringContent(
            JsonSerializer.Serialize(batchRequest),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _client.PostAsync("/exchanges/klineData/batchQuery", requestContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<List<KlineDataRequestResponse>>(
            content,
            TestData.JsonOptions
        );

        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetFirstSuccessfulKlineDataPerCoin_WithNoResults_ShouldReturnEmptyList()
    {
        // Arrange
        var batchRequest = TestData.KlineBatchRequestWithInvalidCoins;

        _factory
            .BinanceServerMock.Given(
                Request
                    .Create()
                    .WithPath("/api/v3/klines")
                    .UsingGet()
                    .WithParam("symbol", "INVALID1USDT")
            )
            .RespondWith(Response.Create().WithStatusCode(400).WithBody("Invalid symbol"));

        _factory
            .BinanceServerMock.Given(
                Request
                    .Create()
                    .WithPath("/api/v3/klines")
                    .UsingGet()
                    .WithParam("symbol", "INVALID2USDT")
            )
            .RespondWith(Response.Create().WithStatusCode(400).WithBody("Invalid symbol"));

        _factory
            .BybitServerMock.Given(
                Request
                    .Create()
                    .WithPath("/v5/market/kline")
                    .UsingGet()
                    .WithParam("symbol", "INVALID1USDT")
            )
            .RespondWith(Response.Create().WithStatusCode(400).WithBody("Invalid symbol"));

        _factory
            .BybitServerMock.Given(
                Request
                    .Create()
                    .WithPath("/v5/market/kline")
                    .UsingGet()
                    .WithParam("symbol", "INVALID2USDT")
            )
            .RespondWith(Response.Create().WithStatusCode(400).WithBody("Invalid symbol"));

        // Act
        var requestContent = new StringContent(
            JsonSerializer.Serialize(batchRequest),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _client.PostAsync("/exchanges/klineData/batchQuery", requestContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<List<KlineDataRequestResponse>>(
            content,
            TestData.JsonOptions
        );

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

        // Set up mocks for both API calls
        // Mock the coins/markets endpoint for asset data
        _factory
            .CoinGeckoServerMock.Given(
                Request
                    .Create()
                    .WithPath($"/api/v3/coins/markets")
                    .WithParam("vs_currency", "usd")
                    .WithParam("per_page", "250")
                    .UsingGet()
            )
            .RespondWith(
                Response
                    .Create()
                    .WithStatusCode(HttpStatusCode.OK)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(
                        JsonSerializer.Serialize(TestData.CoinGeckoMarketData, TestData.JsonOptions)
                    )
            );

        // Mock the stablecoins endpoint
        _factory
            .CoinGeckoServerMock.Given(
                Request
                    .Create()
                    .WithPath("/api/v3/coins/markets")
                    .WithParam("vs_currency", "usd")
                    .WithParam("category", "stablecoins")
                    .WithParam("per_page", "250")
                    .WithParam("page", "1")
                    .WithParam("sparkline", "false")
                    .UsingGet()
            )
            .RespondWith(
                Response
                    .Create()
                    .WithStatusCode(HttpStatusCode.OK)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(
                        JsonSerializer.Serialize(
                            TestData.CoinGeckoStablecoins,
                            TestData.JsonOptions
                        )
                    )
            );

        // Act
        // Build a URL with multiple coinGeckoIds parameters
        var queryParams = string.Join("&", coinIds.Select(id => $"coinGeckoIds={id}"));
        var response = await _client.GetAsync($"exchanges/coins/marketInfo?{queryParams}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseData = await response.Content.ReadFromJsonAsync<List<CoinGeckoAssetInfo>>();

        responseData.Should().NotBeNull();
        responseData!.Should().HaveCount(4);

        // Check each coin is present
        responseData.Should().Contain(c => c.Id == "bitcoin");
        responseData.Should().Contain(c => c.Id == "ethereum");
        responseData.Should().Contain(c => c.Id == "tether");
        responseData.Should().Contain(c => c.Id == "usd-coin");

        // Verify stablecoin flags
        responseData.Should().NotBeNull();

        var btcAsset = responseData!.FirstOrDefault(c => c.Id == "bitcoin");
        var ethAsset = responseData!.FirstOrDefault(c => c.Id == "ethereum");
        var tetherAsset = responseData!.FirstOrDefault(c => c.Id == "tether");
        var usdcAsset = responseData!.FirstOrDefault(c => c.Id == "usd-coin");

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
    public async Task GetCoinGeckoAssetsInfo_WithEmptyResponse_ShouldReturnOkWithEmptyResponse()
    {
        // Arrange
        var coinIds = new List<string> { "unknown-coin-id" };

        // Mock the coins/markets endpoint with empty response
        var coinIdsParam = string.Join(",", coinIds);
        _factory
            .CoinGeckoServerMock.Given(
                Request
                    .Create()
                    .WithPath($"/api/v3/coins/markets")
                    .WithParam("vs_currency", "usd")
                    .WithParam("per_page", "250")
                    .WithParam("ids", coinIdsParam)
                    .UsingGet()
            )
            .RespondWith(
                Response
                    .Create()
                    .WithStatusCode(HttpStatusCode.OK)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody("[]")
            );

        // Mock the stablecoins endpoint
        _factory
            .CoinGeckoServerMock.Given(
                Request
                    .Create()
                    .WithPath("/api/v3/coins/markets")
                    .WithParam("vs_currency", "usd")
                    .WithParam("category", "stablecoins")
                    .WithParam("per_page", "250")
                    .WithParam("page", "1")
                    .WithParam("sparkline", "false")
                    .UsingGet()
            )
            .RespondWith(
                Response
                    .Create()
                    .WithStatusCode(HttpStatusCode.OK)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(
                        JsonSerializer.Serialize(
                            TestData.CoinGeckoStablecoins,
                            TestData.JsonOptions
                        )
                    )
            );

        // Act
        var queryParams = string.Join("&", coinIds.Select(id => $"coinGeckoIds={id}"));
        var response = await _client.GetAsync($"exchanges/coins/marketInfo?{queryParams}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetCoinGeckoAssetsInfo_WithNoIds_ShouldReturnBadRequest()
    {
        // Act
        var response = await _client.GetAsync("exchanges/coins/marketInfo");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
    #endregion

    private static class TestData
    {
        // JSON options for serialization/deserialization
        public static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
        };

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
                            Exchanges = [Exchange.Binance, Exchange.Bybit],
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
                            Exchanges = [Exchange.Binance, Exchange.Bybit],
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

        public static readonly List<object[]> BtcKlineDataShort =
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
        ];

        public static readonly List<object[]> EthKlineDataShort =
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
