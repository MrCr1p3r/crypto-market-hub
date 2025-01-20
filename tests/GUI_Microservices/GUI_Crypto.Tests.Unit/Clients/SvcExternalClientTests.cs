using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using AutoFixture;
using Cysharp.Web;
using FluentAssertions;
using GUI_Crypto.Clients;
using GUI_Crypto.Models.Input;
using GUI_Crypto.Models.Output;
using Moq;
using Moq.Contrib.HttpClient;
using SVC_External.Models.Output;

namespace GUI_Crypto.Tests.Unit.Clients;

public class SvcExternalClientTests
{
    private readonly IFixture _fixture;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly SvcExternalClient _client;

    public SvcExternalClientTests()
    {
        _fixture = new Fixture();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();

        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        var httpClient = _httpMessageHandlerMock.CreateClient();
        httpClient.BaseAddress = new Uri("https://example.com");

        _httpClientFactoryMock
            .Setup(factory => factory.CreateClient("SvcExternalClient"))
            .Returns(httpClient);

        _client = new SvcExternalClient(_httpClientFactoryMock.Object);
    }

    [Fact]
    public async Task GetKlineData_CorrectUrlIsCalled()
    {
        // Arrange
        var request = _fixture.Create<KlineDataRequest>();
        var options = new WebSerializerOptions(WebSerializerProvider.Default)
        {
            CultureInfo = CultureInfo.InvariantCulture,
        };
        var queryString = WebSerializer.ToQueryString(request, options);

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, url => true)
            .ReturnsResponse(HttpStatusCode.OK, JsonContent.Create(new List<KlineDataExchange>()));

        // Act
        await _client.GetKlineData(request);

        // Assert
        _httpMessageHandlerMock.VerifyRequest(
            HttpMethod.Get,
            $"https://example.com/exchanges/klineData?{queryString}"
        );
    }

    [Fact]
    public async Task GetKlineData_ShouldReturnExpectedKlineData()
    {
        // Arrange
        var request = _fixture.Create<KlineDataRequest>();
        var expectedKlineData = _fixture.CreateMany<KlineDataExchange>();
        var options = new WebSerializerOptions(WebSerializerProvider.Default)
        {
            CultureInfo = CultureInfo.InvariantCulture,
        };
        var queryString = WebSerializer.ToQueryString(request, options);

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, url => true)
            .ReturnsResponse(HttpStatusCode.OK, JsonContent.Create(expectedKlineData));

        // Act
        var result = await _client.GetKlineData(request);

        // Assert
        result.Should().BeEquivalentTo(expectedKlineData);
    }

    [Fact]
    public async Task GetKlineData_ShouldReturnEmptyArray()
    {
        // Arrange
        var request = _fixture.Create<KlineDataRequest>();
        var expectedKlineData = new List<KlineDataExchange>();

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, url => true)
            .ReturnsResponse(HttpStatusCode.OK, JsonContent.Create(expectedKlineData));

        // Act
        var result = await _client.GetKlineData(request);

        // Assert
        result.Should().BeEquivalentTo(expectedKlineData);
    }

    [Fact]
    public async Task GetAllListedCoins_CorrectUrlIsCalled()
    {
        // Arrange
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, url => true)
            .ReturnsResponse(HttpStatusCode.OK, JsonContent.Create(new ListedCoins()));

        // Act
        await _client.GetAllListedCoins();

        // Assert
        _httpMessageHandlerMock.VerifyRequest(
            HttpMethod.Get,
            "https://example.com/exchanges/allListedCoins"
        );
    }

    [Fact]
    public async Task GetAllListedCoins_ShouldReturnExpectedCoins()
    {
        // Arrange
        var expectedCoins = _fixture.Create<ListedCoins>();

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, url => true)
            .ReturnsResponse(HttpStatusCode.OK, JsonContent.Create(expectedCoins));

        // Act
        var result = await _client.GetAllListedCoins();

        // Assert
        result.Should().BeEquivalentTo(expectedCoins);
    }

    [Fact]
    public async Task GetAllListedCoins_WhenResponseIsNull_ShouldReturnEmptyObject()
    {
        // Arrange
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, url => true)
            .ReturnsResponse(HttpStatusCode.OK, JsonContent.Create<ListedCoins>(null));

        // Act
        var result = await _client.GetAllListedCoins();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(new ListedCoins());
    }
}
