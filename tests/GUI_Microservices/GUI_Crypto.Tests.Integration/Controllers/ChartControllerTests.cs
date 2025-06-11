using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GUI_Crypto.ApiContracts.Requests.KlineData;
using GUI_Crypto.ApiContracts.Responses;
using SharedLibrary.Enums;
using SharedLibrary.Extensions.Testing;
using WireMock.Admin.Mappings;

namespace GUI_Crypto.Tests.Integration.Controllers;

[Collection("Controllers Integration Tests")]
public class ChartControllerTests(CustomWebApplicationFactory factory)
    : BaseControllerIntegrationTest(factory)
{
    #region Chart Tests

    [Fact]
    public async Task Chart_WithValidData_ShouldReturnChartView()
    {
        // Arrange
        await Factory.SvcExternalServerMock.PostMappingAsync(
            WireMockMappings.SvcExternal.GetKlineData
        );
        await Factory.SvcCoinsServerMock.PostMappingAsync(WireMockMappings.SvcCoins.GetCoinById);

        var request = TestData.BasicKlineDataRequest;

        // Act
        var response = await Client.PostAsJsonAsync("/chart", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/html");
    }

    [Fact]
    public async Task Chart_WhenExternalServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        await Factory.SvcExternalServerMock.PostMappingAsync(
            WireMockMappings.SvcExternal.GetKlineDataError
        );
        await Factory.SvcCoinsServerMock.PostMappingAsync(WireMockMappings.SvcCoins.GetCoinById);

        var request = TestData.BasicKlineDataRequest;

        // Act
        var response = await Client.PostAsJsonAsync("/chart", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Chart_WhenCoinsServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        await Factory.SvcExternalServerMock.PostMappingAsync(
            WireMockMappings.SvcExternal.GetKlineData
        );
        await Factory.SvcCoinsServerMock.PostMappingAsync(
            WireMockMappings.SvcCoins.GetCoinByIdError
        );

        var request = TestData.BasicKlineDataRequest;

        // Act
        var response = await Client.PostAsJsonAsync("/chart", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Chart_WhenCoinNotFound_ShouldReturnNotFound()
    {
        // Arrange
        await Factory.SvcExternalServerMock.PostMappingAsync(
            WireMockMappings.SvcExternal.GetKlineData
        );
        await Factory.SvcCoinsServerMock.PostMappingAsync(
            WireMockMappings.SvcCoins.GetCoinByIdNotFound
        );

        var request = TestData.BasicKlineDataRequest;

        // Act
        var response = await Client.PostAsJsonAsync("/chart", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region GetKlineData Tests

    [Fact]
    public async Task GetKlineData_WithValidData_ShouldReturnOkWithKlineData()
    {
        // Arrange
        await Factory.SvcExternalServerMock.PostMappingAsync(
            WireMockMappings.SvcExternal.GetKlineData
        );

        var request = TestData.CustomKlineDataRequest;

        // Act
        var response = await Client.PostAsJsonAsync("/chart/klines/query", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<Kline>>();

        result.Should().NotBeNull();
        result!.Should().HaveCount(2);

        // Verify first kline data point
        var firstKline = result![0];
        firstKline.OpenTime.Should().Be(1640995200000);
        firstKline.OpenPrice.Should().Be(46000.50m);
        firstKline.HighPrice.Should().Be(47000.75m);
        firstKline.LowPrice.Should().Be(45500.25m);
        firstKline.ClosePrice.Should().Be(46800.00m);
        firstKline.Volume.Should().Be(123.456m);
        firstKline.CloseTime.Should().Be(1640998800000);

        // Verify second kline data point
        var secondKline = result[1];
        secondKline.OpenTime.Should().Be(1640998800000);
        secondKline.OpenPrice.Should().Be(46800.00m);
        secondKline.HighPrice.Should().Be(48000.00m);
        secondKline.LowPrice.Should().Be(46500.00m);
        secondKline.ClosePrice.Should().Be(47500.50m);
        secondKline.Volume.Should().Be(234.567m);
        secondKline.CloseTime.Should().Be(1641002400000);
    }

    [Fact]
    public async Task GetKlineData_WithEmptyData_ShouldReturnOkWithEmptyCollection()
    {
        // Arrange
        await Factory.SvcExternalServerMock.PostMappingAsync(
            WireMockMappings.SvcExternal.GetKlineDataEmpty
        );

        var request = TestData.CustomKlineDataRequest;

        // Act
        var response = await Client.PostAsJsonAsync("/chart/klines/query", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<Kline>>();

        result.Should().NotBeNull();
        result!.Should().BeEmpty();
    }

    [Fact]
    public async Task GetKlineData_WhenExternalServiceFails_ShouldReturnInternalServerError()
    {
        // Arrange
        await Factory.SvcExternalServerMock.PostMappingAsync(
            WireMockMappings.SvcExternal.GetKlineDataError
        );

        var request = TestData.CustomKlineDataRequest;

        // Act
        var response = await Client.PostAsJsonAsync("/chart/klines/query", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GetKlineData_WithCustomParameters_ShouldCallExternalServiceWithCorrectParameters()
    {
        // Arrange
        await Factory.SvcExternalServerMock.PostMappingAsync(
            WireMockMappings.SvcExternal.GetKlineDataWithCustomParams
        );

        var request = TestData.CustomParametersKlineDataRequest;

        // Act
        var response = await Client.PostAsJsonAsync("/chart/klines/query", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<Kline>>();

        result.Should().NotBeNull();
        result!.Should().HaveCount(1);
    }

    #endregion

    private static class WireMockMappings
    {
        public static class SvcExternal
        {
            public static MappingModel GetKlineData =>
                new()
                {
                    Request = new RequestModel
                    {
                        Methods = ["POST"],
                        Path = "/exchanges/kline/query",
                    },
                    Response = new ResponseModel
                    {
                        StatusCode = 200,
                        Headers = new Dictionary<string, object>
                        {
                            ["Content-Type"] = "application/json",
                        },
                        Body = JsonSerializer.Serialize(TestData.SvcExternalKlineResponse),
                    },
                };

            public static MappingModel GetKlineDataError =>
                new()
                {
                    Request = new RequestModel
                    {
                        Methods = ["POST"],
                        Path = "/exchanges/kline/query",
                    },
                    Response = new ResponseModel { StatusCode = 500 },
                };

            public static MappingModel GetKlineDataEmpty =>
                new()
                {
                    Request = new RequestModel
                    {
                        Methods = ["POST"],
                        Path = "/exchanges/kline/query",
                    },
                    Response = new ResponseModel
                    {
                        StatusCode = 200,
                        Headers = new Dictionary<string, object>
                        {
                            ["Content-Type"] = "application/json",
                        },
                        Body = JsonSerializer.Serialize(TestData.SvcExternalEmptyKlineResponse),
                    },
                };

            public static MappingModel GetKlineDataWithCustomParams =>
                new()
                {
                    Request = new RequestModel
                    {
                        Methods = ["POST"],
                        Path = "/exchanges/kline/query",
                    },
                    Response = new ResponseModel
                    {
                        StatusCode = 200,
                        Headers = new Dictionary<string, object>
                        {
                            ["Content-Type"] = "application/json",
                        },
                        Body = JsonSerializer.Serialize(TestData.SvcExternalCustomKlineResponse),
                    },
                };
        }

        public static class SvcCoins
        {
            public static MappingModel GetCoinById =>
                new()
                {
                    Request = new RequestModel
                    {
                        Methods = ["GET"],
                        Path = "/coins",
                        Params = [WireMockParamBuilder.WithExactMatch("ids", "1")],
                    },
                    Response = new ResponseModel
                    {
                        StatusCode = 200,
                        Headers = new Dictionary<string, object>
                        {
                            { "Content-Type", "application/json" },
                        },
                        BodyAsJson = new[] { TestData.SvcCoinsBitcoinResponse },
                    },
                };

            public static MappingModel GetCoinByIdError =>
                new()
                {
                    Request = new RequestModel
                    {
                        Methods = ["GET"],
                        Path = "/coins",
                        Params = [WireMockParamBuilder.WithExactMatch("ids", "1")],
                    },
                    Response = new ResponseModel { StatusCode = 500 },
                };

            public static MappingModel GetCoinByIdNotFound =>
                new()
                {
                    Request = new RequestModel
                    {
                        Methods = ["GET"],
                        Path = "/coins",
                        Params = [WireMockParamBuilder.WithExactMatch("ids", "1")],
                    },
                    Response = new ResponseModel
                    {
                        StatusCode = 200,
                        Headers = new Dictionary<string, object>
                        {
                            { "Content-Type", "application/json" },
                        },
                        BodyAsJson = Array.Empty<object>(), // Empty array to simulate not found
                    },
                };
        }
    }

    private static class TestData
    {
        public static readonly KlineDataRequest BasicKlineDataRequest = new()
        {
            CoinMain = new KlineDataRequestCoin
            {
                Id = 1,
                Symbol = "BTC",
                Name = "Bitcoin",
            },
            IdTradingPair = 101,
            CoinQuote = new KlineDataRequestCoin
            {
                Id = 5,
                Symbol = "USDT",
                Name = "Tether",
            },
            Exchanges = [Exchange.Binance],
        };

        public static readonly KlineDataRequest CustomKlineDataRequest = new()
        {
            CoinMain = new KlineDataRequestCoin
            {
                Id = 1,
                Symbol = "BTC",
                Name = "Bitcoin",
            },
            IdTradingPair = 101,
            CoinQuote = new KlineDataRequestCoin
            {
                Id = 5,
                Symbol = "USDT",
                Name = "Tether",
            },
            Exchanges = [Exchange.Binance],
            Interval = ExchangeKlineInterval.OneHour,
            StartTime = DateTime.Parse("2024-01-01T00:00:00Z"),
            EndTime = DateTime.Parse("2024-01-02T00:00:00Z"),
            Limit = 500,
        };

        public static readonly KlineDataRequest CustomParametersKlineDataRequest = new()
        {
            CoinMain = new KlineDataRequestCoin
            {
                Id = 1,
                Symbol = "BTC",
                Name = "Bitcoin",
            },
            IdTradingPair = 101,
            CoinQuote = new KlineDataRequestCoin
            {
                Id = 5,
                Symbol = "USDT",
                Name = "Tether",
            },
            Exchanges = [Exchange.Binance],
            Interval = ExchangeKlineInterval.FifteenMinutes,
            StartTime = DateTime.Parse("2024-01-01T00:00:00Z"),
            EndTime = DateTime.Parse("2024-01-01T06:00:00Z"),
            Limit = 100,
        };

        public static readonly dynamic SvcExternalKlineResponse = new
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
        };

        public static readonly dynamic SvcExternalEmptyKlineResponse = new
        {
            idTradingPair = 101,
            klineData = Array.Empty<object>(),
        };

        public static readonly dynamic SvcExternalCustomKlineResponse = new
        {
            idTradingPair = 101,
            klineData = new[]
            {
                new
                {
                    openTime = 1704067200000L, // 2024-01-01T00:00:00Z
                    openPrice = 42000.00m,
                    highPrice = 42500.00m,
                    lowPrice = 41800.00m,
                    closePrice = 42200.00m,
                    volume = 100.123m,
                    closeTime = 1704068100000L, // 15 minutes later
                },
            },
        };

        public static readonly dynamic SvcCoinsBitcoinResponse = new
        {
            id = 1,
            symbol = "BTC",
            name = "Bitcoin",
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
                    exchanges = new[] { 1 },
                },
            },
        };
    }
}
