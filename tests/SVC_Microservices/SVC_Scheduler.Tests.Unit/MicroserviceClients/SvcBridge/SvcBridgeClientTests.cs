using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Testing;
using Moq.Contrib.HttpClient;
using SVC_Scheduler.MicroserviceClients.SvcBridge;
using SVC_Scheduler.MicroserviceClients.SvcBridge.Responses;
using SVC_Scheduler.MicroserviceClients.SvcBridge.Responses.Coins;
using SVC_Scheduler.MicroserviceClients.SvcBridge.Responses.KlineData;
using static SharedLibrary.Errors.GenericErrors;

namespace SVC_Scheduler.Tests.Unit.MicroserviceClients.SvcBridge;

/// <summary>
/// Unit tests for SvcBridgeClient class.
/// </summary>
public class SvcBridgeClientTests
{
    private readonly IFixture _fixture;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly FakeLogger<SvcBridgeClient> _logger;
    private readonly SvcBridgeClient _testedClient;

    public SvcBridgeClientTests()
    {
        _fixture = new Fixture();

        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        var httpClient = _httpMessageHandlerMock.CreateClient();
        httpClient.BaseAddress = new Uri("https://example.com");

        _logger = new FakeLogger<SvcBridgeClient>();

        _testedClient = new SvcBridgeClient(httpClient, _logger);
    }

    #region UpdateCoinsMarketData Tests

    [Fact]
    public async Task UpdateCoinsMarketData_CallsCorrectUrl()
    {
        // Arrange
        var expectedMarketData = _fixture.CreateMany<CoinMarketData>();
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Post, url => true)
            .ReturnsJsonResponse(HttpStatusCode.OK, expectedMarketData);

        var expectedUrl = "https://example.com/bridge/coins/market-data";

        // Act
        await _testedClient.UpdateCoinsMarketData();

