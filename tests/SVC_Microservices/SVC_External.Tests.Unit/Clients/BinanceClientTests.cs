using System.Net;
using System.Text.Json;
using AutoFixture;
using FluentAssertions;
using Moq;
using Moq.Protected;
using SVC_External.Clients;
using SVC_External.Models.Input;

namespace SVC_External.Tests.Unit.Clients;

public class BinanceClientTests
{
    private readonly IFixture _fixture;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly BinanceClient _client;

    public BinanceClientTests()
    {
        _fixture = new Fixture();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("https://api.binance.com")
        };

        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _httpClientFactoryMock.Setup(f => f.CreateClient("BinanceClient")).Returns(httpClient);

        _client = new BinanceClient(_httpClientFactoryMock.Object);
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
        result.First().Should().BeEquivalentTo(new{
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
                ItExpr.IsAny<CancellationToken>())
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
                ItExpr.IsAny<CancellationToken>())
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
}
