using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SharedLibrary.Extensions.Testing;
using SVC_Bridge.ApiContracts.Responses;
using SVC_Bridge.Tests.Integration.Factories;
using WireMock.Admin.Mappings;

namespace SVC_Bridge.Tests.Integration.ApiControllers;

public class CoinsControllerTests(CustomWebApplicationFactory factory)
    : BaseIntegrationTest(factory),
        IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task UpdateCoinsMarketData_WithValidData_ShouldReturnOkWithUpdatedData()
    {
        // Arrange
        await Factory.SvcCoinsServerMock.PostMappingsAsync(
            [WireMockMappings.SvcCoins.GetAllCoins, WireMockMappings.SvcCoins.UpdateCoinsMarketData]
        );
        await Factory.SvcExternalServerMock.PostMappingAsync(
            WireMockMappings.SvcExternal.GetCoinGeckoAssetsInfo
        );

        // Act
        var response = await Client.PostAsync("/bridge/coins/market-data", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<CoinMarketData>>();

        result.Should().NotBeNull();
        result!.Should().HaveCount(2);

        var bitcoinData = result!.FirstOrDefault(coinData => coinData.Id == 1);
        var ethereumData = result!.FirstOrDefault(coinData => coinData.Id == 2);

        bitcoinData.Should().NotBeNull();
        bitcoinData!.MarketCapUsd.Should().Be(1000000);
        bitcoinData.PriceUsd.Should().Be("50000");
        bitcoinData.PriceChangePercentage24h.Should().Be(2.5m);

        ethereumData.Should().NotBeNull();
        ethereumData!.MarketCapUsd.Should().Be(500000);
        ethereumData.PriceUsd.Should().Be("3000");
        ethereumData.PriceChangePercentage24h.Should().Be(-1.2m);
    }

    [Fact]
    public async Task UpdateCoinsMarketData_WithNoCoinsHavingCoinGeckoIds_ShouldReturnEmptyResult()
    {
        // Arrange
        await Factory.SvcCoinsServerMock.PostMappingAsync(
            WireMockMappings.SvcCoins.GetAllCoinsWithNoCoinGeckoIds
        );

        // Act
        var response = await Client.PostAsync("/bridge/coins/market-data", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<CoinMarketData>>();

        result.Should().NotBeNull();
        result!.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateCoinsMarketData_WhenSvcExternalFails_ShouldReturnInternalServerError()
    {
        // Arrange
        await Factory.SvcCoinsServerMock.PostMappingAsync(WireMockMappings.SvcCoins.GetAllCoins);
        await Factory.SvcExternalServerMock.PostMappingAsync(
            WireMockMappings.SvcExternal.GetCoinGeckoAssetsInfoError
        );

        // Act
        var response = await Client.PostAsync("/bridge/coins/market-data", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task UpdateCoinsMarketData_WhenSvcCoinsUpdateFails_ShouldReturnInternalServerError()
    {
        // Arrange
        await Factory.SvcCoinsServerMock.PostMappingAsync(WireMockMappings.SvcCoins.GetAllCoins);
        await Factory.SvcExternalServerMock.PostMappingAsync(
            WireMockMappings.SvcExternal.GetCoinGeckoAssetsInfo
        );
        await Factory.SvcCoinsServerMock.PostMappingAsync(
            WireMockMappings.SvcCoins.UpdateCoinsMarketDataError
        );

        // Act
        var response = await Client.PostAsync("/bridge/coins/market-data", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task UpdateCoinsMarketData_WithPartialMarketData_ShouldReturnOnlyMatchingCoins()
    {
        // Arrange
        await Factory.SvcCoinsServerMock.PostMappingAsync(WireMockMappings.SvcCoins.GetAllCoins);
        await Factory.SvcExternalServerMock.PostMappingAsync(
            WireMockMappings.SvcExternal.GetPartialCoinGeckoAssetsInfo
        );
        await Factory.SvcCoinsServerMock.PostMappingAsync(
            WireMockMappings.SvcCoins.UpdateSingleCoinMarketData
        );

        // Act
        var response = await Client.PostAsync("/bridge/coins/market-data", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<CoinMarketData>>();

        result.Should().NotBeNull();
        result!.Should().HaveCount(1);
        result![0].Id.Should().Be(1); // Only Bitcoin should be updated
    }

    [Fact]
    public async Task UpdateCoinsMarketData_WithNoMatchingMarketData_ShouldReturnEmptyResult()
    {
        // Arrange
        await Factory.SvcCoinsServerMock.PostMappingAsync(WireMockMappings.SvcCoins.GetAllCoins);
        await Factory.SvcExternalServerMock.PostMappingAsync(
            WireMockMappings.SvcExternal.GetEmptyCoinGeckoAssetsInfo
        );

        // Act
        var response = await Client.PostAsync("/bridge/coins/market-data", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<CoinMarketData>>();

        result.Should().NotBeNull();
        result!.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateCoinsMarketData_WhenSvcCoinsFails_ShouldReturnInternalServerError()
    {
        // Arrange
        await Factory.SvcCoinsServerMock.PostMappingAsync(
            WireMockMappings.SvcCoins.GetAllCoinsError
        );

        // Act
        var response = await Client.PostAsync("/bridge/coins/market-data", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
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
                        Body = JsonSerializer.Serialize(TestData.CoinsWithCoinGeckoIds),
                    },
                };

            public static MappingModel GetAllCoinsWithNoCoinGeckoIds =>
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
                        Body = JsonSerializer.Serialize(TestData.CoinsWithNoCoinGeckoIds),
                    },
                };

            public static MappingModel UpdateCoinsMarketData =>
                new()
                {
                    Request = new RequestModel { Methods = ["PATCH"], Path = "/coins/market-data" },
                    Response = new ResponseModel
                    {
                        StatusCode = 200,
                        Headers = new Dictionary<string, object>
                        {
                            ["Content-Type"] = "application/json",
                        },
                        Body = JsonSerializer.Serialize(TestData.UpdatedCoins),
                    },
                };

            public static MappingModel UpdateSingleCoinMarketData =>
                new()
                {
                    Request = new RequestModel { Methods = ["PATCH"], Path = "/coins/market-data" },
                    Response = new ResponseModel
                    {
                        StatusCode = 200,
                        Headers = new Dictionary<string, object>
                        {
                            ["Content-Type"] = "application/json",
                        },
                        Body = JsonSerializer.Serialize(TestData.UpdatedSingleCoin),
                    },
                };

            public static MappingModel UpdateCoinsMarketDataError =>
                new()
                {
                    Request = new RequestModel { Methods = ["PATCH"], Path = "/coins/market-data" },
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
                                detail = "Validation failed",
                            }
                        ),
                    },
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
                                detail = "External service unavailable",
                            }
                        ),
                    },
                };
        }

        public static class SvcExternal
        {
            public static MappingModel GetCoinGeckoAssetsInfo =>
                new()
                {
                    Request = new RequestModel
                    {
                        Methods = ["GET"],
                        Path = "/market-data-providers/coingecko/assets-info",
                        Params = [WireMockParamBuilder.WithNotNullOrEmptyMatch("coinGeckoIds")],
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

            public static MappingModel GetPartialCoinGeckoAssetsInfo =>
                new()
                {
                    Request = new RequestModel
                    {
                        Methods = ["GET"],
                        Path = "/market-data-providers/coingecko/assets-info",
                        Params = [WireMockParamBuilder.WithNotNullOrEmptyMatch("coinGeckoIds")],
                    },
                    Response = new ResponseModel
                    {
                        StatusCode = 200,
                        Headers = new Dictionary<string, object>
                        {
                            ["Content-Type"] = "application/json",
                        },
                        Body = JsonSerializer.Serialize(TestData.PartialCoinGeckoMarketData),
                    },
                };

            public static MappingModel GetEmptyCoinGeckoAssetsInfo =>
                new()
                {
                    Request = new RequestModel
                    {
                        Methods = ["GET"],
                        Path = "/market-data-providers/coingecko/assets-info",
                        Params = [WireMockParamBuilder.WithNotNullOrEmptyMatch("coinGeckoIds")],
                    },
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

            public static MappingModel GetCoinGeckoAssetsInfoError =>
                new()
                {
                    Request = new RequestModel
                    {
                        Methods = ["GET"],
                        Path = "/market-data-providers/coingecko/assets-info",
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
        public static readonly List<dynamic> CoinsWithCoinGeckoIds =
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
                tradingPairs = Array.Empty<object>(),
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
                tradingPairs = Array.Empty<object>(),
            },
        ];

        public static readonly List<dynamic> CoinsWithNoCoinGeckoIds =
        [
            new
            {
                id = 1,
                symbol = "BTC",
                name = "Bitcoin",
                category = (object?)null,
                idCoinGecko = (string?)null,
                marketCapUsd = (int?)null,
                priceUsd = (string?)null,
                priceChangePercentage24h = (decimal?)null,
                tradingPairs = Array.Empty<object>(),
            },
            new
            {
                id = 2,
                symbol = "ETH",
                name = "Ethereum",
                category = (object?)null,
                idCoinGecko = string.Empty,
                marketCapUsd = (int?)null,
                priceUsd = (string?)null,
                priceChangePercentage24h = (decimal?)null,
                tradingPairs = Array.Empty<object>(),
            },
        ];

        public static readonly List<dynamic> CoinGeckoMarketData =
        [
            new
            {
                id = "bitcoin",
                marketCapUsd = 1000000L,
                priceUsd = 50000m,
                priceChangePercentage24h = 2.5m,
                isStablecoin = false,
            },
            new
            {
                id = "ethereum",
                marketCapUsd = 500000L,
                priceUsd = 3000m,
                priceChangePercentage24h = -1.2m,
                isStablecoin = false,
            },
        ];

        public static readonly List<dynamic> PartialCoinGeckoMarketData =
        [
            new
            {
                id = "bitcoin",
                marketCapUsd = 1000000L,
                priceUsd = 50000m,
                priceChangePercentage24h = 2.5m,
                isStablecoin = false,
            },
        ];

        public static readonly List<dynamic> UpdatedCoins =
        [
            new
            {
                id = 1,
                symbol = "BTC",
                name = "Bitcoin",
                category = (object?)null,
                idCoinGecko = "bitcoin",
                marketCapUsd = 1000000,
                priceUsd = "50000",
                priceChangePercentage24h = 2.5m,
                tradingPairs = Array.Empty<object>(),
            },
            new
            {
                id = 2,
                symbol = "ETH",
                name = "Ethereum",
                category = (object?)null,
                idCoinGecko = "ethereum",
                marketCapUsd = 500000,
                priceUsd = "3000",
                priceChangePercentage24h = -1.2m,
                tradingPairs = Array.Empty<object>(),
            },
        ];

        public static readonly List<dynamic> UpdatedSingleCoin =
        [
            new
            {
                id = 1,
                symbol = "BTC",
                name = "Bitcoin",
                category = (object?)null,
                idCoinGecko = "bitcoin",
                marketCapUsd = 1000000,
                priceUsd = "50000",
                priceChangePercentage24h = 2.5m,
                tradingPairs = Array.Empty<object>(),
            },
        ];
    }
}
