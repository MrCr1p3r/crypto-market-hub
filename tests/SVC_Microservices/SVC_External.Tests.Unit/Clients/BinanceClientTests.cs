using System.Net;
using System.Text.Json;
using AutoFixture;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Contrib.HttpClient;
using SharedLibrary.Enums;
using SVC_External.Clients;
using SVC_External.Models.Input;
using SVC_External.Models.Output;

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
    public async Task GetKlineData_ReturnsExpectedData()
    {
        // Arrange
        var request = _fixture.Create<KlineDataRequestFormatted>();
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
        var request = _fixture.Create<KlineDataRequestFormatted>();
        var endpoint = Mapping.ToBinanceKlineEndpoint(request);

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, $"https://api.binance.com{endpoint}")
            .ReturnsResponse(HttpStatusCode.BadRequest, "Bad Request");

        // Act
        var result = await _client.GetKlineData(request);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllListedCoins_ReturnsExpectedData()
    {
        // Arrange
        var listedCoinsParameter = new ListedCoins();
        var expectedBaseAssets = new List<string> { "BTC", "ETH", "BNB" };
        var symbols = expectedBaseAssets
            .Select(baseAsset => new Dictionary<string, object>
            {
                { "symbol", baseAsset + "USDT" },
                { "baseAsset", baseAsset },
                { "quoteAsset", "USDT" },
            })
            .ToList();

        var exchangeInfo = new Dictionary<string, object> { { "symbols", symbols } };
        var jsonResponse = JsonSerializer.Serialize(exchangeInfo);

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, "https://api.binance.com/api/v3/exchangeInfo")
            .ReturnsResponse(HttpStatusCode.OK, jsonResponse);

        // Act
        var listedCoins = await _client.GetAllListedCoins(listedCoinsParameter);

        // Assert
        listedCoins.BinanceCoins.Should().BeEquivalentTo(expectedBaseAssets);
    }

    [Fact]
    public async Task GetAllListedCoins_ErrorResponse_ReturnsListWithoutNewCoins()
    {
        // Arrange
        var listedCoinsParameter = new ListedCoins();

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, "https://api.binance.com/api/v3/exchangeInfo")
            .ReturnsResponse(HttpStatusCode.BadRequest, "Bad Request");

        // Act
        var result = await _client.GetAllListedCoins(listedCoinsParameter);

        result.BinanceCoins.Should().BeEmpty();
    }

    private static class Mapping
    {
        public static string ToBinanceKlineEndpoint(KlineDataRequestFormatted request) =>
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
