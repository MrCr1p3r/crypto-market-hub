using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SVC_Bridge.ApiContracts.Responses.Coins;
using SVC_Bridge.Tests.Integration.Factories;
using WireMock.Admin.Mappings;

namespace SVC_Bridge.Tests.Integration.ApiControllers;

public class TradingPairsControllerTests(CustomWebApplicationFactory factory)
    : BaseIntegrationTest(factory),
        IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task UpdateTradingPairs_WithValidData_ShouldReturnOkWithUpdatedTradingPairs()
    {
        // Arrange
        await Factory.SvcCoinsServerMock.PostMappingAsync(WireMockMappings.SvcCoins.GetAllCoins);
        await Factory.SvcCoinsServerMock.PostMappingAsync(
            WireMockMappings.SvcCoins.CreateQuoteCoins
        );
        await Factory.SvcCoinsServerMock.PostMappingAsync(
            WireMockMappings.SvcCoins.ReplaceTradingPairs
        );
        await Factory.SvcCoinsServerMock.PostMappingAsync(
            WireMockMappings.SvcCoins.DeleteUnreferencedCoins
        );

        await Factory.SvcExternalServerMock.PostMappingAsync(
            WireMockMappings.SvcExternal.GetAllSpotCoins
        );

        // Act
        var response = await Client.PostAsync("/bridge/trading-pairs", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<Coin>>();

        result.Should().NotBeNull();
        result!.Should().HaveCount(2);
        result.Should().Contain(c => c.Symbol == "BTC");
        result.Should().Contain(c => c.Symbol == "ETH");

        var btcCoin = result!.First(c => c.Symbol == "BTC");
        btcCoin.TradingPairs.Should().HaveCount(1);
        btcCoin.TradingPairs.First().CoinQuote.Symbol.Should().Be("USDT");
        btcCoin
            .TradingPairs.First()
            .Exchanges.Should()
            .ContainSingle()
            .Which.Should()
            .Be(SharedLibrary.Enums.Exchange.Binance);
    }

    [Fact]
    public async Task UpdateTradingPairs_WithNoValidSpotCoins_ShouldReturnEmptyResult()
    {
        // Arrange
        await Factory.SvcCoinsServerMock.PostMappingAsync(
            WireMockMappings.SvcCoins.GetAllCoinsWithoutMatchingSpotCoins
        );
        await Factory.SvcCoinsServerMock.PostMappingAsync(
            WireMockMappings.SvcCoins.DeleteUnreferencedCoins
        );

        await Factory.SvcExternalServerMock.PostMappingAsync(
            WireMockMappings.SvcExternal.GetAllSpotCoins
        );

        // Act
        var response = await Client.PostAsync("/bridge/trading-pairs", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<Coin>>();
        result.Should().NotBeNull();
        result!.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateTradingPairs_WhenSvcCoinsFails_ShouldReturnInternalServerError()
    {
        // Arrange
        await Factory.SvcCoinsServerMock.PostMappingAsync(
            WireMockMappings.SvcCoins.GetAllCoinsError
        );

        // Act
        var response = await Client.PostAsync("/bridge/trading-pairs", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task UpdateTradingPairs_WhenSvcExternalFails_ShouldReturnInternalServerError()
    {
        // Arrange
        await Factory.SvcCoinsServerMock.PostMappingAsync(WireMockMappings.SvcCoins.GetAllCoins);
        await Factory.SvcExternalServerMock.PostMappingAsync(
            WireMockMappings.SvcExternal.GetAllSpotCoinsError
        );

        // Act
        var response = await Client.PostAsync("/bridge/trading-pairs", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task UpdateTradingPairs_WhenCreateQuoteCoinsFails_ShouldReturnInternalServerError()
    {
        // Arrange
        await Factory.SvcCoinsServerMock.PostMappingAsync(WireMockMappings.SvcCoins.GetAllCoins);
        await Factory.SvcCoinsServerMock.PostMappingAsync(
            WireMockMappings.SvcCoins.CreateQuoteCoinsError
        );

        await Factory.SvcExternalServerMock.PostMappingAsync(
            WireMockMappings.SvcExternal.GetAllSpotCoins
        );

        // Act
        var response = await Client.PostAsync("/bridge/trading-pairs", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task UpdateTradingPairs_WhenReplaceTradingPairsFails_ShouldReturnInternalServerError()
    {
        // Arrange
        await Factory.SvcCoinsServerMock.PostMappingAsync(WireMockMappings.SvcCoins.GetAllCoins);
        await Factory.SvcCoinsServerMock.PostMappingAsync(
            WireMockMappings.SvcCoins.CreateQuoteCoins
        );
        await Factory.SvcCoinsServerMock.PostMappingAsync(
            WireMockMappings.SvcCoins.ReplaceTradingPairsError
        );

        await Factory.SvcExternalServerMock.PostMappingAsync(
            WireMockMappings.SvcExternal.GetAllSpotCoins
        );

        // Act
        var response = await Client.PostAsync("/bridge/trading-pairs", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task UpdateTradingPairs_WhenDeleteUnreferencedCoinsFails_ShouldReturnInternalServerError()
    {
        // Arrange
        await Factory.SvcCoinsServerMock.PostMappingAsync(WireMockMappings.SvcCoins.GetAllCoins);
        await Factory.SvcCoinsServerMock.PostMappingAsync(
            WireMockMappings.SvcCoins.CreateQuoteCoins
        );
        await Factory.SvcCoinsServerMock.PostMappingAsync(
            WireMockMappings.SvcCoins.ReplaceTradingPairs
        );
        await Factory.SvcCoinsServerMock.PostMappingAsync(
            WireMockMappings.SvcCoins.DeleteUnreferencedCoinsError
        );

        await Factory.SvcExternalServerMock.PostMappingAsync(
            WireMockMappings.SvcExternal.GetAllSpotCoins
        );

        // Act
        var response = await Client.PostAsync("/bridge/trading-pairs", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task UpdateTradingPairs_WithNoTradingPairsToCreate_ShouldReturnEmptyResult()
    {
        // Arrange
        await Factory.SvcCoinsServerMock.PostMappingAsync(
            WireMockMappings.SvcCoins.GetAllCoinsMainOnly
        );
        await Factory.SvcCoinsServerMock.PostMappingAsync(
            WireMockMappings.SvcCoins.DeleteUnreferencedCoins
        );

        await Factory.SvcExternalServerMock.PostMappingAsync(
            WireMockMappings.SvcExternal.GetSpotCoinsWithoutTradingPairs
        );

        // Act
        var response = await Client.PostAsync("/bridge/trading-pairs", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<Coin>>();
        result.Should().NotBeNull();
        result!.Should().BeEmpty();
    }

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
                        Body = JsonSerializer.Serialize(TestData.ExistingCoinsWithTradingPairs),
                    },
                };

            public static MappingModel GetAllCoinsMainOnly =>
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
                        Body = JsonSerializer.Serialize(TestData.ExistingMainCoinsOnly),
                    },
                };

            public static MappingModel GetAllCoinsWithoutMatchingSpotCoins =>
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
                        Body = JsonSerializer.Serialize(TestData.CoinsWithoutMatchingSpotCoins),
                    },
                };

            public static MappingModel CreateQuoteCoins =>
                new()
                {
                    Request = new RequestModel { Methods = ["POST"], Path = "/coins/quote" },
                    Response = new ResponseModel
                    {
                        StatusCode = 200,
                        Headers = new Dictionary<string, object>
                        {
                            ["Content-Type"] = "application/json",
                        },
                        Body = JsonSerializer.Serialize(TestData.CreatedQuoteCoins),
                    },
                };

            public static MappingModel ReplaceTradingPairs =>
                new()
                {
                    Request = new RequestModel { Methods = ["PUT"], Path = "/coins/trading-pairs" },
                    Response = new ResponseModel
                    {
                        StatusCode = 200,
                        Headers = new Dictionary<string, object>
                        {
                            ["Content-Type"] = "application/json",
                        },
                        Body = JsonSerializer.Serialize(TestData.UpdatedCoinsWithTradingPairs),
                    },
                };

            public static MappingModel DeleteUnreferencedCoins =>
                new()
                {
                    Request = new RequestModel
                    {
                        Methods = ["DELETE"],
                        Path = "/coins/unreferenced",
                    },
                    Response = new ResponseModel { StatusCode = 200 },
                };

            public static MappingModel GetAllCoinsError =>
                new()
                {
                    Request = new RequestModel { Methods = ["GET"], Path = "/coins" },
                    Response = new ResponseModel
                    {
                        StatusCode = 500,
                        Headers = new Dictionary<string, object>
                        {
                            ["Content-Type"] = "application/json",
                        },
                        Body = JsonSerializer.Serialize(
                            new
                            {
                                type = "https://datatracker.ietf.org/doc/html/rfc9110#section-15.6.1",
                                title = "Internal Server Error",
                                status = 500,
                                detail = "Coins service unavailable",
                            }
                        ),
                    },
                };

            public static MappingModel CreateQuoteCoinsError =>
                new()
                {
                    Request = new RequestModel { Methods = ["POST"], Path = "/coins/quote" },
                    Response = new ResponseModel
                    {
                        StatusCode = 400,
                        Headers = new Dictionary<string, object>
                        {
                            ["Content-Type"] = "application/json",
                        },
                        Body = JsonSerializer.Serialize(
                            new
                            {
                                type = "some type",
                                title = "Bad Request",
                                status = 400,
                                detail = "Failed to create quote coins",
                            }
                        ),
                    },
                };

            public static MappingModel ReplaceTradingPairsError =>
                new()
                {
                    Request = new RequestModel { Methods = ["PUT"], Path = "/coins/trading-pairs" },
                    Response = new ResponseModel
                    {
                        StatusCode = 400,
                        Headers = new Dictionary<string, object>
                        {
                            ["Content-Type"] = "application/json",
                        },
                        Body = JsonSerializer.Serialize(
                            new
                            {
                                type = "some type",
                                title = "Bad Request",
                                status = 400,
                                detail = "Failed to replace trading pairs",
                            }
                        ),
                    },
                };

            public static MappingModel DeleteUnreferencedCoinsError =>
                new()
                {
                    Request = new RequestModel
                    {
                        Methods = ["DELETE"],
                        Path = "/coins/unreferenced",
                    },
                    Response = new ResponseModel
                    {
                        StatusCode = 500,
                        Headers = new Dictionary<string, object>
                        {
                            ["Content-Type"] = "application/json",
                        },
                        Body = JsonSerializer.Serialize(
                            new
                            {
                                type = "https://datatracker.ietf.org/doc/html/rfc9110#section-15.6.1",
                                title = "Internal Server Error",
                                status = 500,
                                detail = "Failed to delete unreferenced coins",
                            }
                        ),
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
                        Body = JsonSerializer.Serialize(TestData.ExternalSpotCoins),
                    },
                };

            public static MappingModel GetSpotCoinsWithoutTradingPairs =>
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
                        Body = JsonSerializer.Serialize(
                            TestData.ExternalSpotCoinsWithoutTradingPairs
                        ),
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
                    Response = new ResponseModel
                    {
                        StatusCode = 500,
                        Headers = new Dictionary<string, object>
                        {
                            ["Content-Type"] = "application/json",
                        },
                        Body = JsonSerializer.Serialize(
                            new
                            {
                                type = "https://datatracker.ietf.org/doc/html/rfc9110#section-15.6.1",
                                title = "Internal Server Error",
                                status = 500,
                                detail = "External service unavailable",
                            }
                        ),
                    },
                };
        }
    }

    private static class TestData
    {
        public static readonly List<dynamic> ExistingCoinsWithTradingPairs =
        [
            new
            {
                id = 1,
                symbol = "BTC",
                name = "Bitcoin",
                category = (object?)null,
                idCoinGecko = "bitcoin",
                marketCapUsd = 1000000000,
                priceUsd = "50000",
                priceChangePercentage24h = 2.5m,
                tradingPairs = new[]
                {
                    new
                    {
                        id = 1,
                        coinQuote = new
                        {
                            id = 97,
                            symbol = "OLD1",
                            name = "Old Coin 1",
                        },
                        exchanges = new[] { 1 },
                    },
                },
            },
            new
            {
                id = 2,
                symbol = "ETH",
                name = "Ethereum",
                category = (object?)null,
                idCoinGecko = "ethereum",
                marketCapUsd = 500000000,
                priceUsd = "3000",
                priceChangePercentage24h = 1.8m,
                tradingPairs = new[]
                {
                    new
                    {
                        id = 2,
                        coinQuote = new
                        {
                            id = 98,
                            symbol = "OLD2",
                            name = "Old Coin 2",
                        },
                        exchanges = new[] { 2 },
                    },
                },
            },
        ];

        public static readonly List<dynamic> ExistingMainCoinsOnly =
        [
            new
            {
                id = 1,
                symbol = "BTC",
                name = "Bitcoin",
                category = (object?)null,
                idCoinGecko = "bitcoin",
                marketCapUsd = (int?)null,
                priceUsd = (string?)null,
                priceChangePercentage24h = (decimal?)null,
                tradingPairs = new[]
                {
                    new
                    {
                        id = 97,
                        coinQuote = new
                        {
                            id = 97,
                            symbol = "OLD1",
                            name = "Old Coin 1",
                        },
                        exchanges = new[] { 1 },
                    },
                },
            },
            new
            {
                id = 2,
                symbol = "ETH",
                name = "Ethereum",
                category = (object?)null,
                idCoinGecko = "ethereum",
                marketCapUsd = (int?)null,
                priceUsd = (string?)null,
                priceChangePercentage24h = (decimal?)null,
                tradingPairs = new[]
                {
                    new
                    {
                        id = 98,
                        coinQuote = new
                        {
                            id = 98,
                            symbol = "OLD2",
                            name = "Old Coin 2",
                        },
                        exchanges = new[] { 2 },
                    },
                },
            },
        ];

        public static readonly List<dynamic> CoinsWithoutMatchingSpotCoins =
        [
            new
            {
                id = 1,
                symbol = "DIFFERENT",
                name = "Different Coin",
                category = (object?)null,
                idCoinGecko = "different",
                marketCapUsd = (int?)null,
                priceUsd = (string?)null,
                priceChangePercentage24h = (decimal?)null,
                tradingPairs = new[]
                {
                    new
                    {
                        id = 99,
                        coinQuote = new
                        {
                            id = 99,
                            symbol = "OLD",
                            name = "Old Coin",
                        },
                        exchanges = new[] { 1 },
                    },
                },
            },
        ];

        public static readonly List<dynamic> ExternalSpotCoins =
        [
            new
            {
                symbol = "BTC",
                name = "Bitcoin",
                tradingPairs = new[]
                {
                    new
                    {
                        coinQuote = new
                        {
                            symbol = "USDT",
                            name = "Tether",
                            category = 1, // Stablecoin
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
                tradingPairs = new[]
                {
                    new
                    {
                        coinQuote = new
                        {
                            symbol = "USDT",
                            name = "Tether",
                            category = 1, // Stablecoin
                            idCoinGecko = "tether",
                        },
                        exchangeInfos = new[]
                        {
                            new { exchange = 1 }, // Binance
                            new { exchange = 2 }, // Coinbase
                        },
                    },
                },
            },
        ];

        public static readonly List<dynamic> ExternalSpotCoinsWithoutTradingPairs =
        [
            new
            {
                symbol = "BTC",
                name = "Bitcoin",
                tradingPairs = Array.Empty<object>(),
            },
            new
            {
                symbol = "ETH",
                name = "Ethereum",
                tradingPairs = Array.Empty<object>(),
            },
        ];

        public static readonly List<dynamic> CreatedQuoteCoins =
        [
            new
            {
                id = 4,
                symbol = "USDT",
                name = "Tether",
                category = 1, // Stablecoin
                idCoinGecko = "tether",
                marketCapUsd = 50000000,
                priceUsd = "1.0",
                priceChangePercentage24h = 0.01m,
            },
        ];

        public static readonly List<dynamic> UpdatedCoinsWithTradingPairs =
        [
            new
            {
                id = 1,
                symbol = "BTC",
                name = "Bitcoin",
                category = (object?)null,
                idCoinGecko = "bitcoin",
                marketCapUsd = 1000000000,
                priceUsd = "50000",
                priceChangePercentage24h = 2.5m,
                tradingPairs = new[]
                {
                    new
                    {
                        id = 1,
                        coinQuote = new
                        {
                            id = 3,
                            symbol = "USDT",
                            name = "Tether",
                            category = 1, // Stablecoin
                            idCoinGecko = "tether",
                            marketCapUsd = 50000000,
                            priceUsd = "1.0",
                            priceChangePercentage24h = 0.01m,
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
                category = (object?)null,
                idCoinGecko = "ethereum",
                marketCapUsd = 500000000,
                priceUsd = "3000",
                priceChangePercentage24h = 1.8m,
                tradingPairs = new[]
                {
                    new
                    {
                        id = 2,
                        coinQuote = new
                        {
                            id = 3,
                            symbol = "USDT",
                            name = "Tether",
                            category = 1, // Stablecoin
                            idCoinGecko = "tether",
                            marketCapUsd = 50000000,
                            priceUsd = "1.0",
                            priceChangePercentage24h = 0.01m,
                        },
                        exchanges = new[] { 1, 2 }, // Binance, Coinbase
                    },
                },
            },
        ];
    }
}
