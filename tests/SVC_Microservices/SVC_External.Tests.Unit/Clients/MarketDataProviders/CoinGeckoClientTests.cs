using System.Collections.Frozen;
using System.Net;
using System.Text.Json;
using AutoFixture;
using FluentAssertions;
using FluentResults.Extensions.FluentAssertions;
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
    public async Task GetCoinsList_ReturnsSuccessfulResultWithExpectedDataInside()
    {
        // Arrange
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, "https://api.coingecko.com/api/v3/coins/list")
            .ReturnsResponse(HttpStatusCode.OK, TestData.CoinsListJsonResponse);

        // Act
        var result = await _client.GetCoinsList();

        // Assert
        result
            .Should()
            .BeSuccess()
            .Which.Value.Should()
            .BeEquivalentTo(TestData.ExpectedCoinsList);
    }

    [Fact]
    public async Task GetCoinsList_ErrorResponse_ReturnsFailedResult()
    {
        // Arrange
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, "https://api.coingecko.com/api/v3/coins/list")
            .ReturnsResponse(HttpStatusCode.BadRequest, "Bad Request");

        // Act
        var result = await _client.GetCoinsList();

        // Assert
        result.Should().BeFailure().Which.Errors.Should().HaveCount(1);
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
        result
            .Should()
            .BeSuccess()
            .Which.Value.Should()
            .BeEquivalentTo(TestData.ExpectedSymbolToIdMap);
    }

    [Fact]
    public async Task GetSymbolToIdMapForExchange_ErrorResponseOnFirstPage_ReturnsFailedResult()
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
        result.Should().BeFailure().Which.Errors.Should().HaveCount(1);
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
        result.Should().BeSuccess().Which.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMarketDataForCoins_ReturnsExpectedData()
    {
        // Arrange
        var ids = new[] { "bitcoin", "ethereum", "solana" };
        var endpoint =
            "/api/v3/coins/markets?vs_currency=usd&per_page=250&ids=bitcoin,ethereum,solana";

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, $"https://api.coingecko.com{endpoint}")
            .ReturnsResponse(HttpStatusCode.OK, TestData.CoinsMarketsJsonResponse);

        // Act
        var result = await _client.GetMarketDataForCoins(ids);

        // Assert
        result
            .Should()
            .BeSuccess()
            .Which.Value.Should()
            .BeEquivalentTo(TestData.ExpectedCoinsMarkets);
    }

    [Fact]
    public async Task GetMarketDataForCoins_ErrorResponse_ReturnsFailedResult()
    {
        // Arrange
        var ids = new[] { "bitcoin", "ethereum" };
        var endpoint = "/api/v3/coins/markets?vs_currency=usd&per_page=250&ids=bitcoin,ethereum";

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, $"https://api.coingecko.com{endpoint}")
            .ReturnsResponse(HttpStatusCode.BadRequest, "Bad Request");

        // Act
        var result = await _client.GetMarketDataForCoins(ids);

        // Assert
        result.Should().BeFailure().Which.Errors.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetMarketDataForCoins_EmptyIds_ReturnsFailedResult()
    {
        // Arrange
        var ids = Array.Empty<string>();

        // Act
        var result = await _client.GetMarketDataForCoins(ids);

        // Assert
        result.Should().BeFailure().Which.Errors.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetMarketDataForCoins_MoreThanMaxIdsPerRequest_MakesMultipleRequests()
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
        var result = await _client.GetMarketDataForCoins(largeIds);

        // Assert
        result.Should().BeSuccess();
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

    [Fact]
    public async Task GetStablecoinsIds_ReturnsExpectedData()
    {
        // Arrange
        var endpoint1 =
            "/api/v3/coins/markets?vs_currency=usd&category=stablecoins&per_page=250&page=1&sparkline=false";
        var endpoint2 =
            "/api/v3/coins/markets?vs_currency=usd&category=stablecoins&per_page=250&page=2&sparkline=false";

        // Set up first page with max items
        var page1Response = _fixture.CreateMany<AssetCoinGecko>(250).ToList();
        foreach (var asset in page1Response)
        {
            asset.Id = $"stablecoin-{Guid.NewGuid()}";
        }
        var page1Json = JsonSerializer.Serialize(page1Response);

        // Set up second page with fewer items (indicating last page)
        var page2Response = _fixture.CreateMany<AssetCoinGecko>(50).ToList();
        foreach (var asset in page2Response)
        {
            asset.Id = $"stablecoin-{Guid.NewGuid()}";
        }
        var page2Json = JsonSerializer.Serialize(page2Response);

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, $"https://api.coingecko.com{endpoint1}")
            .ReturnsResponse(HttpStatusCode.OK, page1Json);

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, $"https://api.coingecko.com{endpoint2}")
            .ReturnsResponse(HttpStatusCode.OK, page2Json);

        // Act
        var result = await _client.GetStablecoinsIds();

        // Assert
        result.Should().BeSuccess();
        result.Value.Should().HaveCount(300);
        result.Value.Should().Contain(page1Response.Select(c => c.Id));
        result.Value.Should().Contain(page2Response.Select(c => c.Id));
    }

    [Fact]
    public async Task GetStablecoinsIds_SinglePage_ReturnsExpectedData()
    {
        // Arrange
        var endpoint =
            "/api/v3/coins/markets?vs_currency=usd&category=stablecoins&per_page=250&page=1&sparkline=false";

        // Create response with fewer than max items (single page)
        var response = _fixture.CreateMany<AssetCoinGecko>(50).ToList();
        foreach (var asset in response)
        {
            asset.Id = $"stablecoin-{Guid.NewGuid()}";
        }
        var jsonResponse = JsonSerializer.Serialize(response);

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, $"https://api.coingecko.com{endpoint}")
            .ReturnsResponse(HttpStatusCode.OK, jsonResponse);

        // Act
        var result = await _client.GetStablecoinsIds();

        // Assert
        result.Should().BeSuccess();
        result.Value.Should().HaveCount(50);
        result.Value.Should().BeEquivalentTo(response.Select(c => c.Id));

        // Verify only one page was requested
        _httpMessageHandlerMock.VerifyRequest(
            HttpMethod.Get,
            $"https://api.coingecko.com{endpoint}",
            Times.Once()
        );
    }

    [Fact]
    public async Task GetStablecoinsIds_ErrorResponse_ReturnsFailedResult()
    {
        // Arrange
        var endpoint =
            "/api/v3/coins/markets?vs_currency=usd&category=stablecoins&per_page=250&page=1&sparkline=false";

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, $"https://api.coingecko.com{endpoint}")
            .ReturnsResponse(HttpStatusCode.BadRequest, "Bad Request");

        // Act
        var result = await _client.GetStablecoinsIds();

        // Assert
        result.Should().BeFailure().Which.Errors.Should().HaveCount(1);
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
                Id = "bitcoin",
                PriceUsd = 68500.45m,
                MarketCapUsd = 1350000000000,
                PriceChangePercentage24h = 1.23m,
            },
            new()
            {
                Id = "ethereum",
                PriceUsd = 3450.22m,
                MarketCapUsd = 420000000000,
                PriceChangePercentage24h = 1.23m,
            },
            new()
            {
                Id = "solana",
                PriceUsd = 145.78m,
                MarketCapUsd = 64000000000,
                PriceChangePercentage24h = 1.23m,
            },
        ];

        public static readonly string CoinsMarketsJsonResponse = JsonSerializer.Serialize(
            CoinsMarkets
        );

        public static readonly List<AssetCoinGecko> ExpectedCoinsMarkets = CoinsMarkets;
        #endregion
    }
}
