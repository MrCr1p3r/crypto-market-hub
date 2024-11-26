using System.Net;
using System.Text.Json;
using AutoFixture;
using FluentAssertions;
using Moq;
using Moq.Protected;
using SVC_External.Clients;
using SVC_External.Models.Input;

namespace SVC_External.Tests.Unit.Clients;

public class MexcClientTests
{
    private readonly IFixture _fixture;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly MexcClient _client;

    public MexcClientTests()
    {
        _fixture = new Fixture();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("https://api.mexc.com")
        };

        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _httpClientFactoryMock.Setup(f => f.CreateClient("MexcClient")).Returns(httpClient);

        _client = new MexcClient(_httpClientFactoryMock.Object);
    }

    [Fact]
    public async Task GetKlineData_ReturnsExpectedData()
    {
        // Arrange
        var request = _fixture.Create<KlineDataRequestFormatted>();
        var expectedResponse = new List<List<object>>
        {
            new() { 123456789, "0.001", "0.002", "0.0005", "0.0015", "100", 123456799 }
        };

        var jsonResponse = JsonSerializer.Serialize(expectedResponse);
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
        result.Should().HaveCount(expectedResponse.Count);
        result.First().Should().BeEquivalentTo(
        new {
            OpenTime = 123456789,
            OpenPrice = 0.001m,
            HighPrice = 0.002m,
            LowPrice = 0.0005m,
            ClosePrice = 0.0015m,
            Volume = 100m,
            CloseTime = 123456799
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
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("[]")
            });

        // Act
        var result = await _client.GetKlineData(request);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllListedCoins_ReturnsExpectedData()
    {
        // Arrange
        var expectedBaseAssets = new List<string> { "BTC", "ETH", "XRP" };

        var symbols = expectedBaseAssets.Select(baseAsset => new Dictionary<string, object>
        {
            { "symbol", baseAsset + "USDT" },
            { "baseAsset", baseAsset },
            { "quoteAsset", "USDT" },
        }).ToList();

        var responseContent = new
        {
            symbols
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
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri != null && req.RequestUri.AbsolutePath.Contains("/api/v3/exchangeInfo")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _client.GetAllListedCoins();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedBaseAssets);
    }

    [Fact]
    public async Task GetAllListedCoins_EmptySymbols_ReturnsEmptyCollection()
    {
        // Arrange
        var responseContent = new
        {
            symbols = new List<object>()
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
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri != null && req.RequestUri.AbsolutePath.Contains("/api/v3/exchangeInfo")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _client.GetAllListedCoins();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllListedCoins_MissingSymbolsProperty_ReturnsEmptyCollection()
    {
        // Arrange
        var responseContent = new
        {
            // "symbols" property is missing
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
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri != null && req.RequestUri.AbsolutePath.Contains("/api/v3/exchangeInfo")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _client.GetAllListedCoins();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllListedCoins_NullOrEmptyBaseAsset_Ignored()
    {
        // Arrange
        var symbols = new List<Dictionary<string, object>>
        {
            new() {
                { "symbol", "BTCUSDT" },
                { "baseAsset", "BTC" },
                { "quoteAsset", "USDT" }
            },
            new() {
                { "symbol", "XRPUSDT" },
                { "baseAsset", "" },
                { "quoteAsset", "USDT" }
            },
            new() {
                { "symbol", "BNBUSDT" },
                // Missing "baseAsset" property
                { "quoteAsset", "USDT" }
            }
        };

        var responseContent = new
        {
            symbols
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
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri != null && req.RequestUri.AbsolutePath.Contains("/api/v3/exchangeInfo")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _client.GetAllListedCoins();

        // Assert
        result.Should().ContainSingle();
        result.First().Should().Be("BTC");
    }
}
