using System.Net;
using System.Net.Http.Json;
using AutoFixture;
using FluentAssertions;
using GUI_Crypto.Clients;
using GUI_Crypto.Models.Input;
using GUI_Crypto.Models.Output;
using Moq;
using Moq.Contrib.HttpClient;

namespace GUI_Crypto.Tests.Unit.Clients;

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
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Get, "https://example.com/coins/all");
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
    public async Task CreateCoin_CorrectUrlIsCalled()
    {
        // Arrange
        var newCoin = _fixture.Create<CoinNew>();

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Post, url => true)
            .ReturnsResponse(HttpStatusCode.OK);

        // Act
        await _client.CreateCoin(newCoin);

        // Assert
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Post, "https://example.com/coins/insert");
    }

    [Fact]
    public async Task DeleteCoin_CorrectUrlIsCalled()
    {
        // Arrange
        var coinId = _fixture.Create<int>();

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Delete, url => true)
            .ReturnsResponse(HttpStatusCode.OK);

        // Act
        await _client.DeleteCoin(coinId);

        // Assert
        _httpMessageHandlerMock.VerifyRequest(
            HttpMethod.Delete,
            $"https://example.com/coins/{coinId}"
        );
    }

    [Fact]
    public async Task CreateTradingPair_CorrectUrlIsCalled()
    {
        // Arrange
        var newTradingPair = _fixture.Create<TradingPairNew>();

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Post, url => true)
            .ReturnsResponse(HttpStatusCode.OK, JsonContent.Create(It.IsAny<int>()));

        // Act
        await _client.CreateTradingPair(newTradingPair);

        // Assert
        _httpMessageHandlerMock.VerifyRequest(
            HttpMethod.Post,
            "https://example.com/coins/tradingPairs/insert"
        );
    }

    [Fact]
    public async Task CreateTradingPair_ShouldReturnInsertedId()
    {
        // Arrange
        var newTradingPair = _fixture.Create<TradingPairNew>();
        var expectedId = _fixture.Create<int>();

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Post, url => true)
            .ReturnsResponse(HttpStatusCode.OK, JsonContent.Create(expectedId));

        // Act
        var result = await _client.CreateTradingPair(newTradingPair);

        // Assert
        result.Should().Be(expectedId);
    }

    [Fact]
    public async Task GetCoinsByIds_CorrectUrlIsCalled()
    {
        // Arrange
        var coinIds = _fixture.CreateMany<int>(3).ToList();

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, url => true)
            .ReturnsResponse(HttpStatusCode.OK, JsonContent.Create(new List<Coin>()));

        // Act
        await _client.GetCoinsByIds(coinIds);

        // Assert
        _httpMessageHandlerMock.VerifyRequest(
            HttpMethod.Get,
            $"https://example.com/coins/byIds?ids={string.Join(",", coinIds)}"
        );
    }

    [Fact]
    public async Task GetCoinsByIds_ShouldReturnListOfCoins()
    {
        // Arrange
        var coinIds = _fixture.CreateMany<int>(3).ToList();
        var expectedCoins = _fixture.CreateMany<Coin>();

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, url => true)
            .ReturnsResponse(HttpStatusCode.OK, JsonContent.Create(expectedCoins));

        // Act
        var result = await _client.GetCoinsByIds(coinIds);

        // Assert
        result.Should().BeEquivalentTo(expectedCoins);
    }

    [Fact]
    public async Task GetCoinsByIds_ShouldReturnEmptyList()
    {
        // Arrange
        var coinIds = _fixture.CreateMany<int>(3).ToList();

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, url => true)
            .ReturnsResponse(HttpStatusCode.OK, JsonContent.Create(new List<Coin>()));

        // Act
        var result = await _client.GetCoinsByIds(coinIds);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetQuoteCoinsPrioritized_CorrectUrlIsCalled()
    {
        // Arrange
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, url => true)
            .ReturnsResponse(HttpStatusCode.OK, JsonContent.Create(new List<Coin>()));

        // Act
        await _client.GetQuoteCoinsPrioritized();

        // Assert
        _httpMessageHandlerMock.VerifyRequest(
            HttpMethod.Get,
            "https://example.com/coins/quoteCoinsPrioritized"
        );
    }

    [Fact]
    public async Task GetQuoteCoinsPrioritized_ShouldReturnListOfCoins()
    {
        // Arrange
        var expectedCoins = _fixture.CreateMany<Coin>();

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, url => true)
            .ReturnsResponse(HttpStatusCode.OK, JsonContent.Create(expectedCoins));

        // Act
        var result = await _client.GetQuoteCoinsPrioritized();

        // Assert
        result.Should().BeEquivalentTo(expectedCoins);
    }

    [Fact]
    public async Task GetQuoteCoinsPrioritized_ShouldReturnEmptyList()
    {
        // Arrange
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, url => true)
            .ReturnsResponse(HttpStatusCode.OK, JsonContent.Create(new List<Coin>()));

        // Act
        var result = await _client.GetQuoteCoinsPrioritized();

        // Assert
        result.Should().BeEmpty();
    }
}
