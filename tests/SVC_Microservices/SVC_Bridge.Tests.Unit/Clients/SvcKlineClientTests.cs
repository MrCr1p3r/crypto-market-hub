using System.Net;
using AutoFixture;
using Moq;
using Moq.Contrib.HttpClient;
using SVC_Bridge.Clients;
using SVC_Bridge.Models.Input;

namespace SVC_Bridge.Tests.Unit.Clients;

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
    public async Task ReplaceAllKlineData_CorrectUrlIsCalled()
    {
        // Arrange
        var newKlineData = _fixture.CreateMany<KlineDataNew>();

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Put, url => true)
            .ReturnsResponse(HttpStatusCode.OK);

        // Act
        await _client.ReplaceAllKlineData(newKlineData);

        // Assert
        _httpMessageHandlerMock.VerifyRequest(
            HttpMethod.Put,
            "https://example.com/kline/replaceAll"
        );
    }
}
