using System.Net;
using FluentResults.Extensions.FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq.Contrib.HttpClient;
using SharedLibrary.Enums;
using SVC_External.ExternalClients.Exchanges.Contracts.Requests;
using SVC_External.ExternalClients.Exchanges.Contracts.Responses;
using SVC_External.ExternalClients.Exchanges.Mexc;

namespace SVC_External.Tests.Unit.ExternalClients.Exchanges;

public class MexcClientTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly Mock<ILogger<MexcClient>> _loggerMock;
    private readonly MexcClient _client;

    public MexcClientTests()
    {
        _loggerMock = new Mock<ILogger<MexcClient>>();

        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        var httpClient = _httpMessageHandlerMock.CreateClient();
        httpClient.BaseAddress = new Uri("https://api.mexc.com");

        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock.Setup(f => f.CreateClient("MexcClient")).Returns(httpClient);

        _client = new MexcClient(httpClientFactoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetAllSpotCoins_ReturnsSuccessfulResultWithExpectedDataInside()
    {
        // Arrange
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, "https://api.mexc.com/api/v3/exchangeInfo")
            .ReturnsJsonResponse(HttpStatusCode.OK, TestData.Response);

        // Act
        var result = await _client.GetAllSpotCoins();

        // Assert
        result.Should().BeSuccess().Which.Value.Should().BeEquivalentTo(TestData.ExpectedResult);
    }

    [Fact]
    public async Task GetAllSpotCoins_ErrorResponse_ReturnsFailedResult()
    {
        // Arrange
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, "https://api.mexc.com/api/v3/exchangeInfo")
            .ReturnsResponse(HttpStatusCode.BadRequest, "Bad Request");

        // Act
        var result = await _client.GetAllSpotCoins();

        // Assert
        result.Should().BeFailure().Which.Errors.Should().HaveCount(1);
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
            .SetupRequest(HttpMethod.Get, $"https://api.mexc.com{TestData.SuccessfulKlineEndpoint}")
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
    public async Task GetKlineData_ErrorResponse_ReturnsFailedResult()
    {
        // Arrange
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, $"https://api.mexc.com{TestData.ErrorKlineEndpoint}")
            .ReturnsResponse(HttpStatusCode.BadRequest, "Bad Request");

        // Act
        var result = await _client.GetKlineData(TestData.ErrorKlineRequest);

        // Assert
        result.Should().BeFailure().Which.Errors.Should().HaveCount(1);
    }

    private static class TestData
    {
        public static readonly MexcDtos.Response Response = new()
        {
            TradingPairs =
            [
                new()
                {
                    BaseAssetSymbol = "BTC",
                    QuoteAssetSymbol = "USDT",
                    Status = MexcDtos.TradingPairStatus.Trading,
                    BaseAssetFullName = "Bitcoin",
                },
                new()
                {
                    BaseAssetSymbol = "BTC",
                    QuoteAssetSymbol = "ETH",
                    Status = MexcDtos.TradingPairStatus.CurrentlyUnavailable,
                    BaseAssetFullName = "Bitcoin",
                },
                new()
                {
                    BaseAssetSymbol = "ETH",
                    QuoteAssetSymbol = "USDT",
                    Status = MexcDtos.TradingPairStatus.Trading,
                    BaseAssetFullName = "Ethereum",
                },
            ],
        };

        public static readonly List<ExchangeCoin> ExpectedResult =
        [
            new()
            {
                Symbol = "BTC",
                Name = "Bitcoin",
                TradingPairs =
                [
                    new()
                    {
                        CoinQuote = new() { Symbol = "USDT" },
                        ExchangeInfo = new()
                        {
                            Exchange = Exchange.Mexc,
                            Status = ExchangeTradingPairStatus.Available,
                        },
                    },
                    new()
                    {
                        CoinQuote = new() { Symbol = "ETH", Name = "Ethereum" },
                        ExchangeInfo = new()
                        {
                            Exchange = Exchange.Mexc,
                            Status = ExchangeTradingPairStatus.CurrentlyUnavailable,
                        },
                    },
                ],
            },
            new()
            {
                Symbol = "ETH",
                Name = "Ethereum",
                TradingPairs =
                [
                    new()
                    {
                        CoinQuote = new() { Symbol = "USDT" },
                        ExchangeInfo = new()
                        {
                            Exchange = Exchange.Mexc,
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
