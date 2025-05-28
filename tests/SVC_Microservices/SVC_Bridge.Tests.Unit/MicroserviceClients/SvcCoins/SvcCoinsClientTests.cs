using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Testing;
using Moq.Contrib.HttpClient;
using SVC_Bridge.MicroserviceClients.SvcCoins;
using SVC_Bridge.MicroserviceClients.SvcCoins.Contracts.Requests;
using SVC_Bridge.MicroserviceClients.SvcCoins.Contracts.Responses;
using static SharedLibrary.Errors.GenericErrors;

namespace SVC_Bridge.Tests.Unit.MicroserviceClients.SvcCoins;

public class SvcCoinsClientTests
{
    private readonly IFixture _fixture;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly FakeLogger<SvcCoinsClient> _logger;
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

        _logger = new FakeLogger<SvcCoinsClient>();

        _client = new SvcCoinsClient(_httpClientFactoryMock.Object, _logger);
    }

    [Fact]
    public async Task GetAllCoins_CallsCorrectUrl()
    {
        // Arrange
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, url => true)
            .ReturnsJsonResponse(HttpStatusCode.OK, new List<Coin>());

        var expectedUrl = "https://example.com/coins";

        // Act
        await _client.GetAllCoins();

        // Assert
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Get, expectedUrl);
    }

    [Fact]
    public async Task GetAllCoins_OnSuccess_ReturnsListOfCoins()
    {
        // Arrange
        var expectedCoins = _fixture.CreateMany<Coin>();
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, url => true)
            .ReturnsJsonResponse(HttpStatusCode.OK, expectedCoins);

        // Act
        var result = await _client.GetAllCoins();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expectedCoins);
    }

    [Fact]
    public async Task GetAllCoins_OnSuccess_ReturnsEmptyList()
    {
        // Arrange
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, url => true)
            .ReturnsJsonResponse(HttpStatusCode.OK, new List<Coin>());

        // Act
        var result = await _client.GetAllCoins();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllCoins_OnServerError_ReturnsFailedResult()
    {
        // Arrange
        var problemDetails = _fixture.Create<ProblemDetails>();
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, _ => true)
            .ReturnsJsonResponse(HttpStatusCode.InternalServerError, problemDetails);

        // Act
        var result = await _client.GetAllCoins();

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Should().BeOfType<InternalError>();
    }

    [Fact]
    public async Task UpdateCoinsMarketData_CallsCorrectUrlWithCorrectBody()
    {
        // Arrange
        var expectedCoins = _fixture.CreateMany<Coin>();
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Patch, url => true)
            .ReturnsJsonResponse(HttpStatusCode.OK, expectedCoins);

        var expectedBody = _fixture.CreateMany<CoinMarketDataUpdateRequest>();
        var expectedUrl = "https://example.com/coins/market-data";

        // Act
        await _client.UpdateCoinsMarketData(expectedBody);

        // Assert
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Patch, expectedUrl);
    }

    [Fact]
    public async Task UpdateCoinsMarketData_OnSuccess_ReturnsSuccessWithListOfUpdatedCoins()
    {
        // Arrange
        var expectedCoins = _fixture.CreateMany<Coin>();
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Patch, url => true)
            .ReturnsJsonResponse(HttpStatusCode.OK, expectedCoins);

        var expectedBody = _fixture.CreateMany<CoinMarketDataUpdateRequest>();

        // Act
        var result = await _client.UpdateCoinsMarketData(expectedBody);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expectedCoins);
    }

    [Fact]
    public async Task UpdateCoinsMarketData_OnClientError_ReturnsFailWithErrorsInside()
    {
        // Arrange
        var problemDetails = _fixture.Create<ProblemDetails>();
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Patch, _ => true)
            .ReturnsJsonResponse(HttpStatusCode.BadRequest, problemDetails);

        var expectedBody = _fixture.CreateMany<CoinMarketDataUpdateRequest>();

        // Act
        var result = await _client.UpdateCoinsMarketData(expectedBody);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Should().BeOfType<BadRequestError>();
    }

    [Fact]
    public async Task UpdateCoinsMarketData_OnServerError_ReturnsFailWithErrorsInside()
    {
        // Arrange
        var problemDetails = _fixture.Create<ProblemDetails>();
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Patch, _ => true)
            .ReturnsJsonResponse(HttpStatusCode.InternalServerError, problemDetails);

        var expectedBody = _fixture.CreateMany<CoinMarketDataUpdateRequest>();

        // Act
        var result = await _client.UpdateCoinsMarketData(expectedBody);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Should().BeOfType<InternalError>();
    }
}
