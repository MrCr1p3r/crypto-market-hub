using System.Globalization;
using System.Net;
using System.Text.Json;
using AutoFixture;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Contrib.HttpClient;
using SharedLibrary.Enums;
using SVC_External.Clients.Exchanges;
using SVC_External.Models.Exchanges.ClientResponses;
using SVC_External.Models.Exchanges.Input;
using SVC_External.Models.Exchanges.Output;

namespace SVC_External.Tests.Unit.Clients;

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
    public async Task GetAllSpotCoins_ReturnsExpectedData()
    {
        // Arrange
        _httpMessageHandlerMock
            .SetupRequest(
                HttpMethod.Get,
                "https://api.bybit.com/v5/market/instruments-info?category=spot"
            )
            .ReturnsResponse(HttpStatusCode.OK, TestData.JsonResponse);

        // Act
        var result = await _client.GetAllSpotCoins();

        // Assert
        result.Should().BeEquivalentTo(TestData.ExpectedResult);
    }

    [Fact]
    public async Task GetAllSpotCoins_ErrorResponse_ReturnsEmptyCollection()
    {
        // Arrange
        _httpMessageHandlerMock
            .SetupRequest(
                HttpMethod.Get,
                "https://api.bybit.com/v5/market/instruments-info?category=spot"
            )
            .ReturnsResponse(HttpStatusCode.BadRequest, "Bad Request");

        // Act
        var result = await _client.GetAllSpotCoins();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetKlineData_ReturnsExpectedData()
    {
        // Arrange
        var request = _fixture.Create<ExchangeKlineDataRequest>();
        var endpoint = Mapping.ToBybitKlineEndpoint(request);
        var expectedResponse = new
        {
            result = new
            {
                list = new List<List<string>>
                {
                    new() { "123456789", "0.001", "0.002", "0.0005", "0.0015", "100" },
                },
            },
        };
        var jsonResponse = JsonSerializer.Serialize(expectedResponse);

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, $"https://api.bybit.com{endpoint}")
            .ReturnsResponse(HttpStatusCode.OK, jsonResponse);

        // Act
        var result = await _client.GetKlineData(request);

        // Assert
        result.Should().HaveCount(1);
        result
            .First()
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
    public async Task GetKlineData_ErrorResponse_ReturnsEmptyCollection()
    {
        // Arrange
        var request = _fixture.Create<ExchangeKlineDataRequest>();
        var endpoint = Mapping.ToBybitKlineEndpoint(request);

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, $"https://api.bybit.com{endpoint}")
            .ReturnsResponse(HttpStatusCode.BadRequest, "Bad Request");

        // Act
        var result = await _client.GetKlineData(request);

        // Assert
        result.Should().BeEmpty();
    }

    private static class Mapping
    {
        public static string ToBybitKlineEndpoint(ExchangeKlineDataRequest request) =>
            $"/v5/market/kline?category=spot"
            + $"&symbol={request.CoinMain}{request.CoinQuote}"
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
        public static readonly BybitDtos.BybitSpotAssetsResponse Response = new()
        {
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

        public static readonly string JsonResponse = JsonSerializer.Serialize(Response);

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
                        ExchangeInfos =
                        [
                            new()
                            {
                                Exchange = Exchange.Bybit,
                                Status = ExchangeTradingPairStatus.Available,
                            },
                        ],
                    },
                    new()
                    {
                        CoinQuote = new() { Symbol = "ETH" },
                        ExchangeInfos =
                        [
                            new()
                            {
                                Exchange = Exchange.Bybit,
                                Status = ExchangeTradingPairStatus.CurrentlyUnavailable,
                            },
                        ],
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
                        ExchangeInfos =
                        [
                            new()
                            {
                                Exchange = Exchange.Bybit,
                                Status = ExchangeTradingPairStatus.Available,
                            },
                        ],
                    },
                ],
            },
        ];
    }
}
