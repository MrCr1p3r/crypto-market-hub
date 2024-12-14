using System.Net;
using System.Net.Http.Json;
using AutoFixture;
using FluentAssertions;
using Moq;
using Moq.Contrib.HttpClient;
using SVC_Bridge.Clients;
using SVC_Bridge.Models.Input;
using SVC_Bridge.Models.Output;

namespace SVC_Bridge.Tests.Unit.Clients;

public class SvcCoinsClientTests
{
    private readonly IFixture _fixture;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly SvcCoinsClient _client;

    public SvcCoinsClientTests()
    {
        _fixture = new Fixture();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();

        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        var httpClient = _httpMessageHandlerMock.CreateClient();
        httpClient.BaseAddress = new Uri("https://example.com");

        _httpClientFactoryMock
            .Setup(factory => factory.CreateClient("SvcCoinsClient"))
            .Returns(httpClient);

        _client = new SvcCoinsClient(_httpClientFactoryMock.Object);
    }

    [Fact]
    public async Task GetAllCoins_CorrectUrlIsCalled()
    {
        // Arrange
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, url => true)
            .ReturnsResponse(HttpStatusCode.OK, JsonContent.Create(new List<Coin>()));

        // Act
        await _client.GetAllCoins();

        // Assert
        _httpMessageHandlerMock.VerifyRequest(
            HttpMethod.Get,
            "https://example.com/api/coins/getAll"
        );
    }

    [Fact]
    public async Task GetAllCoins_ShouldReturnListOfCoins()
    {
        // Arrange
        var expectedCoins = _fixture.CreateMany<Coin>();

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, url => true)
            .ReturnsResponse(HttpStatusCode.OK, JsonContent.Create(expectedCoins));

        // Act
        var result = await _client.GetAllCoins();

        // Assert
        result.Should().BeEquivalentTo(expectedCoins);
    }

    [Fact]
    public async Task GetAllCoins_ShouldReturnEmptyList()
    {
        // Arrange
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, url => true)
            .ReturnsResponse(HttpStatusCode.OK, JsonContent.Create(new List<Coin>()));

        // Act
        var result = await _client.GetAllCoins();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task InsertTradingPair_CorrectUrlIsCalled()
    {
        // Arrange
        var newTradingPair = _fixture.Create<TradingPairNew>();

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Post, url => true)
            .ReturnsResponse(HttpStatusCode.OK, JsonContent.Create(It.IsAny<int>()));

        // Act
        await _client.InsertTradingPair(newTradingPair);

        // Assert
        _httpMessageHandlerMock.VerifyRequest(
            HttpMethod.Post,
            "https://example.com/api/coins/tradingPair/insert"
        );
    }

    [Fact]
    public async Task InsertTradingPair_ShouldReturnInsertedId()
    {
        // Arrange
        var newTradingPair = _fixture.Create<TradingPairNew>();
        var expectedId = _fixture.Create<int>();

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Post, url => true)
            .ReturnsResponse(HttpStatusCode.OK, JsonContent.Create(expectedId));

        // Act
        var result = await _client.InsertTradingPair(newTradingPair);

        // Assert
        result.Should().Be(expectedId);
    }
}
