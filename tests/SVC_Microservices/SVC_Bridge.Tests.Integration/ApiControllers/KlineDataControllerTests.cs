using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SVC_Bridge.ApiContracts.Responses.KlineData;
using SVC_Bridge.Tests.Integration.Factories;
using WireMock.Admin.Mappings;

namespace SVC_Bridge.Tests.Integration.ApiControllers;

public class KlineDataControllerTests(CustomWebApplicationFactory factory)
    : BaseIntegrationTest(factory),
        IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task UpdateKlineData_WithValidData_ShouldReturnOkWithUpdatedData()
    {
        // Arrange
        await Factory.SvcCoinsServerMock.PostMappingsAsync(
            [WireMockMappings.SvcCoins.GetAllCoinsWithTradingPairs]
        );
        await Factory.SvcExternalServerMock.PostMappingAsync(
            WireMockMappings.SvcExternal.GetKlineData
        );
        await Factory.SvcKlineServerMock.PostMappingAsync(
            WireMockMappings.SvcKline.ReplaceKlineData
        );

        // Act
        var response = await Client.PostAsync("/bridge/kline", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<KlineDataResponse>>();

        result.Should().NotBeNull();
        result!.Should().HaveCount(2);

        var firstTradingPairData = result!.FirstOrDefault(data => data.IdTradingPair == 1);
        var secondTradingPairData = result!.FirstOrDefault(data => data.IdTradingPair == 2);

        firstTradingPairData.Should().NotBeNull();
        firstTradingPairData!.Klines.Should().HaveCount(2);
        firstTradingPairData.Klines.First().OpenPrice.Should().Be("47000");
        firstTradingPairData.Klines.First().ClosePrice.Should().Be("47500");

        secondTradingPairData.Should().NotBeNull();
        secondTradingPairData!.Klines.Should().HaveCount(2);
        secondTradingPairData.Klines.First().OpenPrice.Should().Be("3700");
        secondTradingPairData.Klines.First().ClosePrice.Should().Be("3750");
    }

    [Fact]
    public async Task UpdateKlineData_WithNoCoinsHavingTradingPairs_ShouldReturnEmptyResult()
    {
        // Arrange
        await Factory.SvcCoinsServerMock.PostMappingAsync(
            WireMockMappings.SvcCoins.GetAllCoinsWithNoTradingPairs
        );

        // Act
        var response = await Client.PostAsync("/bridge/kline", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<KlineDataResponse>>();

        result.Should().NotBeNull();
        result!.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateKlineData_WhenSvcExternalFails_ShouldReturnInternalServerError()
    {
        // Arrange
        await Factory.SvcCoinsServerMock.PostMappingAsync(
            WireMockMappings.SvcCoins.GetAllCoinsWithTradingPairs
        );
        await Factory.SvcExternalServerMock.PostMappingAsync(
            WireMockMappings.SvcExternal.GetKlineDataError
        );

        // Act
        var response = await Client.PostAsync("/bridge/kline", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task UpdateKlineData_WhenSvcKlineFails_ShouldReturnInternalServerError()
    {
        // Arrange
        await Factory.SvcCoinsServerMock.PostMappingAsync(
            WireMockMappings.SvcCoins.GetAllCoinsWithTradingPairs
        );
        await Factory.SvcExternalServerMock.PostMappingAsync(
            WireMockMappings.SvcExternal.GetKlineData
        );
        await Factory.SvcKlineServerMock.PostMappingAsync(
            WireMockMappings.SvcKline.ReplaceKlineDataError
        );

        // Act
        var response = await Client.PostAsync("/bridge/kline", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task UpdateKlineData_WithPartialKlineData_ShouldReturnOnlyAvailableData()
    {
        // Arrange
        await Factory.SvcCoinsServerMock.PostMappingAsync(
            WireMockMappings.SvcCoins.GetAllCoinsWithTradingPairs
        );
        await Factory.SvcExternalServerMock.PostMappingAsync(
            WireMockMappings.SvcExternal.GetPartialKlineData
        );
        await Factory.SvcKlineServerMock.PostMappingAsync(
            WireMockMappings.SvcKline.ReplacePartialKlineData
        );

        // Act
        var response = await Client.PostAsync("/bridge/kline", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<KlineDataResponse>>();

        result.Should().NotBeNull();
        result!.Should().HaveCount(1);
        result![0].IdTradingPair.Should().Be(1); // Only first trading pair should have data
    }

    [Fact]
    public async Task UpdateKlineData_WithNoKlineDataReturned_ShouldReturnEmptyResult()
    {
        // Arrange
        await Factory.SvcCoinsServerMock.PostMappingAsync(
            WireMockMappings.SvcCoins.GetAllCoinsWithTradingPairs
        );
        await Factory.SvcExternalServerMock.PostMappingAsync(
            WireMockMappings.SvcExternal.GetEmptyKlineData
        );

        // Act
        var response = await Client.PostAsync("/bridge/kline", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<KlineDataResponse>>();

        result.Should().NotBeNull();
        result!.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateKlineData_WhenSvcCoinsFails_ShouldReturnInternalServerError()
    {
        // Arrange
        await Factory.SvcCoinsServerMock.PostMappingAsync(
            WireMockMappings.SvcCoins.GetAllCoinsError
        );

        // Act
        var response = await Client.PostAsync("/bridge/kline", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    private static class WireMockMappings
    {
        public static class SvcCoins
        {
            public static MappingModel GetAllCoinsWithTradingPairs =>
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
                        Body = JsonSerializer.Serialize(TestData.CoinsWithTradingPairs),
                    },
                };

            public static MappingModel GetAllCoinsWithNoTradingPairs =>
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
                        Body = JsonSerializer.Serialize(TestData.CoinsWithNoTradingPairs),
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
                                detail = "Coins service unavailable",
                            }
                        ),
                    },
                };
        }

        public static class SvcExternal
        {
            public static MappingModel GetKlineData =>
                new()
                {
                    Request = new RequestModel
                    {
                        Methods = ["POST"],
                        Path = "/exchanges/kline/query/bulk",
                    },
                    Response = new ResponseModel
                    {
                        StatusCode = 200,
                        Headers = new Dictionary<string, object>
                        {
                            ["Content-Type"] = "application/json",
                        },
                        Body = JsonSerializer.Serialize(TestData.ExternalKlineDataResponse),
                    },
                };

            public static MappingModel GetPartialKlineData =>
                new()
                {
                    Request = new RequestModel
                    {
                        Methods = ["POST"],
                        Path = "/exchanges/kline/query/bulk",
                    },
                    Response = new ResponseModel
                    {
                        StatusCode = 200,
                        Headers = new Dictionary<string, object>
                        {
                            ["Content-Type"] = "application/json",
                        },
                        Body = JsonSerializer.Serialize(TestData.PartialExternalKlineDataResponse),
                    },
                };

            public static MappingModel GetEmptyKlineData =>
                new()
                {
                    Request = new RequestModel
                    {
                        Methods = ["POST"],
                        Path = "/exchanges/kline/query/bulk",
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

            public static MappingModel GetKlineDataError =>
                new()
                {
                    Request = new RequestModel { Methods = ["POST"], Path = "/kline" },
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
                                detail = "External kline data service unavailable",
                            }
                        ),
                    },
                };
        }

        public static class SvcKline
        {
            public static MappingModel ReplaceKlineData =>
                new()
                {
                    Request = new RequestModel { Methods = ["PUT"] },
                    Response = new ResponseModel
                    {
                        StatusCode = 200,
                        Headers = new Dictionary<string, object>
                        {
                            ["Content-Type"] = "application/json",
                        },
                        Body = JsonSerializer.Serialize(TestData.KlineServiceResponse),
                    },
                };

            public static MappingModel ReplacePartialKlineData =>
                new()
                {
                    Request = new RequestModel { Methods = ["PUT"] },
                    Response = new ResponseModel
                    {
                        StatusCode = 200,
                        Headers = new Dictionary<string, object>
                        {
                            ["Content-Type"] = "application/json",
                        },
                        Body = JsonSerializer.Serialize(TestData.PartialKlineServiceResponse),
                    },
                };

            public static MappingModel ReplaceKlineDataError =>
                new()
                {
                    Request = new RequestModel { Methods = ["PUT"] },
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
                                detail = "Kline data validation failed",
                            }
                        ),
                    },
                };
        }
    }

    private static class TestData
    {
        public static readonly List<dynamic> CoinsWithTradingPairs =
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
                        id = 1,
                        coinQuote = new
                        {
                            id = 3,
                            symbol = "USDT",
                            name = "Tether",
                        },
                        exchanges = new[] { 1, 2 }, // Binance, Coinbase
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
                        id = 2,
                        coinQuote = new
                        {
                            id = 3,
                            symbol = "USDT",
                            name = "Tether",
                        },
                        exchanges = new[] { 1 }, // Binance
                    },
                },
            },
        ];

        public static readonly List<dynamic> CoinsWithNoTradingPairs =
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

        public static readonly List<dynamic> ExternalKlineDataResponse =
        [
            new
            {
                idTradingPair = 1,
                klines = new[]
                {
                    new
                    {
                        openTime = 1640995200000L, // 2022-01-01 00:00:00 UTC
                        openPrice = "47000",
                        highPrice = "48000",
                        lowPrice = "46000",
                        closePrice = "47500",
                        volume = "1000",
                        closeTime = 1641081599999L, // 2022-01-01 23:59:59 UTC
                    },
                    new
                    {
                        openTime = 1641081600000L, // 2022-01-02 00:00:00 UTC
                        openPrice = "47500",
                        highPrice = "49000",
                        lowPrice = "47000",
                        closePrice = "48500",
                        volume = "1200",
                        closeTime = 1641167999999L, // 2022-01-02 23:59:59 UTC
                    },
                },
            },
            new
            {
                idTradingPair = 2,
                klines = new[]
                {
                    new
                    {
                        openTime = 1640995200000L,
                        openPrice = "3700",
                        highPrice = "3800",
                        lowPrice = "3600",
                        closePrice = "3750",
                        volume = "500",
                        closeTime = 1641081599999L,
                    },
                    new
                    {
                        openTime = 1641081600000L,
                        openPrice = "3750",
                        highPrice = "3900",
                        lowPrice = "3700",
                        closePrice = "3850",
                        volume = "600",
                        closeTime = 1641167999999L,
                    },
                },
            },
        ];

        public static readonly List<dynamic> PartialExternalKlineDataResponse =
        [
            new
            {
                idTradingPair = 1,
                klines = new[]
                {
                    new
                    {
                        openTime = 1640995200000L,
                        openPrice = "47000m",
                        highPrice = "48000",
                        lowPrice = "46000",
                        closePrice = "47500",
                        volume = "1000",
                        closeTime = 1641081599999L,
                    },
                },
            },
        ];

        public static readonly List<dynamic> KlineServiceResponse =
        [
            new
            {
                idTradingPair = 1,
                klines = new[]
                {
                    new
                    {
                        openTime = 1640995200000L,
                        openPrice = "47000",
                        highPrice = "48000",
                        lowPrice = "46000",
                        closePrice = "47500",
                        volume = "1000",
                        closeTime = 1641081599999L,
                    },
                    new
                    {
                        openTime = 1641081600000L,
                        openPrice = "47500",
                        highPrice = "49000",
                        lowPrice = "47000",
                        closePrice = "48500",
                        volume = "1200",
                        closeTime = 1641167999999L,
                    },
                },
            },
            new
            {
                idTradingPair = 2,
                klines = new[]
                {
                    new
                    {
                        openTime = 1640995200000L,
                        openPrice = "3700",
                        highPrice = "3800",
                        lowPrice = "3600",
                        closePrice = "3750",
                        volume = "500",
                        closeTime = 1641081599999L,
                    },
                    new
                    {
                        openTime = 1641081600000L,
                        openPrice = "3750",
                        highPrice = "3900",
                        lowPrice = "3700",
                        closePrice = "3850",
                        volume = "600",
                        closeTime = 1641167999999L,
                    },
                },
            },
        ];

        public static readonly List<dynamic> PartialKlineServiceResponse =
        [
            new
            {
                idTradingPair = 1,
                klines = new[]
                {
                    new
                    {
                        openTime = 1640995200000L,
                        openPrice = "47000",
                        highPrice = "48000",
                        lowPrice = "46000",
                        closePrice = "47500",
                        volume = "1000",
                        closeTime = 1641081599999L,
                    },
                },
            },
        ];
    }
}
