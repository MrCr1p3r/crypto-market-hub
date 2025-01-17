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

public class SvcKlineClientTests
{
    private readonly IFixture _fixture;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly SvcKlineClient _client;

    public SvcKlineClientTests()
    {
        _fixture = new Fixture();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();

        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        var httpClient = _httpMessageHandlerMock.CreateClient();
        httpClient.BaseAddress = new Uri("https://example.com");

        _httpClientFactoryMock
            .Setup(factory => factory.CreateClient("SvcKlineClient"))
            .Returns(httpClient);

        _client = new SvcKlineClient(_httpClientFactoryMock.Object);
    }

    [Fact]
    public async Task InsertKlineData_CorrectUrlIsCalled()
    {
        // Arrange
        var klineData = _fixture.Create<KlineDataNew>();

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Post, url => true)
            .ReturnsResponse(HttpStatusCode.OK);

        // Act
        await _client.InsertKlineData(klineData);

        // Assert
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Post, "https://example.com/kline/insert");
    }

    [Fact]
    public async Task InsertManyKlineData_CorrectUrlIsCalled()
    {
        // Arrange
        var klineDataList = _fixture.CreateMany<KlineDataNew>();

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Post, url => true)
            .ReturnsResponse(HttpStatusCode.OK);

        // Act
        await _client.InsertManyKlineData(klineDataList);

        // Assert
        _httpMessageHandlerMock.VerifyRequest(
            HttpMethod.Post,
            "https://example.com/kline/insertMany"
        );
    }

    [Fact]
    public async Task GetAllKlineData_CorrectUrlIsCalled()
    {
        // Arrange
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, url => true)
            .ReturnsResponse(
                HttpStatusCode.OK,
                JsonContent.Create(new Dictionary<int, IEnumerable<KlineData>>())
            );

        // Act
        await _client.GetAllKlineData();

        // Assert
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Get, "https://example.com/kline/all");
    }

    [Fact]
    public async Task GetAllKlineData_ShouldReturnExpectedKlineData()
    {
        // Arrange
        var expectedKlineData = new Dictionary<int, IEnumerable<KlineData>>
        {
            { _fixture.Create<int>(), _fixture.CreateMany<KlineData>() },
            { _fixture.Create<int>(), _fixture.CreateMany<KlineData>() },
        };

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, url => true)
            .ReturnsResponse(HttpStatusCode.OK, JsonContent.Create(expectedKlineData));

        // Act
        var result = await _client.GetAllKlineData();

        // Assert
        result.Should().BeEquivalentTo(expectedKlineData);
    }

    [Fact]
    public async Task GetAllKlineData_ShouldReturnEmptyDictionary()
    {
        // Arrange
        var expectedKlineData = new Dictionary<int, IEnumerable<KlineData>>();

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, url => true)
            .ReturnsResponse(HttpStatusCode.OK, JsonContent.Create(expectedKlineData));

        // Act
        var result = await _client.GetAllKlineData();

        // Assert
        result.Should().BeEquivalentTo(expectedKlineData);
    }

    [Fact]
    public async Task DeleteKlineDataForTradingPair_CorrectUrlIsCalled()
    {
        // Arrange
        var idTradePair = _fixture.Create<int>();

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Delete, url => true)
            .ReturnsResponse(HttpStatusCode.OK);

        // Act
        await _client.DeleteKlineDataForTradingPair(idTradePair);

        // Assert
        _httpMessageHandlerMock.VerifyRequest(
            HttpMethod.Delete,
            $"https://example.com/kline/{idTradePair}"
        );
    }
}
