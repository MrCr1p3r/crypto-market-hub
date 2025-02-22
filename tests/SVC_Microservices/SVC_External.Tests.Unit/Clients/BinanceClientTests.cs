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

public class BinanceClientTests
{
    private readonly IFixture _fixture;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly Mock<ILogger<BinanceClient>> _loggerMock;
    private readonly BinanceClient _client;

    public BinanceClientTests()
    {
        _fixture = new Fixture();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _loggerMock = new Mock<ILogger<BinanceClient>>();

        var httpClient = _httpMessageHandlerMock.CreateClient();
        httpClient.BaseAddress = new Uri("https://api.binance.com");

        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock.Setup(f => f.CreateClient("BinanceClient")).Returns(httpClient);

        _client = new BinanceClient(httpClientFactoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetAllSpotCoins_ReturnsExpectedData()
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
        result.Should().BeEquivalentTo(TestData.ExpectedResult);
    }

    [Fact]
    public async Task GetAllSpotCoins_ErrorResponse_ReturnsEmptyCollection()
    {
        // Arrange
        _httpMessageHandlerMock
            .SetupRequest(
                HttpMethod.Get,
                "https://api.binance.com/api/v3/exchangeInfo?showPermissionSets=false"
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
        var endpoint = Mapping.ToBinanceKlineEndpoint(request);
        var expectedResponse = new List<List<object>>
        {
            new() { 123456789, "0.001", "0.002", "0.0005", "0.0015", "100", 123456799 },
        };
        var jsonResponse = JsonSerializer.Serialize(expectedResponse);

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, $"https://api.binance.com{endpoint}")
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
                    CloseTime = 123456799,
                }
            );
    }

    [Fact]
    public async Task GetKlineData_ErrorResponse_ReturnsEmptyCollection()
    {
        // Arrange
        var request = _fixture.Create<ExchangeKlineDataRequest>();
        var endpoint = Mapping.ToBinanceKlineEndpoint(request);

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, $"https://api.binance.com{endpoint}")
            .ReturnsResponse(HttpStatusCode.BadRequest, "Bad Request");

        // Act
        var result = await _client.GetKlineData(request);

        // Assert
        result.Should().BeEmpty();
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
                                Exchange = Exchange.Binance,
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
                                Exchange = Exchange.Binance,
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
                                Exchange = Exchange.Binance,
                                Status = ExchangeTradingPairStatus.Available,
                            },
                        ],
                    },
                ],
            },
        ];
    }

    private static class Mapping
    {
        public static string ToBinanceKlineEndpoint(ExchangeKlineDataRequest request) =>
            $"/api/v3/klines?symbol={request.CoinMain + request.CoinQuote}"
            + $"&interval={ToBinanceTimeFrame(request.Interval)}"
            + $"&limit={request.Limit}"
            + $"&startTime={request.StartTimeUnix}"
            + $"&endTime={request.EndTimeUnix}";

        public static string ToBinanceTimeFrame(ExchangeKlineInterval timeFrame) =>
            timeFrame switch
            {
                ExchangeKlineInterval.OneMinute => "1m",
                ExchangeKlineInterval.FiveMinutes => "5m",
                ExchangeKlineInterval.FifteenMinutes => "15m",
                ExchangeKlineInterval.ThirtyMinutes => "30m",
                ExchangeKlineInterval.OneHour => "1h",
                ExchangeKlineInterval.FourHours => "4h",
                ExchangeKlineInterval.OneDay => "1d",
                ExchangeKlineInterval.OneWeek => "1w",
                ExchangeKlineInterval.OneMonth => "1M",
                _ => throw new ArgumentException($"Unsupported TimeFrame: {timeFrame}"),
            };
    }
}
