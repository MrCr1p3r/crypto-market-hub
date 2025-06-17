using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GUI_Crypto.ApiContracts.Requests.CoinCreation;
using GUI_Crypto.ApiContracts.Responses.CandidateCoin;
using GUI_Crypto.ApiContracts.Responses.OverviewCoin;
using WireMock.Admin.Mappings;

namespace GUI_Crypto.Tests.Integration.Controllers;

[Collection("Controllers Integration Tests")]
public class OverviewControllerTests(CustomWebApplicationFactory factory)
    : BaseControllerIntegrationTest(factory)
{
    #region RenderOverview Tests

    [Fact]
    public async Task RenderOverview_ShouldReturnSuccessStatusCode()
    {
        // Act
        var response = await Client.GetAsync("/overview");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/html");
    }

    #endregion

    #region GetOverviewCoins Tests

    [Fact]
    public async Task GetOverviewCoins_WithValidData_ShouldReturnOkWithOverviewCoins()
    {
        // Arrange
        await Factory.SvcCoinsServerMock.PostMappingAsync(WireMockMappings.SvcCoins.GetAllCoins);
        await Factory.SvcKlineServerMock.PostMappingAsync(
            WireMockMappings.SvcKline.GetAllKlineData
        );

        // Act
        var response = await Client.GetAsync("/coins");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<OverviewCoin>>();

        result.Should().NotBeNull();
        result!.Should().HaveCount(2);

        // Verify first coin (Bitcoin)
        var btcCoin = result!.FirstOrDefault(c => c.Symbol == "BTC");
        btcCoin.Should().NotBeNull();
        btcCoin!.Id.Should().Be(1);
        btcCoin.Name.Should().Be("Bitcoin");
        btcCoin.Category.Should().BeNull(); // Regular cryptocurrency
        btcCoin.MarketCapUsd.Should().Be(1_200_000_000_000);
        btcCoin.PriceUsd.Should().Be("50000.00");
        btcCoin.PriceChangePercentage24h.Should().Be(3.5m);
        btcCoin.TradingPairIds.Should().NotBeEmpty();
        btcCoin.KlineData.Should().NotBeNull();
        btcCoin.KlineData!.TradingPairId.Should().Be(101);
        btcCoin.KlineData.Klines.Should().HaveCount(2);

        // Verify second coin (Ethereum)
        var ethCoin = result!.FirstOrDefault(c => c.Symbol == "ETH");
        ethCoin.Should().NotBeNull();
        ethCoin!.Id.Should().Be(2);
        ethCoin.Name.Should().Be("Ethereum");
        ethCoin.Category.Should().BeNull(); // Regular cryptocurrency
        ethCoin.MarketCapUsd.Should().Be(600_000_000_000);
        ethCoin.PriceUsd.Should().Be("3500.00");
        ethCoin.PriceChangePercentage24h.Should().Be(-1.2m);
        ethCoin.TradingPairIds.Should().NotBeEmpty();
        ethCoin.KlineData.Should().NotBeNull();
        ethCoin.KlineData!.TradingPairId.Should().Be(102);
        ethCoin.KlineData.Klines.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetOverviewCoins_WhenCoinsServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        await Factory.SvcCoinsServerMock.PostMappingAsync(
            WireMockMappings.SvcCoins.GetAllCoinsError
        );
        await Factory.SvcKlineServerMock.PostMappingAsync(
            WireMockMappings.SvcKline.GetAllKlineData
        );

        // Act
        var response = await Client.GetAsync("/coins");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GetOverviewCoins_WhenKlineServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        await Factory.SvcCoinsServerMock.PostMappingAsync(WireMockMappings.SvcCoins.GetAllCoins);
        await Factory.SvcKlineServerMock.PostMappingAsync(
            WireMockMappings.SvcKline.GetAllKlineDataError
        );

        // Act
        var response = await Client.GetAsync("/coins");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GetOverviewCoins_WithNoCoins_ShouldReturnEmptyCollection()
    {
        // Arrange
        await Factory.SvcCoinsServerMock.PostMappingAsync(
            WireMockMappings.SvcCoins.GetAllCoinsEmpty
        );
        await Factory.SvcKlineServerMock.PostMappingAsync(
            WireMockMappings.SvcKline.GetAllKlineDataEmpty
        );

        // Act
        var response = await Client.GetAsync("/coins");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<OverviewCoin>>();

        result.Should().NotBeNull();
        result!.Should().BeEmpty();
    }

    #endregion

    #region GetCandidateCoins Tests

    [Fact]
    public async Task GetCandidateCoins_WithValidData_ShouldReturnOkWithCandidateCoins()
    {
        // Arrange
        await Factory.SvcExternalServerMock.PostMappingAsync(
            WireMockMappings.SvcExternal.GetAllSpotCoins
        );
        await Factory.SvcCoinsServerMock.PostMappingAsync(
            WireMockMappings.SvcCoins.GetAllCoinsForCandidates
        );

        // Act
        var response = await Client.GetAsync("/candidate-coins");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<CandidateCoin>>();

        result.Should().NotBeNull();
        result!.Should().HaveCount(2); // ADA and DOT are candidates

        var adaCoin = result!.FirstOrDefault(c => c.Symbol == "ADA");
        adaCoin.Should().NotBeNull();
        adaCoin!.Id.Should().BeNull(); // New coin
        adaCoin.Name.Should().Be("Cardano");

        var dotCoin = result!.FirstOrDefault(c => c.Symbol == "DOT");
        dotCoin.Should().NotBeNull();
        dotCoin!.Id.Should().BeNull(); // New coin
        dotCoin.Name.Should().Be("Polkadot");
    }

    [Fact]
    public async Task GetCandidateCoins_WhenExternalServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        await Factory.SvcExternalServerMock.PostMappingAsync(
            WireMockMappings.SvcExternal.GetAllSpotCoinsError
        );
        await Factory.SvcCoinsServerMock.PostMappingAsync(
            WireMockMappings.SvcCoins.GetAllCoinsForCandidates
        );

        // Act
        var response = await Client.GetAsync("/candidate-coins");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GetCandidateCoins_WhenAllCoinsAlreadyExist_ShouldReturnEmptyCollection()
    {
        // Arrange
        await Factory.SvcExternalServerMock.PostMappingAsync(
            WireMockMappings.SvcExternal.GetAllSpotCoinsExisting
        );
        await Factory.SvcCoinsServerMock.PostMappingAsync(
            WireMockMappings.SvcCoins.GetAllCoinsForCandidates
        );

        // Act
        var response = await Client.GetAsync("/candidate-coins");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<CandidateCoin>>();

        result.Should().NotBeNull();
        result!.Should().BeEmpty();
    }

    #endregion

    #region CreateCoins Tests

    [Fact]
    public async Task CreateCoins_WithValidData_ShouldReturnOkWithCreatedCoins()
    {
        // Arrange
        await Factory.SvcCoinsServerMock.PostMappingAsync(WireMockMappings.SvcCoins.CreateCoins);

        var coinCreationRequests = TestData.CoinCreationRequests;

        // Act
        var response = await Client.PostAsJsonAsync("/coins", coinCreationRequests);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<OverviewCoin>>();

        result.Should().NotBeNull();
        result!.Should().HaveCount(2);

        var adaCoin = result!.FirstOrDefault(c => c.Symbol == "ADA");
        adaCoin.Should().NotBeNull();
        adaCoin!.Id.Should().Be(3);
        adaCoin.Name.Should().Be("Cardano");
        adaCoin.Category.Should().BeNull();
        adaCoin.MarketCapUsd.Should().Be(15_000_000_000);
        adaCoin.PriceUsd.Should().Be("0.45");
        adaCoin.PriceChangePercentage24h.Should().Be(5.2m);

        var dotCoin = result!.FirstOrDefault(c => c.Symbol == "DOT");
        dotCoin.Should().NotBeNull();
        dotCoin!.Id.Should().Be(4);
        dotCoin.Name.Should().Be("Polkadot");
        dotCoin.Category.Should().BeNull();
        dotCoin.MarketCapUsd.Should().Be(8_000_000_000);
        dotCoin.PriceUsd.Should().Be("7.50");
        dotCoin.PriceChangePercentage24h.Should().Be(-2.1m);
    }

    [Fact]
    public async Task CreateCoins_WhenServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        await Factory.SvcCoinsServerMock.PostMappingAsync(
            WireMockMappings.SvcCoins.CreateCoinsError
        );

        var coinCreationRequests = TestData.CoinCreationRequests;

        // Act
        var response = await Client.PostAsJsonAsync("/coins", coinCreationRequests);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task CreateCoins_WithEmptyRequest_ShouldReturnOkWithEmptyCollection()
    {
        // Arrange
        await Factory.SvcCoinsServerMock.PostMappingAsync(
            WireMockMappings.SvcCoins.CreateCoinsEmpty
        );

        var emptyRequests = Array.Empty<CoinCreationRequest>();

        // Act
        var response = await Client.PostAsJsonAsync("/coins", emptyRequests);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<OverviewCoin>>();

        result.Should().NotBeNull();
        result!.Should().BeEmpty();
    }

    #endregion

    #region DeleteMainCoin Tests

    [Fact]
    public async Task DeleteMainCoin_WithValidId_ShouldReturnNoContent()
    {
        // Arrange
        const int coinId = 1;
        await Factory.SvcCoinsServerMock.PostMappingAsync(WireMockMappings.SvcCoins.DeleteMainCoin);

        // Act
        var response = await Client.DeleteAsync($"/coins/{coinId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteMainCoin_WhenCoinNotFound_ShouldReturnNotFound()
    {
        // Arrange
        const int coinId = 999;
        await Factory.SvcCoinsServerMock.PostMappingAsync(
            WireMockMappings.SvcCoins.DeleteMainCoinNotFound
        );

        // Act
        var response = await Client.DeleteAsync($"/coins/{coinId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteMainCoin_WhenServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        const int coinId = 1;
        await Factory.SvcCoinsServerMock.PostMappingAsync(
            WireMockMappings.SvcCoins.DeleteMainCoinError
        );

        // Act
        var response = await Client.DeleteAsync($"/coins/{coinId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    #endregion

    #region DeleteAllCoins Tests

    [Fact]
    public async Task DeleteAllCoins_WithValidRequest_ShouldReturnNoContent()
    {
        // Arrange
        await Factory.SvcCoinsServerMock.PostMappingAsync(WireMockMappings.SvcCoins.DeleteAllCoins);

        // Act
        var response = await Client.DeleteAsync("/coins");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteAllCoins_WhenServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        await Factory.SvcCoinsServerMock.PostMappingAsync(
            WireMockMappings.SvcCoins.DeleteAllCoinsError
        );

        // Act
        var response = await Client.DeleteAsync("/coins");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    #endregion

    private static class WireMockMappings
    {
        public static class SvcCoins
        {
            public static MappingModel GetAllCoins =>
                new()
                {
                    Request = new RequestModel { Methods = ["GET"], Path = "/coins" },
                    Response = new ResponseModel
                    {
                        StatusCode = 200,
                        Headers = new Dictionary<string, object>
                        {
                            ["Content-Type"] = "application/json",
                        },
                        Body = JsonSerializer.Serialize(TestData.SvcCoinsResponse),
                    },
                };

            public static MappingModel GetAllCoinsError =>
                new()
                {
                    Request = new RequestModel { Methods = ["GET"], Path = "/coins" },
                    Response = new ResponseModel { StatusCode = 500 },
                };

            public static MappingModel GetAllCoinsEmpty =>
                new()
                {
                    Request = new RequestModel { Methods = ["GET"], Path = "/coins" },
                    Response = new ResponseModel
                    {
                        StatusCode = 200,
                        Headers = new Dictionary<string, object>
                        {
                            ["Content-Type"] = "application/json",
                        },
                        Body = JsonSerializer.Serialize(Array.Empty<object>()),
                    },
                };

            public static MappingModel GetAllCoinsForCandidates =>
                new()
                {
                    Request = new RequestModel { Methods = ["GET"], Path = "/coins" },
                    Response = new ResponseModel
                    {
                        StatusCode = 200,
                        Headers = new Dictionary<string, object>
                        {
                            ["Content-Type"] = "application/json",
                        },
                        Body = JsonSerializer.Serialize(TestData.SvcCoinsExistingCoins),
                    },
                };

            public static MappingModel CreateCoins =>
                new()
                {
                    Request = new RequestModel { Methods = ["POST"], Path = "/coins" },
                    Response = new ResponseModel
                    {
                        StatusCode = 200,
                        Headers = new Dictionary<string, object>
                        {
                            ["Content-Type"] = "application/json",
                        },
                        Body = JsonSerializer.Serialize(TestData.SvcCoinsCreatedCoins),
                    },
                };

            public static MappingModel CreateCoinsError =>
                new()
                {
                    Request = new RequestModel { Methods = ["POST"], Path = "/coins" },
                    Response = new ResponseModel { StatusCode = 500 },
                };

            public static MappingModel CreateCoinsEmpty =>
                new()
                {
                    Request = new RequestModel { Methods = ["POST"], Path = "/coins" },
                    Response = new ResponseModel
                    {
                        StatusCode = 200,
                        Headers = new Dictionary<string, object>
                        {
                            ["Content-Type"] = "application/json",
                        },
                        Body = JsonSerializer.Serialize(Array.Empty<object>()),
                    },
                };

            public static MappingModel DeleteMainCoin =>
                new()
                {
                    Request = new RequestModel { Methods = ["DELETE"], Path = "/coins/1" },
                    Response = new ResponseModel { StatusCode = 204 },
                };

            public static MappingModel DeleteMainCoinNotFound =>
                new()
                {
                    Request = new RequestModel { Methods = ["DELETE"], Path = "/coins/999" },
                    Response = new ResponseModel
                    {
                        StatusCode = 404,
                        Headers = new Dictionary<string, object>
                        {
                            ["Content-Type"] = "application/json",
                        },
                        Body = JsonSerializer.Serialize(
                            new
                            {
                                type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                                title = "Not Found",
                                status = 404,
                                detail = "Coin with ID 999 not found.",
                                instance = "/coins/999",
                            }
                        ),
                    },
                };

            public static MappingModel DeleteMainCoinError =>
                new()
                {
                    Request = new RequestModel { Methods = ["DELETE"], Path = "/coins/1" },
                    Response = new ResponseModel { StatusCode = 500 },
                };

            public static MappingModel DeleteAllCoins =>
                new()
                {
                    Request = new RequestModel { Methods = ["DELETE"], Path = "/coins" },
                    Response = new ResponseModel { StatusCode = 204 },
                };

            public static MappingModel DeleteAllCoinsError =>
                new()
                {
                    Request = new RequestModel { Methods = ["DELETE"], Path = "/coins" },
                    Response = new ResponseModel { StatusCode = 500 },
                };
        }

        public static class SvcKline
        {
            public static MappingModel GetAllKlineData =>
                new()
                {
                    Request = new RequestModel { Methods = ["GET"], Path = "/kline" },
                    Response = new ResponseModel
                    {
                        StatusCode = 200,
                        Headers = new Dictionary<string, object>
                        {
                            ["Content-Type"] = "application/json",
                        },
                        Body = JsonSerializer.Serialize(TestData.SvcKlineResponse),
                    },
                };

            public static MappingModel GetAllKlineDataError =>
                new()
                {
                    Request = new RequestModel { Methods = ["GET"], Path = "/kline" },
                    Response = new ResponseModel { StatusCode = 500 },
                };

            public static MappingModel GetAllKlineDataEmpty =>
                new()
                {
                    Request = new RequestModel { Methods = ["GET"], Path = "/kline" },
                    Response = new ResponseModel
                    {
                        StatusCode = 200,
                        Headers = new Dictionary<string, object>
                        {
                            ["Content-Type"] = "application/json",
                        },
                        Body = JsonSerializer.Serialize(Array.Empty<object>()),
                    },
                };
        }

        public static class SvcExternal
        {
            public static MappingModel GetAllSpotCoins =>
                new()
                {
                    Request = new RequestModel
                    {
                        Methods = ["GET"],
                        Path = "/exchanges/coins/spot",
                    },
                    Response = new ResponseModel
                    {
                        StatusCode = 200,
                        Headers = new Dictionary<string, object>
                        {
                            ["Content-Type"] = "application/json",
                        },
                        Body = JsonSerializer.Serialize(TestData.SvcExternalSpotCoins),
                    },
                };

            public static MappingModel GetAllSpotCoinsError =>
                new()
                {
                    Request = new RequestModel
                    {
                        Methods = ["GET"],
                        Path = "/exchanges/coins/spot",
                    },
                    Response = new ResponseModel { StatusCode = 500 },
                };

            public static MappingModel GetAllSpotCoinsExisting =>
                new()
                {
                    Request = new RequestModel
                    {
                        Methods = ["GET"],
                        Path = "/exchanges/coins/spot",
                    },
                    Response = new ResponseModel
                    {
                        StatusCode = 200,
                        Headers = new Dictionary<string, object>
                        {
                            ["Content-Type"] = "application/json",
                        },
                        Body = JsonSerializer.Serialize(TestData.SvcExternalExistingSpotCoins),
                    },
                };
        }
    }

    private static class TestData
    {
        public static readonly List<dynamic> SvcCoinsResponse =
        [
            new
            {
                id = 1,
                symbol = "BTC",
                name = "Bitcoin",
                category = (string?)null,
                marketCapUsd = 1_200_000_000_000L,
                priceUsd = "50000.00",
                priceChangePercentage24h = 3.5m,
                idCoinGecko = "bitcoin",
                tradingPairs = new[]
                {
                    new
                    {
                        id = 101,
                        coinQuote = new
                        {
                            id = 5,
                            symbol = "USDT",
                            name = "Tether",
                        },
                        exchanges = new[] { 1 }, // Binance
                    },
                },
            },
            new
            {
                id = 2,
                symbol = "ETH",
                name = "Ethereum",
                category = (string?)null,
                marketCapUsd = 600_000_000_000L,
                priceUsd = "3500.00",
                priceChangePercentage24h = -1.2m,
                idCoinGecko = "ethereum",
                tradingPairs = new[]
                {
                    new
                    {
                        id = 102,
                        coinQuote = new
                        {
                            id = 5,
                            symbol = "USDT",
                            name = "Tether",
                        },
                        exchanges = new[] { 1 }, // Binance
                    },
                },
            },
        ];

        public static readonly List<dynamic> SvcKlineResponse =
        [
            new
            {
                idTradingPair = 101,
                klineData = new[]
                {
                    new
                    {
                        openTime = 1640995200000L,
                        openPrice = 46000.50m,
                        highPrice = 47000.75m,
                        lowPrice = 45500.25m,
                        closePrice = 46800.00m,
                        volume = 123.456m,
                        closeTime = 1640998800000L,
                    },
                    new
                    {
                        openTime = 1640998800000L,
                        openPrice = 46800.00m,
                        highPrice = 48000.00m,
                        lowPrice = 46500.00m,
                        closePrice = 47500.50m,
                        volume = 234.567m,
                        closeTime = 1641002400000L,
                    },
                },
            },
            new
            {
                idTradingPair = 102,
                klineData = new[]
                {
                    new
                    {
                        openTime = 1640995200000L,
                        openPrice = 3000.00m,
                        highPrice = 3100.00m,
                        lowPrice = 2900.00m,
                        closePrice = 3050.00m,
                        volume = 200.000m,
                        closeTime = 1640998800000L,
                    },
                    new
                    {
                        openTime = 1640998800000L,
                        openPrice = 3050.00m,
                        highPrice = 3200.00m,
                        lowPrice = 3000.00m,
                        closePrice = 3150.00m,
                        volume = 250.000m,
                        closeTime = 1641002400000L,
                    },
                },
            },
        ];

        public static readonly List<dynamic> SvcCoinsExistingCoins =
        [
            new
            {
                id = 1,
                symbol = "BTC",
                name = "Bitcoin",
                category = (string?)null,
                marketCapUsd = 1_200_000_000_000L,
                priceUsd = "50000.00",
                priceChangePercentage24h = 3.5m,
                tradingPairs = new[]
                {
                    new
                    {
                        id = 101,
                        coinQuote = new
                        {
                            id = 5,
                            symbol = "USDT",
                            name = "Tether",
                        },
                        exchanges = new[] { 1 }, // Binance
                    },
                },
            },
            new
            {
                id = 2,
                symbol = "ETH",
                name = "Ethereum",
                category = (string?)null,
                marketCapUsd = 600_000_000_000L,
                priceUsd = "3500.00",
                priceChangePercentage24h = -1.2m,
                tradingPairs = new[]
                {
                    new
                    {
                        id = 102,
                        coinQuote = new
                        {
                            id = 5,
                            symbol = "USDT",
                            name = "Tether",
                        },
                        exchanges = new[] { 1 }, // Binance
                    },
                },
            },
        ];

        public static readonly List<dynamic> SvcExternalSpotCoins =
        [
            new
            {
                symbol = "BTC",
                name = "Bitcoin",
                category = (string?)null,
                idCoinGecko = "bitcoin",
                tradingPairs = new[]
                {
                    new
                    {
                        coinQuote = new
                        {
                            symbol = "USDT",
                            name = "Tether",
                            category = 0, // Stablecoin category
                            idCoinGecko = "tether",
                        },
                        exchangeInfos = new[] { new { exchange = 1 } }, // Binance
                    },
                },
            },
            new
            {
                symbol = "ETH",
                name = "Ethereum",
                category = (string?)null,
                idCoinGecko = "ethereum",
                tradingPairs = new[]
                {
                    new
                    {
                        coinQuote = new
                        {
                            symbol = "USDT",
                            name = "Tether",
                            category = 0, // Stablecoin category
                            idCoinGecko = "tether",
                        },
                        exchangeInfos = new[] { new { exchange = 1 } }, // Binance
                    },
                },
            },
            new
            {
                symbol = "ADA",
                name = "Cardano",
                category = (string?)null,
                idCoinGecko = "cardano",
                tradingPairs = new[]
                {
                    new
                    {
                        coinQuote = new
                        {
                            symbol = "USDT",
                            name = "Tether",
                            category = 0, // Stablecoin category
                            idCoinGecko = "tether",
                        },
                        exchangeInfos = new[] { new { exchange = 1 } }, // Binance
                    },
                },
            },
            new
            {
                symbol = "DOT",
                name = "Polkadot",
                category = (string?)null,
                idCoinGecko = "polkadot",
                tradingPairs = new[]
                {
                    new
                    {
                        coinQuote = new
                        {
                            symbol = "USDT",
                            name = "Tether",
                            category = 0, // Stablecoin category
                            idCoinGecko = "tether",
                        },
                        exchangeInfos = new[] { new { exchange = 2 } }, // Bybit
                    },
                },
            },
        ];

        public static readonly List<dynamic> SvcExternalExistingSpotCoins =
        [
            new
            {
                symbol = "BTC",
                name = "Bitcoin",
                category = (string?)null,
                tradingPairs = Array.Empty<object>(),
            },
            new
            {
                symbol = "ETH",
                name = "Ethereum",
                category = (string?)null,
                tradingPairs = Array.Empty<object>(),
            },
        ];

        public static readonly List<CoinCreationRequest> CoinCreationRequests =
        [
            new CoinCreationRequest
            {
                Id = null,
                Symbol = "ADA",
                Name = "Cardano",
                Category = null,
                IdCoinGecko = "cardano",
                TradingPairs = [],
            },
            new CoinCreationRequest
            {
                Id = null,
                Symbol = "DOT",
                Name = "Polkadot",
                Category = null,
                IdCoinGecko = "polkadot",
                TradingPairs = [],
            },
        ];

        public static readonly List<dynamic> SvcCoinsCreatedCoins =
        [
            new
            {
                id = 3,
                symbol = "ADA",
                name = "Cardano",
                category = (string?)null,
                marketCapUsd = 15_000_000_000L,
                priceUsd = "0.45",
                priceChangePercentage24h = 5.2m,
                tradingPairs = Array.Empty<object>(),
            },
            new
            {
                id = 4,
                symbol = "DOT",
                name = "Polkadot",
                category = (string?)null,
                marketCapUsd = 8_000_000_000L,
                priceUsd = "7.50",
                priceChangePercentage24h = -2.1m,
                tradingPairs = Array.Empty<object>(),
            },
        ];
    }
}
