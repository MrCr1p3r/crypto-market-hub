using System.Globalization;
using System.Net;
using System.Text.Json;
using AutoFixture;
using FluentAssertions;
using Moq;
using Moq.Protected;
using SharedLibrary.Enums;
using SVC_External.Clients;
using SVC_External.Models.Input;

namespace SVC_External.Tests.Unit.Clients;

public class BybitClientTests
{
    private readonly IFixture _fixture;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly BybitClient _client;

    public BybitClientTests()
    {
        _fixture = new Fixture();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("https://api.bybit.com")
        };

        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _httpClientFactoryMock.Setup(f => f.CreateClient("BybitClient")).Returns(httpClient);

        _client = new BybitClient(_httpClientFactoryMock.Object);
    }

    [Fact]
    public async Task GetKlineData_ReturnsExpectedData()
    {
        // Arrange
        var request = _fixture.Create<KlineDataRequestFormatted>();
        var expectedData = new[]
        {
            new[] { "1234567890000", "0.001", "0.002", "0.0005", "0.0015", "100" }
        };

        var jsonResponse = JsonSerializer.Serialize(new
        {
            result = new
            {
                list = expectedData
            }
        });

        var httpResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(jsonResponse)
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _client.GetKlineData(request);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(expectedData.Length);
        result.First().Should().BeEquivalentTo(new
        {
            OpenTime = long.Parse(expectedData[0][0]),
            OpenPrice = decimal.Parse(expectedData[0][1], CultureInfo.InvariantCulture),
            HighPrice = decimal.Parse(expectedData[0][2], CultureInfo.InvariantCulture),
            LowPrice = decimal.Parse(expectedData[0][3], CultureInfo.InvariantCulture),
            ClosePrice = decimal.Parse(expectedData[0][4], CultureInfo.InvariantCulture),
            Volume = decimal.Parse(expectedData[0][5], CultureInfo.InvariantCulture),
            CloseTime = Mapping.CalculateCloseTime(expectedData[0][0], request.Interval)
        });
    }

    [Fact]
    public async Task GetKlineData_ErrorResponse_ThrowsException()
    {
        // Arrange
        var request = _fixture.Create<KlineDataRequestFormatted>();

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("Bad Request")
            });

        // Act
        var act = async () => await _client.GetKlineData(request);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetKlineData_EmptyResponse_ReturnsEmptyCollection()
    {
        // Arrange
        var request = _fixture.Create<KlineDataRequestFormatted>();
        var jsonResponse = JsonSerializer.Serialize(new
        {
            result = new
            {
                list = Array.Empty<string[]>()
            }
        });

        var httpResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(jsonResponse)
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _client.GetKlineData(request);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllListedCoins_ReturnsExpectedData()
    {
        // Arrange
        var expectedBaseCoins = new List<string> { "BTC", "ETH", "XRP" };

        var instruments = expectedBaseCoins.Select(baseCoin => new Dictionary<string, object>
        {
            { "symbol", baseCoin + "USDT" },
            { "baseCoin", baseCoin },
            { "quoteCoin", "USDT" },
        }).ToList();

        var responseContent = new
        {
            result = new
            {
                list = instruments
            }
        };

        var jsonResponse = JsonSerializer.Serialize(responseContent);
        var httpResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(jsonResponse)
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.AbsolutePath.Contains("/v5/market/instruments-info")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _client.GetAllListedCoins();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedBaseCoins);
    }

    [Fact]
    public async Task GetAllListedCoins_EmptyList_ReturnsEmptyCollection()
    {
        // Arrange
        var responseContent = new
        {
            result = new
            {
                list = new List<object>()
            }
        };

        var jsonResponse = JsonSerializer.Serialize(responseContent);
        var httpResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(jsonResponse)
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.AbsolutePath.Contains("/v5/market/instruments-info")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _client.GetAllListedCoins();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllListedCoins_MissingResultProperty_ReturnsEmptyCollection()
    {
        // Arrange
        var responseContent = new
        {
            // "result" property is missing
        };

        var jsonResponse = JsonSerializer.Serialize(responseContent);
        var httpResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(jsonResponse)
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.AbsolutePath.Contains("/v5/market/instruments-info")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _client.GetAllListedCoins();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllListedCoins_MissingListProperty_ReturnsEmptyCollection()
    {
        // Arrange
        var responseContent = new
        {
            result = new
            {
                // "list" property is missing
            }
        };

        var jsonResponse = JsonSerializer.Serialize(responseContent);
        var httpResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(jsonResponse)
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.AbsolutePath.Contains("/v5/market/instruments-info")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _client.GetAllListedCoins();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllListedCoins_NullOrEmptyBaseCoin_Ignored()
    {
        // Arrange
        var instruments = new List<Dictionary<string, object>>
        {
            new() {
                { "symbol", "BTCUSDT" },
                { "baseCoin", "BTC" },
                { "quoteCoin", "USDT" }
            },
            new() {
                { "symbol", "XRPUSDT" },
                { "baseCoin", "" }, // Empty baseCoin
                { "quoteCoin", "USDT" }
            },
            new() {
                { "symbol", "BNBUSDT" },
                // Missing "baseCoin" property
                { "quoteCoin", "USDT" }
            }
        };

        var responseContent = new
        {
            result = new
            {
                list = instruments
            }
        };

        var jsonResponse = JsonSerializer.Serialize(responseContent);
        var httpResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(jsonResponse)
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.AbsolutePath.Contains("/v5/market/instruments-info")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _client.GetAllListedCoins();

        // Assert
        result.Should().ContainSingle();
        result.First().Should().Be("BTC");
    }

    private static class Mapping
    {
        public static long CalculateCloseTime(string openTimeString, ExchangeKlineInterval interval)
        {
            var openTime = long.Parse(openTimeString);
            var durationInMinutes = (long)interval;
            return openTime + durationInMinutes * 60 * 1000;
        }
    }
}
