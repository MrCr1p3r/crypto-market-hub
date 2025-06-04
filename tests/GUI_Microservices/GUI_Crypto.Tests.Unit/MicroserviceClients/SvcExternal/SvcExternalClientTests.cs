using System.Net;
using GUI_Crypto.MicroserviceClients.SvcExternal;
using GUI_Crypto.MicroserviceClients.SvcExternal.Contracts.Requests;
using GUI_Crypto.MicroserviceClients.SvcExternal.Contracts.Responses.Coins;
using GUI_Crypto.MicroserviceClients.SvcExternal.Contracts.Responses.KlineData;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Testing;
using Moq.Contrib.HttpClient;
using static SharedLibrary.Errors.GenericErrors;

namespace GUI_Crypto.Tests.Unit.MicroserviceClients.SvcExternal;

public class SvcExternalClientTests
{
    private readonly IFixture _fixture;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly FakeLogger<SvcExternalClient> _logger;
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

        _logger = new FakeLogger<SvcExternalClient>();

        _client = new SvcExternalClient(_httpClientFactoryMock.Object, _logger);
    }

    #region GetAllSpotCoins Tests

    [Fact]
    public async Task GetAllSpotCoins_CallsCorrectUrl()
    {
        // Arrange
        var expectedCoins = _fixture.CreateMany<Coin>();

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, url => true)
            .ReturnsJsonResponse(HttpStatusCode.OK, expectedCoins);

        // Act
        await _client.GetAllSpotCoins();

        // Assert
        _httpMessageHandlerMock.VerifyRequest(
            HttpMethod.Get,
            "https://example.com/exchanges/coins/spot"
        );
    }

    [Fact]
    public async Task GetAllSpotCoins_OnSuccess_ReturnsSuccessWithListOfCoins()
    {
        // Arrange
        var expectedCoins = _fixture.CreateMany<Coin>(5).ToList();

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, url => true)
            .ReturnsJsonResponse(HttpStatusCode.OK, expectedCoins);

        // Act
        var result = await _client.GetAllSpotCoins();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expectedCoins);
        result.Value.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetAllSpotCoins_OnSuccess_ReturnsEmptyList()
    {
        // Arrange
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, url => true)
            .ReturnsJsonResponse(HttpStatusCode.OK, new List<Coin>());

        // Act
        var result = await _client.GetAllSpotCoins();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllSpotCoins_OnClientError_ReturnsFailWithErrorsInside()
    {
        // Arrange
        var problemDetails = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title = "Bad Request",
            Status = 400,
            Detail = "Invalid request for spot coins.",
            Instance = "/exchanges/coins/spot",
        };

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, _ => true)
            .ReturnsJsonResponse(HttpStatusCode.BadRequest, problemDetails);

        // Act
        var result = await _client.GetAllSpotCoins();

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Should().BeOfType<BadRequestError>();
    }

    [Fact]
    public async Task GetAllSpotCoins_OnServerError_ReturnsFailWithErrorsInside()
    {
        // Arrange
        var problemDetails = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Title = "Internal Server Error",
            Status = 500,
            Detail = "An error occurred while processing the spot coins request.",
            Instance = "/exchanges/coins/spot",
        };

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, _ => true)
            .ReturnsJsonResponse(HttpStatusCode.InternalServerError, problemDetails);

        // Act
        var result = await _client.GetAllSpotCoins();

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Should().BeOfType<InternalError>();
    }

    #endregion

    #region GetKlineData Tests

    [Fact]
    public async Task GetKlineData_CallsCorrectUrl()
    {
        // Arrange
        var request = _fixture.Create<KlineDataRequest>();
        var expectedResponse = _fixture.Create<KlineDataResponse>();

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Post, url => true)
            .ReturnsJsonResponse(HttpStatusCode.OK, expectedResponse);

        // Act
        await _client.GetKlineData(request);

        // Assert
        _httpMessageHandlerMock.VerifyRequest(
            HttpMethod.Post,
            "https://example.com/exchanges/kline/query"
        );
    }

    [Fact]
    public async Task GetKlineData_OnSuccess_ReturnsSuccessWithKlineDataResponse()
    {
        // Arrange
        var request = _fixture.Create<KlineDataRequest>();
        var expectedResponse = _fixture.Create<KlineDataResponse>();

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Post, url => true)
            .ReturnsJsonResponse(HttpStatusCode.OK, expectedResponse);

        // Act
        var result = await _client.GetKlineData(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task GetKlineData_SendsRequestBodyAsJson()
    {
        // Arrange
        var request = _fixture.Create<KlineDataRequest>();
        var expectedResponse = _fixture.Create<KlineDataResponse>();

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Post, url => true)
            .ReturnsJsonResponse(HttpStatusCode.OK, expectedResponse);

        // Act
        await _client.GetKlineData(request);

        // Assert
        _httpMessageHandlerMock.VerifyRequest(
            HttpMethod.Post,
            httpRequest =>
            {
                httpRequest.RequestUri!.ToString().Should().Contain("exchanges/kline/query");
                httpRequest.Content.Should().NotBeNull();
                httpRequest.Content!.Headers.ContentType!.MediaType.Should().Be("application/json");
                return true;
            }
        );
    }

    [Fact]
    public async Task GetKlineData_OnClientError_ReturnsFailWithErrorsInside()
    {
        // Arrange
        var request = _fixture.Create<KlineDataRequest>();
        var problemDetails = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title = "Bad Request",
            Status = 400,
            Detail = "Invalid kline data request parameters.",
            Instance = "/exchanges/kline/query",
        };

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Post, _ => true)
            .ReturnsJsonResponse(HttpStatusCode.BadRequest, problemDetails);

        // Act
        var result = await _client.GetKlineData(request);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Should().BeOfType<BadRequestError>();
    }

    [Fact]
    public async Task GetKlineData_OnServerError_ReturnsFailWithErrorsInside()
    {
        // Arrange
        var request = _fixture.Create<KlineDataRequest>();
        var problemDetails = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Title = "Internal Server Error",
            Status = 500,
            Detail = "An error occurred while processing the kline data request.",
            Instance = "/exchanges/kline/query",
        };

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Post, _ => true)
            .ReturnsJsonResponse(HttpStatusCode.InternalServerError, problemDetails);

        // Act
        var result = await _client.GetKlineData(request);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Should().BeOfType<InternalError>();
    }

    [Fact]
    public async Task GetKlineData_WithEmptyKlineData_ReturnsSuccessWithEmptyData()
    {
        // Arrange
        var request = _fixture.Create<KlineDataRequest>();
        var expectedResponse = new KlineDataResponse
        {
            IdTradingPair = _fixture.Create<int>(),
            KlineData = [],
        };

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Post, url => true)
            .ReturnsJsonResponse(HttpStatusCode.OK, expectedResponse);

        // Act
        var result = await _client.GetKlineData(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expectedResponse);
        result.Value.KlineData.Should().BeEmpty();
    }

    [Fact]
    public async Task GetKlineData_WithValidKlineData_ReturnsSuccessWithData()
    {
        // Arrange
        var request = _fixture.Create<KlineDataRequest>();
        var expectedKlineData = _fixture.CreateMany<KlineData>(10).ToList();
        var expectedResponse = new KlineDataResponse
        {
            IdTradingPair = _fixture.Create<int>(),
            KlineData = expectedKlineData,
        };

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Post, url => true)
            .ReturnsJsonResponse(HttpStatusCode.OK, expectedResponse);

        // Act
        var result = await _client.GetKlineData(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expectedResponse);
        result.Value.KlineData.Should().HaveCount(10);
        result.Value.KlineData.Should().BeEquivalentTo(expectedKlineData);
    }

    #endregion
}
