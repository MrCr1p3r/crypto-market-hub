using System.Net;
using GUI_Crypto.MicroserviceClients.SvcKline;
using GUI_Crypto.MicroserviceClients.SvcKline.Contracts.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Testing;
using Moq.Contrib.HttpClient;
using static SharedLibrary.Errors.GenericErrors;

namespace GUI_Crypto.Tests.Unit.MicroserviceClients.SvcKline;

public class SvcKlineClientTests
{
    private readonly IFixture _fixture;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly FakeLogger<SvcKlineClient> _logger;
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

        _logger = new FakeLogger<SvcKlineClient>();

        _client = new SvcKlineClient(_httpClientFactoryMock.Object, _logger);
    }

    #region GetAllKlineData Tests

    [Fact]
    public async Task GetAllKlineData_CallsCorrectUrl()
    {
        // Arrange
        var expectedResponse = _fixture.CreateMany<KlineDataResponse>();

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, url => true)
            .ReturnsJsonResponse(HttpStatusCode.OK, expectedResponse);

        // Act
        await _client.GetAllKlineData();

        // Assert
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Get, "https://example.com/kline");
    }

    [Fact]
    public async Task GetAllKlineData_OnSuccess_ReturnsSuccessWithKlineDataResponses()
    {
        // Arrange
        var expectedResponse = _fixture.CreateMany<KlineDataResponse>(3).ToList();

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, url => true)
            .ReturnsJsonResponse(HttpStatusCode.OK, expectedResponse);

        // Act
        var result = await _client.GetAllKlineData();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expectedResponse);
        result.Value.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetAllKlineData_OnSuccess_ReturnsEmptyList()
    {
        // Arrange
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, url => true)
            .ReturnsJsonResponse(HttpStatusCode.OK, new List<KlineDataResponse>());

        // Act
        var result = await _client.GetAllKlineData();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllKlineData_OnClientError_ReturnsFailWithErrorsInside()
    {
        // Arrange
        var problemDetails = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title = "Bad Request",
            Status = 400,
            Detail = "Invalid kline data request.",
            Instance = "/kline",
        };

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, _ => true)
            .ReturnsJsonResponse(HttpStatusCode.BadRequest, problemDetails);

        // Act
        var result = await _client.GetAllKlineData();

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Should().BeOfType<BadRequestError>();
    }

    [Fact]
    public async Task GetAllKlineData_OnServerError_ReturnsFailWithErrorsInside()
    {
        // Arrange
        var problemDetails = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Title = "Internal Server Error",
            Status = 500,
            Detail = "An error occurred while retrieving kline data.",
            Instance = "/kline",
        };

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, _ => true)
            .ReturnsJsonResponse(HttpStatusCode.InternalServerError, problemDetails);

        // Act
        var result = await _client.GetAllKlineData();

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Should().BeOfType<InternalError>();
    }

    [Fact]
    public async Task GetAllKlineData_WithMultipleTradingPairs_ReturnsSuccessWithAllData()
    {
        // Arrange
        var tradingPair1Data = new KlineDataResponse
        {
            IdTradingPair = 1,
            KlineData = [.. _fixture.CreateMany<KlineData>(5)],
        };

        var tradingPair2Data = new KlineDataResponse
        {
            IdTradingPair = 2,
            KlineData = [.. _fixture.CreateMany<KlineData>(3)],
        };

        var expectedResponse = new List<KlineDataResponse> { tradingPair1Data, tradingPair2Data };

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, url => true)
            .ReturnsJsonResponse(HttpStatusCode.OK, expectedResponse);

        // Act
        var result = await _client.GetAllKlineData();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().BeEquivalentTo(expectedResponse);

        var tradingPair1Result = result.Value.First(response => response.IdTradingPair == 1);
        tradingPair1Result.KlineData.Should().HaveCount(5);

        var tradingPair2Result = result.Value.First(response => response.IdTradingPair == 2);
        tradingPair2Result.KlineData.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetAllKlineData_WithEmptyKlineDataCollections_ReturnsSuccessWithEmptyKlineData()
    {
        // Arrange
        var tradingPairData = new KlineDataResponse { IdTradingPair = 1, KlineData = [] };

        var expectedResponse = new List<KlineDataResponse> { tradingPairData };

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, url => true)
            .ReturnsJsonResponse(HttpStatusCode.OK, expectedResponse);

        // Act
        var result = await _client.GetAllKlineData();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value.First().KlineData.Should().BeEmpty();
    }

    #endregion
}
