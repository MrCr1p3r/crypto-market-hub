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

    #region CreateQuoteCoins Tests

    [Fact]
    public async Task CreateQuoteCoins_CallsCorrectUrlWithCorrectBody()
    {
        // Arrange
        var expectedQuoteCoins = _fixture.CreateMany<TradingPairCoinQuote>();
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Post, url => true)
            .ReturnsJsonResponse(HttpStatusCode.OK, expectedQuoteCoins);

        var expectedBody = _fixture.CreateMany<QuoteCoinCreationRequest>();
        var expectedUrl = "https://example.com/coins/quote";

        // Act
        await _client.CreateQuoteCoins(expectedBody);

        // Assert
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Post, expectedUrl);
    }

    [Fact]
    public async Task CreateQuoteCoins_OnSuccess_ReturnsSuccessWithListOfCreatedQuoteCoins()
    {
        // Arrange
        var expectedQuoteCoins = _fixture.CreateMany<TradingPairCoinQuote>();
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Post, url => true)
            .ReturnsJsonResponse(HttpStatusCode.OK, expectedQuoteCoins);

        var expectedBody = _fixture.CreateMany<QuoteCoinCreationRequest>();

        // Act
        var result = await _client.CreateQuoteCoins(expectedBody);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expectedQuoteCoins);
    }

    [Fact]
    public async Task CreateQuoteCoins_OnClientError_ReturnsFailWithErrorsInside()
    {
        // Arrange
        var problemDetails = _fixture.Create<ProblemDetails>();
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Post, _ => true)
            .ReturnsJsonResponse(HttpStatusCode.BadRequest, problemDetails);

        var expectedBody = _fixture.CreateMany<QuoteCoinCreationRequest>();

        // Act
        var result = await _client.CreateQuoteCoins(expectedBody);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Should().BeOfType<BadRequestError>();
    }

    [Fact]
    public async Task CreateQuoteCoins_OnServerError_ReturnsFailWithErrorsInside()
    {
        // Arrange
        var problemDetails = _fixture.Create<ProblemDetails>();
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Post, _ => true)
            .ReturnsJsonResponse(HttpStatusCode.InternalServerError, problemDetails);

        var expectedBody = _fixture.CreateMany<QuoteCoinCreationRequest>();

        // Act
        var result = await _client.CreateQuoteCoins(expectedBody);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Should().BeOfType<InternalError>();
    }

    #endregion

    #region ReplaceTradingPairs Tests

    [Fact]
    public async Task ReplaceTradingPairs_CallsCorrectUrlWithCorrectBody()
    {
        // Arrange
        var expectedCoins = _fixture.CreateMany<Coin>();
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Put, url => true)
            .ReturnsJsonResponse(HttpStatusCode.OK, expectedCoins);

        var expectedBody = _fixture.CreateMany<TradingPairCreationRequest>();
        var expectedUrl = "https://example.com/coins/trading-pairs";

        // Act
        await _client.ReplaceTradingPairs(expectedBody);

        // Assert
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Put, expectedUrl);
    }

    [Fact]
    public async Task ReplaceTradingPairs_OnSuccess_ReturnsSuccessWithListOfCoins()
    {
        // Arrange
        var expectedCoins = _fixture.CreateMany<Coin>();
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Put, url => true)
            .ReturnsJsonResponse(HttpStatusCode.OK, expectedCoins);

        var expectedBody = _fixture.CreateMany<TradingPairCreationRequest>();

        // Act
        var result = await _client.ReplaceTradingPairs(expectedBody);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expectedCoins);
    }

    [Fact]
    public async Task ReplaceTradingPairs_OnClientError_ReturnsFailWithErrorsInside()
    {
        // Arrange
        var problemDetails = _fixture.Create<ProblemDetails>();
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Put, _ => true)
            .ReturnsJsonResponse(HttpStatusCode.BadRequest, problemDetails);

        var expectedBody = _fixture.CreateMany<TradingPairCreationRequest>();

        // Act
        var result = await _client.ReplaceTradingPairs(expectedBody);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Should().BeOfType<BadRequestError>();
    }

    [Fact]
    public async Task ReplaceTradingPairs_OnServerError_ReturnsFailWithErrorsInside()
    {
        // Arrange
        var problemDetails = _fixture.Create<ProblemDetails>();
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Put, _ => true)
            .ReturnsJsonResponse(HttpStatusCode.InternalServerError, problemDetails);

        var expectedBody = _fixture.CreateMany<TradingPairCreationRequest>();

        // Act
        var result = await _client.ReplaceTradingPairs(expectedBody);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Should().BeOfType<InternalError>();
    }

    #endregion

    #region DeleteUnreferencedCoins Tests

    [Fact]
    public async Task DeleteUnreferencedCoins_CallsCorrectUrl()
    {
        // Arrange
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Delete, url => true)
            .ReturnsResponse(HttpStatusCode.NoContent);

        var expectedUrl = "https://example.com/coins/unreferenced";

        // Act
        await _client.DeleteUnreferencedCoins();

        // Assert
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Delete, expectedUrl);
    }

    [Fact]
    public async Task DeleteUnreferencedCoins_OnSuccess_ReturnsSuccessResult()
    {
        // Arrange
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Delete, url => true)
            .ReturnsResponse(HttpStatusCode.NoContent);

        // Act
        var result = await _client.DeleteUnreferencedCoins();

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteUnreferencedCoins_OnServerError_ReturnsFailWithErrorsInside()
    {
        // Arrange
        var problemDetails = _fixture.Create<ProblemDetails>();
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Delete, _ => true)
            .ReturnsJsonResponse(HttpStatusCode.InternalServerError, problemDetails);

        // Act
        var result = await _client.DeleteUnreferencedCoins();

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Should().BeOfType<InternalError>();
    }

    #endregion
}
