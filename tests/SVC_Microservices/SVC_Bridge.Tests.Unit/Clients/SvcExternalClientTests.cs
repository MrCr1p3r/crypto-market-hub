using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using AutoFixture;
using Cysharp.Web;
using FluentAssertions;
using Moq;
using Moq.Contrib.HttpClient;
using SVC_Bridge.Clients;
using SVC_Bridge.Models.Input;
using SVC_Bridge.Models.Output;

namespace SVC_Bridge.Tests.Unit.Clients;

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
            .ReturnsResponse(HttpStatusCode.OK, JsonContent.Create(new List<KlineData>()));

        // Act
        await _client.GetKlineData(request);

        // Assert
        _httpMessageHandlerMock.VerifyRequest(
            HttpMethod.Get,
            $"https://example.com/exchanges/klineData?{queryString}"
        );
    }

    [Fact]
    public async Task GetKlineData_ShouldReturnListOfKlineData()
    {
        // Arrange
        var request = _fixture.Create<KlineDataRequest>();
        var expectedKlineData = _fixture.CreateMany<KlineData>();

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, url => true)
            .ReturnsResponse(HttpStatusCode.OK, JsonContent.Create(expectedKlineData));

        // Act
        var result = await _client.GetKlineData(request);

        // Assert
        result.Should().BeEquivalentTo(expectedKlineData);
    }

    [Fact]
    public async Task GetKlineData_ShouldReturnEmptyList()
    {
        // Arrange
        var request = _fixture.Create<KlineDataRequest>();

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, url => true)
            .ReturnsResponse(HttpStatusCode.OK, JsonContent.Create(new List<KlineData>()));

        // Act
        var result = await _client.GetKlineData(request);

        // Assert
        result.Should().BeEmpty();
    }
}
