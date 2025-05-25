using System.Net;
using System.Text.Json;
using FluentResults.Extensions.FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq.Contrib.HttpClient;
using SharedLibrary.Enums;
using SVC_External.ExternalClients.Exchanges.Binance;
using SVC_External.ExternalClients.Exchanges.Contracts.Requests;
using SVC_External.ExternalClients.Exchanges.Contracts.Responses;

namespace SVC_External.Tests.Unit.ExternalClients.Exchanges;

public class BinanceClientTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly Mock<ILogger<BinanceClient>> _loggerMock;
    private readonly BinanceClient _client;

    public BinanceClientTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _loggerMock = new Mock<ILogger<BinanceClient>>();

        var httpClient = _httpMessageHandlerMock.CreateClient();
        httpClient.BaseAddress = new Uri("https://api.binance.com");

        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock.Setup(f => f.CreateClient("BinanceClient")).Returns(httpClient);

        _client = new BinanceClient(httpClientFactoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetAllSpotCoins_ReturnsSuccessfulResultWithExpectedDataInside()
    {
        // Arrange
        _httpMessageHandlerMock
            .SetupRequest(
                HttpMethod.Get,
                "https://api.binance.com/api/v3/exchangeInfo?showPermissionSets=false"
            )
            .ReturnsResponse(HttpStatusCode.OK, TestData.JsonResponse);

        // Act
        var result = await _client.GetAllSpotCoins();

        // Assert
        result
            .Should()
            .BeSuccess()
            .Which.Value.Should()
            .BeEquivalentTo(TestData.ExpectedResultValue);
    }

    [Fact]
    public async Task GetAllSpotCoins_ErrorResponse_ReturnsFailedResult()
    {
        // Arrange
        _httpMessageHandlerMock
            .SetupRequest(
                HttpMethod.Get,
                "https://api.binance.com/api/v3/exchangeInfo?showPermissionSets=false"
            )
            .ReturnsResponse(HttpStatusCode.BadRequest, "Oopsie woopsie");

        // Act
        var result = await _client.GetAllSpotCoins();

        // Assert
        result.Should().BeFailure().Which.Errors.Should().HaveCount(1);
        result.Errors[0].Metadata.Should().ContainValue("Oopsie woopsie");
    }

    [Fact]
    public async Task GetKlineData_ReturnsExpectedData()
    {
        // Arrange
        var expectedResponse = new List<List<object>>
        {
            new() { 123456789, "0.001", "0.002", "0.0005", "0.0015", "100", 123456799 },
        };

        _httpMessageHandlerMock
            .SetupRequest(
                HttpMethod.Get,
                $"https://api.binance.com{TestData.SuccessfulKlineEndpoint}"
            )
            .ReturnsJsonResponse(HttpStatusCode.OK, expectedResponse);

        // Act
        var result = await _client.GetKlineData(TestData.SuccessfulKlineRequest);

        // Assert
        result.Should().BeSuccess().Which.Value.Should().HaveCount(1);
        result
            .Value.First()
            .Should()
            .BeEquivalentTo(
                new
                {
                    OpenTime = 123456789,
                    OpenPrice = 0.001m,
                    HighPrice = 0.002m,
                    LowPrice = 0.0005m,
                    ClosePrice = 0.0015m,
                    Volume = 100m,
                    CloseTime = 123456799,
                }
            );
    }

    [Fact]
    public async Task GetKlineData_ErrorResponse_ReturnsEmptyCollection()
    {
        // Arrange
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, $"https://api.binance.com{TestData.ErrorKlineEndpoint}")
            .ReturnsResponse(HttpStatusCode.BadRequest, "Bad Request");

        // Act
        var result = await _client.GetKlineData(TestData.ErrorKlineRequest);

        // Assert
        result.Should().BeFailure().Which.Errors.Should().HaveCount(1);
    }

    private static class TestData
    {
        public static readonly BinanceDtos.Response Response = new()
        {
            TradingPairs =
            [
                new()
                {
                    BaseAssetSymbol = "BTC",
                    QuoteAssetSymbol = "USDT",
                    Status = BinanceDtos.TradingPairStatus.TRADING,
                },
                new()
                {
                    BaseAssetSymbol = "BTC",
                    QuoteAssetSymbol = "ETH",
                    Status = BinanceDtos.TradingPairStatus.HALT,
                },
                new()
                {
                    BaseAssetSymbol = "ETH",
                    QuoteAssetSymbol = "USDT",
                    Status = BinanceDtos.TradingPairStatus.TRADING,
                },
            ],
        };

        public static readonly string JsonResponse = JsonSerializer.Serialize(Response);

        public static readonly List<ExchangeCoin> ExpectedResultValue =
        [
            new()
            {
                Symbol = "BTC",
                TradingPairs =
                [
                    new()
                    {
                        CoinQuote = new() { Symbol = "USDT" },
                        ExchangeInfo = new()
                        {
                            Exchange = Exchange.Binance,
                            Status = ExchangeTradingPairStatus.Available,
                        },
                    },
                    new()
                    {
                        CoinQuote = new() { Symbol = "ETH" },
                        ExchangeInfo = new()
                        {
                            Exchange = Exchange.Binance,
                            Status = ExchangeTradingPairStatus.CurrentlyUnavailable,
                        },
                    },
                ],
            },
            new()
            {
                Symbol = "ETH",
                TradingPairs =
                [
                    new()
                    {
                        CoinQuote = new() { Symbol = "USDT" },
                        ExchangeInfo = new()
                        {
                            Exchange = Exchange.Binance,
                            Status = ExchangeTradingPairStatus.Available,
                        },
                    },
                ],
            },
        ];

        // Kline data test scenarios
        public static readonly ExchangeKlineDataRequest SuccessfulKlineRequest = new()
        {
            CoinMainSymbol = "BTC",
            CoinQuoteSymbol = "USDT",
            Interval = ExchangeKlineInterval.OneHour,
            Limit = 100,
            StartTimeUnix = 1640995200000,
            EndTimeUnix = 1641081600000,
        };

        public static readonly string SuccessfulKlineEndpoint =
            "/api/v3/klines?symbol=BTCUSDT&interval=1h&limit=100&startTime=1640995200000&endTime=1641081600000";

        public static readonly ExchangeKlineDataRequest ErrorKlineRequest = new()
        {
            CoinMainSymbol = "ETH",
            CoinQuoteSymbol = "BTC",
            Interval = ExchangeKlineInterval.FiveMinutes,
            Limit = 50,
            StartTimeUnix = 1640995200000,
            EndTimeUnix = 1641081600000,
        };

        public static readonly string ErrorKlineEndpoint =
            "/api/v3/klines?symbol=ETHBTC&interval=5m&limit=50&startTime=1640995200000&endTime=1641081600000";
    }
}
