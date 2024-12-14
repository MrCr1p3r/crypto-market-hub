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
    public async Task GetKlineData_ReturnsExpectedData()
    {
        // Arrange
        var request = _fixture.Create<KlineDataRequestFormatted>();
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
        var request = _fixture.Create<KlineDataRequestFormatted>();
        var endpoint = Mapping.ToBybitKlineEndpoint(request);

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, $"https://api.bybit.com{endpoint}")
            .ReturnsResponse(HttpStatusCode.BadRequest, "Bad Request");

        // Act
        var result = await _client.GetKlineData(request);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllListedCoins_ReturnsExpectedData()
    {
        // Arrange
        var listedCoinsParameter = new ListedCoins();
        var expectedBaseAssets = new List<string> { "BTC", "ETH", "BNB" };
        var response = new
        {
            result = new
            {
                list = expectedBaseAssets
                    .Select(baseAsset => new { baseCoin = baseAsset })
                    .ToList(),
            },
        };
        var jsonResponse = JsonSerializer.Serialize(response);

        _httpMessageHandlerMock
            .SetupRequest(
                HttpMethod.Get,
                "https://api.bybit.com/v5/market/instruments-info?category=linear"
            )
            .ReturnsResponse(HttpStatusCode.OK, jsonResponse);

        // Act
        var listedCoins = await _client.GetAllListedCoins(listedCoinsParameter);

        // Assert
        listedCoins.BybitCoins.Should().BeEquivalentTo(expectedBaseAssets);
    }

    [Fact]
    public async Task GetAllListedCoins_ErrorResponse_ReturnsListWithoutNewCoins()
    {
        // Arrange
        var listedCoinsParameter = new ListedCoins();

        _httpMessageHandlerMock
            .SetupRequest(
                HttpMethod.Get,
                "https://api.bybit.com/v5/market/instruments-info?category=linear"
            )
            .ReturnsResponse(HttpStatusCode.BadRequest, "Bad Request");

        // Act
        var result = await _client.GetAllListedCoins(listedCoinsParameter);

        // Assert
        result.BybitCoins.Should().BeEmpty();
    }

    private static class Mapping
    {
        public static string ToBybitKlineEndpoint(KlineDataRequestFormatted request) =>
            $"/v5/market/kline?category=linear"
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
            var openTime = long.Parse(openTimeString);
            var durationInMinutes = (long)interval;
            return openTime + durationInMinutes * 60 * 1000;
        }
    }
}
