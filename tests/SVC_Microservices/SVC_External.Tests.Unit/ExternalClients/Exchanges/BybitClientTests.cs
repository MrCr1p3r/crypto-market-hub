using System.Net;
using System.Text.Json;
using FluentResults.Extensions.FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq.Contrib.HttpClient;
using SharedLibrary.Enums;
using SharedLibrary.Models;
using SVC_External.ExternalClients.Exchanges.Bybit;
using SVC_External.ExternalClients.Exchanges.Contracts.Requests;
using SVC_External.ExternalClients.Exchanges.Contracts.Responses;

namespace SVC_External.Tests.Unit.ExternalClients.Exchanges;

public class BybitClientTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly Mock<ILogger<BybitClient>> _loggerMock;
    private readonly BybitClient _client;

    public BybitClientTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _loggerMock = new Mock<ILogger<BybitClient>>();

        var httpClient = _httpMessageHandlerMock.CreateClient();
        httpClient.BaseAddress = new Uri("https://api.bybit.com");

        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock.Setup(f => f.CreateClient("BybitClient")).Returns(httpClient);

        _client = new BybitClient(httpClientFactoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetAllSpotCoins_ReturnsSuccessfulResultWithExpectedDataInside()
    {
        // Arrange
        _httpMessageHandlerMock
            .SetupRequest(
                HttpMethod.Get,
                "https://api.bybit.com/v5/market/instruments-info?category=spot"
            )
            .ReturnsResponse(HttpStatusCode.OK, TestData.JsonSpotAssetsResponse);

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
            .SetupRequest(
                HttpMethod.Get,
                "https://api.bybit.com/v5/market/instruments-info?category=spot"
            )
            .ReturnsResponse(HttpStatusCode.OK, TestData.JsonSpotAssetsErrorResponse);

        // Act
        var result = await _client.GetAllSpotCoins();

        // Assert
        result.Should().BeFailure().Which.Errors.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetKlineData_ReturnsExpectedData()
    {
        // Arrange
        _httpMessageHandlerMock
            .SetupRequest(
                HttpMethod.Get,
                $"https://api.bybit.com{TestData.SuccessfulKlineEndpoint}"
            )
            .ReturnsResponse(HttpStatusCode.OK, TestData.JsonKlineResponse);

        // Act
        var result = await _client.GetKlineData(TestData.SuccessfulKlineRequest);

        // Assert
        result.Should().BeSuccess().Which.Value.Should().HaveCount(1);
        result
            .Value.First()
            .Should()
            .BeEquivalentTo(
                new Kline
                {
                    OpenTime = 123456789,
                    OpenPrice = "0.001",
                    HighPrice = "0.002",
                    LowPrice = "0.0005",
                    ClosePrice = "0.0015",
                    Volume = "100",
                    CloseTime = 127056789, // Pre-calculated close time
                }
            );
    }

    [Fact]
    public async Task GetKlineData_ErrorResponse_ReturnsFailedResult()
    {
        // Arrange
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, $"https://api.bybit.com{TestData.ErrorKlineEndpoint}")
            .ReturnsResponse(HttpStatusCode.OK, TestData.JsonKlineErrorResponse);

        // Act
        var result = await _client.GetKlineData(TestData.ErrorKlineRequest);

        // Assert
        result.Should().BeFailure().Which.Errors.Should().HaveCount(1);
    }

    private static class TestData
    {
        private static readonly BybitDtos.BybitSpotAssetsResponse SpotAssetsResponse = new()
        {
            ResponseCode = 0,
            Result = new()
            {
                TradingPairs =
                [
                    new()
                    {
                        BaseAssetSymbol = "BTC",
                        QuoteAssetSymbol = "USDT",
                        TradingStatus = BybitDtos.TradingPairStatus.Trading,
                    },
                    new()
                    {
                        BaseAssetSymbol = "BTC",
                        QuoteAssetSymbol = "ETH",
                        TradingStatus = BybitDtos.TradingPairStatus.PreLaunch,
                    },
                    new()
                    {
                        BaseAssetSymbol = "ETH",
                        QuoteAssetSymbol = "USDT",
                        TradingStatus = BybitDtos.TradingPairStatus.Trading,
                    },
                ],
            },
        };

        public static readonly string JsonSpotAssetsResponse = JsonSerializer.Serialize(
            SpotAssetsResponse
        );

        public static readonly List<ExchangeCoin> ExpectedResult =
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
                            Exchange = Exchange.Bybit,
                            Status = ExchangeTradingPairStatus.Available,
                        },
                    },
                    new()
                    {
                        CoinQuote = new() { Symbol = "ETH" },
                        ExchangeInfo = new()
                        {
                            Exchange = Exchange.Bybit,
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
                            Exchange = Exchange.Bybit,
                            Status = ExchangeTradingPairStatus.Available,
                        },
                    },
                ],
            },
        ];

        private static readonly BybitDtos.BybitSpotAssetsResponse SpotAssetsErrorResponse = new()
        {
            ResponseCode = 10001,
            ResponseMessage = "params error: Symbol Is Invalid",
            Result = new(),
        };

        public static readonly string JsonSpotAssetsErrorResponse = JsonSerializer.Serialize(
            SpotAssetsErrorResponse
        );

        private static readonly BybitDtos.BybitKlineResponse KlineResponse = new()
        {
            ResponseCode = 0,
            ResponseMessage = "success",
            Result = new()
            {
                List =
                [
                    ["123456789", "0.001", "0.002", "0.0005", "0.0015", "100"],
                ],
            },
        };

        public static readonly string JsonKlineResponse = JsonSerializer.Serialize(KlineResponse);

        private static readonly BybitDtos.BybitKlineResponse KlineErrorResponse = new()
        {
            ResponseCode = 10001,
            ResponseMessage = "params error: Symbol Is Invalid",
            Result = new(),
        };

        public static readonly string JsonKlineErrorResponse = JsonSerializer.Serialize(
            KlineErrorResponse
        );

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
            "/v5/market/kline?category=spot&symbol=BTCUSDT&interval=60&start=1640995200000&end=1641081600000&limit=100";

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
            "/v5/market/kline?category=spot&symbol=ETHBTC&interval=5&start=1640995200000&end=1641081600000&limit=50";
    }
}
