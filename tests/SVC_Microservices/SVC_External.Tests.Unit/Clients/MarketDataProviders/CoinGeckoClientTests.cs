using System.Collections.Frozen;
using System.Net;
using System.Text.Json;
using AutoFixture;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Contrib.HttpClient;
using SVC_External.Clients.MarketDataProviders;
using SVC_External.Models.MarketDataProviders.Output;
using static SVC_External.Models.MarketDataProviders.ClientResponses.CoinGeckoDtos;

namespace SVC_External.Tests.Unit.Clients.MarketDataProviders;

public class CoinGeckoClientTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly Mock<ILogger<CoinGeckoClient>> _loggerMock;
    private readonly CoinGeckoClient _client;
    private readonly IFixture _fixture;

    public CoinGeckoClientTests()
    {
        _fixture = new Fixture();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _loggerMock = new Mock<ILogger<CoinGeckoClient>>();

        var httpClient = _httpMessageHandlerMock.CreateClient();
        httpClient.BaseAddress = new Uri("https://api.coingecko.com");

        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock.Setup(f => f.CreateClient("CoinGeckoClient")).Returns(httpClient);

        _client = new CoinGeckoClient(httpClientFactoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetCoinsList_ReturnsExpectedData()
    {
        // Arrange
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, "https://api.coingecko.com/api/v3/coins/list")
            .ReturnsResponse(HttpStatusCode.OK, TestData.CoinsListJsonResponse);

        // Act
        var result = await _client.GetCoinsList();

        // Assert
        result.Should().BeEquivalentTo(TestData.ExpectedCoinsList);
    }

    [Fact]
    public async Task GetCoinsList_ErrorResponse_ReturnsEmptyCollection()
    {
        // Arrange
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, "https://api.coingecko.com/api/v3/coins/list")
            .ReturnsResponse(HttpStatusCode.BadRequest, "Bad Request");

        // Act
        var result = await _client.GetCoinsList();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSymbolToIdMapForExchange_ReturnsExpectedData()
    {
        // Arrange
        var exchangeId = "binance";
        var page1Endpoint =
            $"/api/v3/exchanges/{exchangeId}/tickers?depth=false&order=volume_desc&page=1";

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, $"https://api.coingecko.com{page1Endpoint}")
            .ReturnsResponse(HttpStatusCode.OK, TestData.ExchangeTickersPage1JsonResponse);

        // Act
        var result = await _client.GetSymbolToIdMapForExchange(exchangeId);

        // Assert
        result.Should().BeEquivalentTo(TestData.ExpectedSymbolToIdMap);
    }

    [Fact]
    public async Task GetSymbolToIdMapForExchange_ErrorResponseOnFirstPage_ReturnsEmptyDictionary()
    {
        // Arrange
        var exchangeId = "binance";
        var endpoint =
            $"/api/v3/exchanges/{exchangeId}/tickers?depth=false&order=volume_desc&page=1";

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, $"https://api.coingecko.com{endpoint}")
            .ReturnsResponse(HttpStatusCode.BadRequest, "Bad Request");

        // Act
        var result = await _client.GetSymbolToIdMapForExchange(exchangeId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSymbolToIdMapForExchange_EmptyResponseOnFirstPage_ReturnsEmptyDictionary()
    {
        // Arrange
        var exchangeId = "binance";
        var endpoint =
            $"/api/v3/exchanges/{exchangeId}/tickers?depth=false&order=volume_desc&page=1";
        var emptyResponse = new ExchangeTickersResponse { Tickers = [] };

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, $"https://api.coingecko.com{endpoint}")
            .ReturnsResponse(HttpStatusCode.OK, JsonSerializer.Serialize(emptyResponse));

        // Act
        var result = await _client.GetSymbolToIdMapForExchange(exchangeId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCoinsMarkets_ReturnsExpectedData()
    {
        // Arrange
        var ids = new[] { "bitcoin", "ethereum", "solana" };
        var endpoint =
            "/api/v3/coins/markets?vs_currency=usd&per_page=250&ids=bitcoin,ethereum,solana";

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, $"https://api.coingecko.com{endpoint}")
            .ReturnsResponse(HttpStatusCode.OK, TestData.CoinsMarketsJsonResponse);

        // Act
        var result = await _client.GetCoinsMarkets(ids);

        // Assert
        result.Should().BeEquivalentTo(TestData.ExpectedCoinsMarkets);
    }

    [Fact]
    public async Task GetCoinsMarkets_ErrorResponse_ReturnsEmptyCollection()
    {
        // Arrange
        var ids = new[] { "bitcoin", "ethereum" };
        var endpoint = "/api/v3/coins/markets?vs_currency=usd&per_page=250&ids=bitcoin,ethereum";

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, $"https://api.coingecko.com{endpoint}")
            .ReturnsResponse(HttpStatusCode.BadRequest, "Bad Request");

        // Act
        var result = await _client.GetCoinsMarkets(ids);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCoinsMarkets_EmptyIds_ReturnsEmptyCollection()
    {
        // Arrange
        var ids = Array.Empty<string>();

        // Act
        var result = await _client.GetCoinsMarkets(ids);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCoinsMarkets_MoreThanMaxIdsPerRequest_MakesMultipleRequests()
    {
        // Arrange
        var largeIds = Enumerable.Range(1, 300).Select(i => $"coin{i}").ToArray();

        // First chunk (250 ids)
        var firstChunkIds = string.Join(",", largeIds.Take(250));
        var firstEndpoint =
            $"/api/v3/coins/markets?vs_currency=usd&per_page=250&ids={firstChunkIds}";

        // Second chunk (50 ids)
        var secondChunkIds = string.Join(",", largeIds.Skip(250));
        var secondEndpoint =
            $"/api/v3/coins/markets?vs_currency=usd&per_page=250&ids={secondChunkIds}";

        // Create sample response data using AutoFixture
        var sampleAssets = _fixture.CreateMany<AssetCoinGecko>(3).ToList();
        var jsonResponse = JsonSerializer.Serialize(sampleAssets);

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, $"https://api.coingecko.com{firstEndpoint}")
            .ReturnsResponse(HttpStatusCode.OK, jsonResponse);

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, $"https://api.coingecko.com{secondEndpoint}")
            .ReturnsResponse(HttpStatusCode.OK, "[]");

        // Act
        await _client.GetCoinsMarkets(largeIds);

        // Assert
        _httpMessageHandlerMock.VerifyRequest(
            HttpMethod.Get,
            $"https://api.coingecko.com{firstEndpoint}",
            Times.Once()
        );

        _httpMessageHandlerMock.VerifyRequest(
            HttpMethod.Get,
            $"https://api.coingecko.com{secondEndpoint}",
            Times.Once()
        );
    }

    private static class TestData
    {
        #region GetCoinsList
        public static readonly List<CoinListResponse> CoinsListResponse =
        [
            new()
            {
                Id = "bitcoin",
                Symbol = "btc",
                Name = "Bitcoin",
            },
            new()
            {
                Id = "ethereum",
                Symbol = "eth",
                Name = "Ethereum",
            },
            new()
            {
                Id = "solana",
                Symbol = "sol",
                Name = "Solana",
            },
        ];

        public static readonly string CoinsListJsonResponse = JsonSerializer.Serialize(
            CoinsListResponse
        );

        public static readonly List<CoinCoinGecko> ExpectedCoinsList =
        [
            new()
            {
                Id = "bitcoin",
                Symbol = "btc",
                Name = "Bitcoin",
            },
            new()
            {
                Id = "ethereum",
                Symbol = "eth",
                Name = "Ethereum",
            },
            new()
            {
                Id = "solana",
                Symbol = "sol",
                Name = "Solana",
            },
        ];
        #endregion

        #region GetSymbolToIdMapForExchange
        public static readonly ExchangeTickersResponse ExchangeTickersPage1Response = new()
        {
            Tickers =
            [
                new()
                {
                    BaseCoin = "BTC",
                    BaseCoinId = "bitcoin",
                    TargetCoin = "USDT",
                    TargetCoinId = "tether",
                },
                new()
                {
                    BaseCoin = "ETH",
                    BaseCoinId = "ethereum",
                    TargetCoin = "USDT",
                    TargetCoinId = "tether",
                },
                new()
                {
                    BaseCoin = "SOL",
                    BaseCoinId = "solana",
                    TargetCoin = "USDT",
                    TargetCoinId = "tether",
                },
            ],
        };

        public static readonly string ExchangeTickersPage1JsonResponse = JsonSerializer.Serialize(
            ExchangeTickersPage1Response
        );

        public static readonly FrozenDictionary<string, string?> ExpectedSymbolToIdMap =
            new Dictionary<string, string?>
            {
                { "BTC", "bitcoin" },
                { "ETH", "ethereum" },
                { "SOL", "solana" },
                { "USDT", "tether" },
            }.ToFrozenDictionary();
        #endregion

        #region GetCoinsMarkets
        public static readonly List<AssetCoinGecko> CoinsMarkets =
        [
            new()
            {
                Symbol = "btc",
                Name = "Bitcoin",
                PriceUsd = 68500.45m,
                MarketCapUsd = 1350000000000,
            },
            new()
            {
                Symbol = "eth",
                Name = "Ethereum",
                PriceUsd = 3450.22m,
                MarketCapUsd = 420000000000,
            },
            new()
            {
                Symbol = "sol",
                Name = "Solana",
                PriceUsd = 145.78m,
                MarketCapUsd = 64000000000,
            },
        ];

        public static readonly string CoinsMarketsJsonResponse = JsonSerializer.Serialize(
            CoinsMarkets
        );

        public static readonly List<AssetCoinGecko> ExpectedCoinsMarkets = CoinsMarkets;
        #endregion
    }
}
