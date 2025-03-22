using System.Globalization;
using System.Net;
using System.Text.Json;
using AutoFixture;
using FluentAssertions;
using FluentResults.Extensions.FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Contrib.HttpClient;
using SharedLibrary.Enums;
using SVC_External.Clients.Exchanges;
using SVC_External.Models.Exchanges.ClientResponses;
using SVC_External.Models.Exchanges.Input;
using SVC_External.Models.Exchanges.Output;

namespace SVC_External.Tests.Unit.Clients.Exchanges;

public class BybitClientTests
{
    private readonly IFixture _fixture;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly Mock<ILogger<BybitClient>> _loggerMock;
    private readonly BybitClient _client;

    public BybitClientTests()
    {
        _fixture = new Fixture();
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
        var request = _fixture.Create<ExchangeKlineDataRequest>();
        var endpoint = Mapping.ToBybitKlineEndpoint(request);

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, $"https://api.bybit.com{endpoint}")
            .ReturnsResponse(HttpStatusCode.OK, TestData.JsonKlineResponse);

        // Act
        var result = await _client.GetKlineData(request);

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
                    CloseTime = Mapping.CalculateCloseTime("123456789", request.Interval),
                }
            );
    }

    [Fact]
    public async Task GetKlineData_ErrorResponse_ReturnsFailedResult()
    {
        // Arrange
        var request = _fixture.Create<ExchangeKlineDataRequest>();
        var endpoint = Mapping.ToBybitKlineEndpoint(request);

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, $"https://api.bybit.com{endpoint}")
            .ReturnsResponse(HttpStatusCode.OK, TestData.JsonKlineErrorResponse);

        // Act
        var result = await _client.GetKlineData(request);

        // Assert
        result.Should().BeFailure().Which.Errors.Should().HaveCount(1);
    }

    private static class Mapping
    {
        public static string ToBybitKlineEndpoint(ExchangeKlineDataRequest request) =>
            $"/v5/market/kline?category=spot"
            + $"&symbol={request.CoinMainSymbol}{request.CoinQuoteSymbol}"
            + $"&interval={ToBybitTimeFrame(request.Interval)}"
            + $"&start={request.StartTimeUnix}"
            + $"&end={request.EndTimeUnix}"
            + $"&limit={request.Limit}";

        public static string ToBybitTimeFrame(ExchangeKlineInterval interval) =>
            interval switch
            {
                ExchangeKlineInterval.OneMinute => "1",
                ExchangeKlineInterval.FiveMinutes => "5",
                ExchangeKlineInterval.FifteenMinutes => "15",
                ExchangeKlineInterval.ThirtyMinutes => "30",
                ExchangeKlineInterval.OneHour => "60",
                ExchangeKlineInterval.FourHours => "240",
                ExchangeKlineInterval.OneDay => "D",
                ExchangeKlineInterval.OneWeek => "W",
                ExchangeKlineInterval.OneMonth => "M",
                _ => throw new ArgumentException($"Unsupported TimeFrame: {interval}"),
            };

        public static long CalculateCloseTime(string openTimeString, ExchangeKlineInterval interval)
        {
            var openTime = long.Parse(openTimeString, CultureInfo.InvariantCulture);
            var durationInMinutes = (long)interval;
            return openTime + (durationInMinutes * 60 * 1000);
        }
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
    }
}
