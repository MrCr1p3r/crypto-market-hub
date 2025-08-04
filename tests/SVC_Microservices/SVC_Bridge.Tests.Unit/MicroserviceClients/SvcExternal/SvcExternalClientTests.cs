using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Testing;
using Moq.Contrib.HttpClient;
using SVC_Bridge.MicroserviceClients.SvcExternal;
using SVC_Bridge.MicroserviceClients.SvcExternal.Contracts.Requests;
using SVC_Bridge.MicroserviceClients.SvcExternal.Contracts.Responses;
using SVC_Bridge.MicroserviceClients.SvcExternal.Contracts.Responses.Coins;
using static SharedLibrary.Errors.GenericErrors;

namespace SVC_Bridge.Tests.Unit.MicroserviceClients.SvcExternal;

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

    [Fact]
    public async Task GetCoinGeckoAssetsInfo_CallsCorrectUrlWithQueryString()
    {
        // Arrange
        var coinGeckoIds = new[] { "bitcoin", "ethereum", "tether" };
        var expectedAssets = _fixture.CreateMany<CoinGeckoAssetInfo>();

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Post, url => true)
            .ReturnsJsonResponse(HttpStatusCode.OK, expectedAssets);

        // Act
        await _client.GetCoinGeckoAssetsInfo(coinGeckoIds);

        // Assert
        _httpMessageHandlerMock.VerifyRequest(
            HttpMethod.Post,
            request =>
                request
                    .RequestUri!.ToString()
                    .Contains("market-data-providers/coingecko/assets-info/query")
        );
    }

    [Fact]
    public async Task GetCoinGeckoAssetsInfo_OnSuccess_ReturnsSuccessWithListOfAssets()
    {
        // Arrange
        var coinGeckoIds = new[] { "bitcoin", "ethereum" };
        var expectedAssets = _fixture.CreateMany<CoinGeckoAssetInfo>();

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Post, url => true)
            .ReturnsJsonResponse(HttpStatusCode.OK, expectedAssets);

        // Act
        var result = await _client.GetCoinGeckoAssetsInfo(coinGeckoIds);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expectedAssets);
    }

    [Fact]
    public async Task GetCoinGeckoAssetsInfo_OnSuccess_ReturnsEmptyList()
    {
        // Arrange
        var coinGeckoIds = new[] { "unknown-coin" };

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Post, url => true)
            .ReturnsJsonResponse(HttpStatusCode.OK, new List<CoinGeckoAssetInfo>());

        // Act
        var result = await _client.GetCoinGeckoAssetsInfo(coinGeckoIds);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCoinGeckoAssetsInfo_WithEmptyIds_CallsCorrectUrl()
    {
        // Arrange
        var coinGeckoIds = Array.Empty<string>();

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Post, url => true)
            .ReturnsJsonResponse(HttpStatusCode.OK, new List<CoinGeckoAssetInfo>());

        // Act
        await _client.GetCoinGeckoAssetsInfo(coinGeckoIds);

        // Assert
        _httpMessageHandlerMock.VerifyRequest(
            HttpMethod.Post,
            "https://example.com/market-data-providers/coingecko/assets-info/query"
        );
    }

    [Fact]
    public async Task GetCoinGeckoAssetsInfo_WithSingleId_CallsCorrectUrl()
    {
        // Arrange
        var coinGeckoIds = new[] { "bitcoin" };

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Post, url => true)
            .ReturnsJsonResponse(HttpStatusCode.OK, new List<CoinGeckoAssetInfo>());

        // Act
        await _client.GetCoinGeckoAssetsInfo(coinGeckoIds);

        // Assert
        _httpMessageHandlerMock.VerifyRequest(
            HttpMethod.Post,
            request =>
                request
                    .RequestUri!.ToString()
                    .Contains("market-data-providers/coingecko/assets-info/query")
        );
    }

    [Fact]
    public async Task GetCoinGeckoAssetsInfo_OnClientError_ReturnsFailWithErrorsInside()
    {
        // Arrange
        var coinGeckoIds = new[] { "bitcoin", "ethereum" };
        var problemDetails = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title = "Bad Request",
            Status = 400,
            Detail = "CoinGecko IDs must be provided.",
            Instance = "/market-data-providers/coingecko/assets-info/query",
        };

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Post, _ => true)
            .ReturnsJsonResponse(HttpStatusCode.BadRequest, problemDetails);

        // Act
        var result = await _client.GetCoinGeckoAssetsInfo(coinGeckoIds);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Should().BeOfType<BadRequestError>();
    }

    [Fact]
    public async Task GetCoinGeckoAssetsInfo_OnServerError_ReturnsFailWithErrorsInside()
    {
        // Arrange
        var coinGeckoIds = new[] { "bitcoin", "ethereum" };
        var problemDetails = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Title = "Internal Server Error",
            Status = 500,
            Detail = "An error occurred while processing the request.",
            Instance = "/market-data-providers/coingecko/assets-info/query",
        };

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Post, _ => true)
            .ReturnsJsonResponse(HttpStatusCode.InternalServerError, problemDetails);

        // Act
        var result = await _client.GetCoinGeckoAssetsInfo(coinGeckoIds);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Should().BeOfType<InternalError>();
    }

    [Fact]
    public async Task GetCoinGeckoAssetsInfo_WithSpecialCharactersInIds_CallsCorrectUrl()
    {
        // Arrange
        var coinGeckoIds = new[]
        {
            "coin-with-dashes",
            "coin_with_underscores",
            "coin with spaces",
        };

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Post, url => true)
            .ReturnsJsonResponse(HttpStatusCode.OK, new List<CoinGeckoAssetInfo>());

        // Act
        await _client.GetCoinGeckoAssetsInfo(coinGeckoIds);

        // Assert
        _httpMessageHandlerMock.VerifyRequest(
            HttpMethod.Post,
            request =>
                request
                    .RequestUri!.ToString()
                    .Contains("market-data-providers/coingecko/assets-info/query")
        );
    }

    [Fact]
    public async Task GetKlineData_CallsCorrectUrl()
    {
        // Arrange
        var request = _fixture.Create<KlineDataBatchRequest>();
        var expectedResponse = _fixture.CreateMany<KlineDataResponse>();

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Post, url => true)
            .ReturnsJsonResponse(HttpStatusCode.OK, expectedResponse);

        // Act
        await _client.GetKlineData(request);

        // Assert
        _httpMessageHandlerMock.VerifyRequest(
            HttpMethod.Post,
            "https://example.com/exchanges/kline/query/bulk"
        );
    }

    [Fact]
    public async Task GetKlineData_OnSuccess_ReturnsSuccessWithKlineDataResponses()
    {
        // Arrange
        var request = _fixture.Create<KlineDataBatchRequest>();
        var expectedResponse = _fixture.CreateMany<KlineDataResponse>(3).ToList();

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Post, url => true)
            .ReturnsJsonResponse(HttpStatusCode.OK, expectedResponse);

        // Act
        var result = await _client.GetKlineData(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expectedResponse);
        result.Value.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetKlineData_OnSuccess_ReturnsEmptyList()
    {
        // Arrange
        var request = _fixture.Create<KlineDataBatchRequest>();

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Post, url => true)
            .ReturnsJsonResponse(HttpStatusCode.OK, new List<KlineDataResponse>());

        // Act
        var result = await _client.GetKlineData(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetKlineData_SendsRequestBodyAsJson()
    {
        // Arrange
        var request = _fixture.Create<KlineDataBatchRequest>();
        var expectedResponse = _fixture.CreateMany<KlineDataResponse>();

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
                httpRequest.RequestUri!.ToString().Should().Contain("exchanges/kline/query/bulk");
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
        var request = _fixture.Create<KlineDataBatchRequest>();
        var problemDetails = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title = "Bad Request",
            Status = 400,
            Detail = "Invalid kline data request parameters.",
            Instance = "/exchanges/kline/query/bulk",
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
        var request = _fixture.Create<KlineDataBatchRequest>();
        var problemDetails = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Title = "Internal Server Error",
            Status = 500,
            Detail = "An error occurred while processing the kline data request.",
            Instance = "/exchanges/kline/query/bulk",
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
}
