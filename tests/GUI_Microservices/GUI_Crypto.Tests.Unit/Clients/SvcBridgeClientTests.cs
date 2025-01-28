using System.Net;
using AutoFixture;
using FluentAssertions;
using GUI_Crypto.Clients;
using GUI_Crypto.Models.Input;
using Moq;
using Moq.Contrib.HttpClient;

namespace GUI_Crypto.Tests.Unit.Clients;

public class SvcBridgeClientTests
{
    private readonly IFixture _fixture;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly SvcBridgeClient _client;

    public SvcBridgeClientTests()
    {
        _fixture = new Fixture();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();

        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        var httpClient = _httpMessageHandlerMock.CreateClient();
        httpClient.BaseAddress = new Uri("https://example.com");

        _httpClientFactoryMock
            .Setup(factory => factory.CreateClient("SvcBridgeClient"))
            .Returns(httpClient);

        _client = new SvcBridgeClient(_httpClientFactoryMock.Object);
    }

    [Fact]
    public async Task UpdateEntireKlineData_CorrectUrlIsCalled()
    {
        // Arrange
        var request = _fixture.Create<KlineDataUpdateRequest>();

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Post, "https://example.com/bridge/kline/updateEntireKlineData")
            .ReturnsResponse(HttpStatusCode.OK);

        // Act
        await _client.UpdateEntireKlineData(request);

        // Assert
        _httpMessageHandlerMock.VerifyRequest(
            HttpMethod.Post,
            "https://example.com/bridge/kline/updateEntireKlineData",
            Times.Once()
        );
    }

    [Fact]
    public async Task UpdateEntireKlineData_WhenRequestFails_ThrowsHttpRequestException()
    {
        // Arrange
        var request = _fixture.Create<KlineDataUpdateRequest>();

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Post, "https://example.com/bridge/kline/updateEntireKlineData")
            .ReturnsResponse(HttpStatusCode.BadRequest);

        // Act
        var act = () => _client.UpdateEntireKlineData(request);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }
}