        // Assert
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Post, expectedUrl);
    }

    [Fact]
    public async Task UpdateCoinsMarketData_OnSuccess_ReturnsListOfCoinMarketData()
    {
        // Arrange
        var expectedMarketData = _fixture.CreateMany<CoinMarketData>(3).ToList();
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Post, url => true)
            .ReturnsJsonResponse(HttpStatusCode.OK, expectedMarketData);

        // Act
        var result = await _testedClient.UpdateCoinsMarketData();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expectedMarketData);
        result.Value.Should().HaveCount(3);
    }

    [Fact]
    public async Task UpdateCoinsMarketData_OnSuccess_ReturnsEmptyList()
    {
        // Arrange
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Post, url => true)
            .ReturnsJsonResponse(HttpStatusCode.OK, new List<CoinMarketData>());

        // Act
        var result = await _testedClient.UpdateCoinsMarketData();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateCoinsMarketData_OnClientError_ReturnsFailedResult()
    {
        // Arrange
        var problemDetails = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title = "Bad Request",
            Status = 400,
            Detail = "Invalid request for updating coins market data.",
            Instance = "/bridge/coins/market-data",
        };

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Post, _ => true)
            .ReturnsJsonResponse(HttpStatusCode.BadRequest, problemDetails);

        // Act
        var result = await _testedClient.UpdateCoinsMarketData();

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Should().BeOfType<BadRequestError>();
    }

    [Fact]
    public async Task UpdateCoinsMarketData_OnServerError_ReturnsFailedResult()
    {
        // Arrange
        var problemDetails = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Title = "Internal Server Error",
            Status = 500,
            Detail = "An error occurred while updating coins market data.",
            Instance = "/bridge/coins/market-data",
        };

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Post, _ => true)
            .ReturnsJsonResponse(HttpStatusCode.InternalServerError, problemDetails);

        // Act
        var result = await _testedClient.UpdateCoinsMarketData();

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Should().BeOfType<InternalError>();
    }

    [Fact]
    public async Task UpdateCoinsMarketData_SendsEmptyPostRequest()
    {
        // Arrange
        var expectedMarketData = _fixture.CreateMany<CoinMarketData>();
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Post, url => true)
            .ReturnsJsonResponse(HttpStatusCode.OK, expectedMarketData);

        // Act
        await _testedClient.UpdateCoinsMarketData();

        // Assert
        _httpMessageHandlerMock.VerifyRequest(
            HttpMethod.Post,
            httpRequest =>
            {
                httpRequest.RequestUri!.ToString().Should().Contain("bridge/coins/market-data");
                return true;
            }
        );
    }

    #endregion

    #region UpdateKlineData Tests

    [Fact]
    public async Task UpdateKlineData_CallsCorrectUrl()
    {
        // Arrange
        var expectedKlineData = _fixture.CreateMany<KlineDataResponse>();
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Post, url => true)
            .ReturnsJsonResponse(HttpStatusCode.OK, expectedKlineData);

        var expectedUrl = "https://example.com/bridge/kline";

        // Act
        await _testedClient.UpdateKlineData();

        // Assert
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Post, expectedUrl);
    }

    [Fact]
    public async Task UpdateKlineData_OnSuccess_ReturnsListOfKlineDataResponses()
    {
        // Arrange
        var expectedKlineData = _fixture.CreateMany<KlineDataResponse>(5).ToList();
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Post, url => true)
            .ReturnsJsonResponse(HttpStatusCode.OK, expectedKlineData);

        // Act
        var result = await _testedClient.UpdateKlineData();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expectedKlineData);
        result.Value.Should().HaveCount(5);
    }

    [Fact]
    public async Task UpdateKlineData_OnSuccess_ReturnsEmptyList()
    {
        // Arrange
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Post, url => true)
            .ReturnsJsonResponse(HttpStatusCode.OK, new List<KlineDataResponse>());

        // Act
        var result = await _testedClient.UpdateKlineData();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateKlineData_OnClientError_ReturnsFailedResult()
    {
        // Arrange
        var problemDetails = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title = "Bad Request",
            Status = 400,
            Detail = "Invalid request for updating kline data.",
            Instance = "/bridge/kline",
        };

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Post, _ => true)
            .ReturnsJsonResponse(HttpStatusCode.BadRequest, problemDetails);

        // Act
        var result = await _testedClient.UpdateKlineData();

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Should().BeOfType<BadRequestError>();
    }

    [Fact]
    public async Task UpdateKlineData_OnServerError_ReturnsFailedResult()
    {
        // Arrange
        var problemDetails = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Title = "Internal Server Error",
            Status = 500,
            Detail = "An error occurred while updating kline data.",
            Instance = "/bridge/kline",
        };

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Post, _ => true)
            .ReturnsJsonResponse(HttpStatusCode.InternalServerError, problemDetails);

        // Act
        var result = await _testedClient.UpdateKlineData();

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Should().BeOfType<InternalError>();
    }

    [Fact]
    public async Task UpdateKlineData_SendsEmptyPostRequest()
    {
        // Arrange
        var expectedKlineData = _fixture.CreateMany<KlineDataResponse>();
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Post, url => true)
            .ReturnsJsonResponse(HttpStatusCode.OK, expectedKlineData);

        // Act
        await _testedClient.UpdateKlineData();

        // Assert
        _httpMessageHandlerMock.VerifyRequest(
            HttpMethod.Post,
            httpRequest =>
            {
                httpRequest.RequestUri!.ToString().Should().Contain("bridge/kline");
                return true;
            }
        );
    }

    #endregion

    #region UpdateTradingPairs Tests

    [Fact]
    public async Task UpdateTradingPairs_CallsCorrectUrl()
    {
        // Arrange
        var expectedCoins = _fixture.CreateMany<Coin>();
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Post, url => true)
            .ReturnsJsonResponse(HttpStatusCode.OK, expectedCoins);

        var expectedUrl = "https://example.com/bridge/trading-pairs";

        // Act
        await _testedClient.UpdateTradingPairs();

        // Assert
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Post, expectedUrl);
    }

    [Fact]
    public async Task UpdateTradingPairs_OnSuccess_ReturnsListOfCoins()
    {
        // Arrange
        var expectedCoins = _fixture.CreateMany<Coin>(4).ToList();
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Post, url => true)
            .ReturnsJsonResponse(HttpStatusCode.OK, expectedCoins);

        // Act
        var result = await _testedClient.UpdateTradingPairs();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expectedCoins);
        result.Value.Should().HaveCount(4);
    }

    [Fact]
    public async Task UpdateTradingPairs_OnSuccess_ReturnsEmptyList()
    {
        // Arrange
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Post, url => true)
            .ReturnsJsonResponse(HttpStatusCode.OK, new List<Coin>());

        // Act
        var result = await _testedClient.UpdateTradingPairs();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateTradingPairs_OnClientError_ReturnsFailedResult()
    {
        // Arrange
        var problemDetails = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title = "Bad Request",
            Status = 400,
            Detail = "Invalid request for updating trading pairs.",
            Instance = "/bridge/trading-pairs",
        };

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Post, _ => true)
            .ReturnsJsonResponse(HttpStatusCode.BadRequest, problemDetails);

        // Act
        var result = await _testedClient.UpdateTradingPairs();

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Should().BeOfType<BadRequestError>();
    }

    [Fact]
    public async Task UpdateTradingPairs_OnServerError_ReturnsFailedResult()
    {
        // Arrange
        var problemDetails = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Title = "Internal Server Error",
            Status = 500,
            Detail = "An error occurred while updating trading pairs.",
            Instance = "/bridge/trading-pairs",
        };

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Post, _ => true)
            .ReturnsJsonResponse(HttpStatusCode.InternalServerError, problemDetails);

        // Act
        var result = await _testedClient.UpdateTradingPairs();

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Should().BeOfType<InternalError>();
    }

    [Fact]
    public async Task UpdateTradingPairs_SendsEmptyPostRequest()
    {
        // Arrange
        var expectedCoins = _fixture.CreateMany<Coin>();
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Post, url => true)
            .ReturnsJsonResponse(HttpStatusCode.OK, expectedCoins);

        // Act
        await _testedClient.UpdateTradingPairs();

        // Assert
        _httpMessageHandlerMock.VerifyRequest(
            HttpMethod.Post,
            httpRequest =>
            {
                httpRequest.RequestUri!.ToString().Should().Contain("bridge/trading-pairs");
                return true;
            }
        );
    }

    #endregion
}
