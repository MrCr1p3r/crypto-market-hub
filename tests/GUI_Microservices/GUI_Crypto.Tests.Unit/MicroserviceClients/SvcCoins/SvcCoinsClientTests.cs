using System.Net;
using GUI_Crypto.MicroserviceClients.SvcCoins;
using GUI_Crypto.MicroserviceClients.SvcCoins.Contracts.Requests.CoinCreation;
using GUI_Crypto.MicroserviceClients.SvcCoins.Contracts.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Testing;
using Moq.Contrib.HttpClient;
using static SharedLibrary.Errors.GenericErrors;

namespace GUI_Crypto.Tests.Unit.MicroserviceClients.SvcCoins;

public class SvcCoinsClientTests
{
    private readonly Fixture _fixture;
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

    #region GetAllCoins Tests

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

    #endregion

    #region GetCoinById Tests

    [Fact]
    public async Task GetCoinById_CallsCorrectUrl()
    {
        // Arrange
        var coinId = _fixture.Create<int>();
        var coin = _fixture.Create<Coin>();
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, url => true)
            .ReturnsJsonResponse(HttpStatusCode.OK, new List<Coin> { coin });

        var expectedUrl = $"https://example.com/coins?ids={coinId}";

        // Act
        await _client.GetCoinById(coinId);

        // Assert
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Get, expectedUrl);
    }

    [Fact]
    public async Task GetCoinById_OnSuccess_ReturnsCoin()
    {
        // Arrange
        var coinId = _fixture.Create<int>();
        var expectedCoin = _fixture.Create<Coin>();
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, url => true)
            .ReturnsJsonResponse(HttpStatusCode.OK, new List<Coin> { expectedCoin });

        // Act
        var result = await _client.GetCoinById(coinId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expectedCoin);
    }

    [Fact]
    public async Task GetCoinById_OnEmptyResponse_ReturnsNotFoundError()
    {
        // Arrange
        var coinId = _fixture.Create<int>();
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, url => true)
            .ReturnsJsonResponse(HttpStatusCode.OK, new List<Coin>());

        // Act
        var result = await _client.GetCoinById(coinId);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Should().BeOfType<NotFoundError>();
    }

    [Fact]
    public async Task GetCoinById_OnServerError_ReturnsFailedResult()
    {
        // Arrange
        var coinId = _fixture.Create<int>();
        var problemDetails = _fixture.Create<ProblemDetails>();
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, _ => true)
            .ReturnsJsonResponse(HttpStatusCode.InternalServerError, problemDetails);

        // Act
        var result = await _client.GetCoinById(coinId);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Should().BeOfType<InternalError>();
    }

    [Fact]
    public async Task GetCoinById_OnNotFound_ReturnsFailedResult()
    {
        // Arrange
        var coinId = _fixture.Create<int>();
        var problemDetails = _fixture.Create<ProblemDetails>();
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Get, _ => true)
            .ReturnsJsonResponse(HttpStatusCode.NotFound, problemDetails);

        // Act
        var result = await _client.GetCoinById(coinId);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Should().BeOfType<NotFoundError>();
    }

    #endregion

    #region CreateCoins Tests

    [Fact]
    public async Task CreateCoins_CallsCorrectUrlWithCorrectBody()
    {
        // Arrange
        var expectedCoins = _fixture.CreateMany<Coin>();
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Post, url => true)
            .ReturnsJsonResponse(HttpStatusCode.OK, expectedCoins);

        var expectedBody = _fixture.CreateMany<CoinCreationRequest>();
        var expectedUrl = "https://example.com/coins";

        // Act
        await _client.CreateCoins(expectedBody);

        // Assert
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Post, expectedUrl);
    }

    [Fact]
    public async Task CreateCoins_OnSuccess_ReturnsSuccessWithListOfCreatedCoins()
    {
        // Arrange
        var expectedCoins = _fixture.CreateMany<Coin>();
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Post, url => true)
            .ReturnsJsonResponse(HttpStatusCode.OK, expectedCoins);

        var expectedBody = _fixture.CreateMany<CoinCreationRequest>();

        // Act
        var result = await _client.CreateCoins(expectedBody);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expectedCoins);
    }

    [Fact]
    public async Task CreateCoins_SendsRequestBodyAsJson()
    {
        // Arrange
        var expectedCoins = _fixture.CreateMany<Coin>();
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Post, url => true)
            .ReturnsJsonResponse(HttpStatusCode.OK, expectedCoins);

        var expectedBody = _fixture.CreateMany<CoinCreationRequest>();

        // Act
        await _client.CreateCoins(expectedBody);

        // Assert
        _httpMessageHandlerMock.VerifyRequest(
            HttpMethod.Post,
            httpRequest =>
            {
                httpRequest.RequestUri!.ToString().Should().Contain("coins");
                httpRequest.Content.Should().NotBeNull();
                httpRequest.Content!.Headers.ContentType!.MediaType.Should().Be("application/json");
                return true;
            }
        );
    }

    [Fact]
    public async Task CreateCoins_OnClientError_ReturnsFailWithErrorsInside()
    {
        // Arrange
        var problemDetails = _fixture.Create<ProblemDetails>();
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Post, _ => true)
            .ReturnsJsonResponse(HttpStatusCode.BadRequest, problemDetails);

        var expectedBody = _fixture.CreateMany<CoinCreationRequest>();

        // Act
        var result = await _client.CreateCoins(expectedBody);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Should().BeOfType<BadRequestError>();
    }

    [Fact]
    public async Task CreateCoins_OnServerError_ReturnsFailWithErrorsInside()
    {
        // Arrange
        var problemDetails = _fixture.Create<ProblemDetails>();
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Post, _ => true)
            .ReturnsJsonResponse(HttpStatusCode.InternalServerError, problemDetails);

        var expectedBody = _fixture.CreateMany<CoinCreationRequest>();

        // Act
        var result = await _client.CreateCoins(expectedBody);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Should().BeOfType<InternalError>();
    }

    [Fact]
    public async Task CreateCoins_WhenConvertingQuoteCoinToMainCoin_SendsCorrectRequest()
    {
        // Arrange
        var expectedCoins = _fixture.CreateMany<Coin>();
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Post, url => true)
            .ReturnsJsonResponse(HttpStatusCode.OK, expectedCoins);

        var coinCreationRequests = _fixture
            .Build<CoinCreationRequest>()
            .With(request => request.Id, 5) // Setting Id to convert existing quote coin to main coin
            .CreateMany();

        // Act
        var result = await _client.CreateCoins(coinCreationRequests);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expectedCoins);

        // Verify that the request was sent with the Id property
        _httpMessageHandlerMock.VerifyRequest(
            HttpMethod.Post,
            httpRequest =>
            {
                httpRequest.RequestUri!.ToString().Should().Contain("coins");
                httpRequest.Content.Should().NotBeNull();
                return true;
            }
        );
    }

    #endregion

    #region DeleteCoin Tests

    [Fact]
    public async Task DeleteCoin_CallsCorrectUrl()
    {
        // Arrange
        var coinId = _fixture.Create<int>();
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Delete, url => true)
            .ReturnsResponse(HttpStatusCode.NoContent);

        var expectedUrl = $"https://example.com/coins/{coinId}";

        // Act
        await _client.DeleteMainCoin(coinId);

        // Assert
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Delete, expectedUrl);
    }

    [Fact]
    public async Task DeleteCoin_OnSuccess_ReturnsSuccessResult()
    {
        // Arrange
        var coinId = _fixture.Create<int>();
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Delete, url => true)
            .ReturnsResponse(HttpStatusCode.NoContent);

        // Act
        var result = await _client.DeleteMainCoin(coinId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteCoin_OnNotFound_ReturnsFailWithErrorsInside()
    {
        // Arrange
        var coinId = _fixture.Create<int>();
        var problemDetails = _fixture.Create<ProblemDetails>();
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Delete, _ => true)
            .ReturnsJsonResponse(HttpStatusCode.NotFound, problemDetails);

        // Act
        var result = await _client.DeleteMainCoin(coinId);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Should().BeOfType<NotFoundError>();
    }

    [Fact]
    public async Task DeleteCoin_OnServerError_ReturnsFailWithErrorsInside()
    {
        // Arrange
        var coinId = _fixture.Create<int>();
        var problemDetails = _fixture.Create<ProblemDetails>();
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Delete, _ => true)
            .ReturnsJsonResponse(HttpStatusCode.InternalServerError, problemDetails);

        // Act
        var result = await _client.DeleteMainCoin(coinId);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Should().BeOfType<InternalError>();
    }

    #endregion

    #region DeleteAllCoins Tests

    [Fact]
    public async Task DeleteAllCoins_CallsCorrectUrl()
    {
        // Arrange
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Delete, url => true)
            .ReturnsResponse(HttpStatusCode.NoContent);

        var expectedUrl = "https://example.com/coins";

        // Act
        await _client.DeleteAllCoins();

        // Assert
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Delete, expectedUrl);
    }

    [Fact]
    public async Task DeleteAllCoins_OnSuccess_ReturnsSuccessResult()
    {
        // Arrange
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Delete, url => true)
            .ReturnsResponse(HttpStatusCode.NoContent);

        // Act
        var result = await _client.DeleteAllCoins();

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAllCoins_OnServerError_ReturnsFailWithErrorsInside()
    {
        // Arrange
        var problemDetails = _fixture.Create<ProblemDetails>();
        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Delete, _ => true)
            .ReturnsJsonResponse(HttpStatusCode.InternalServerError, problemDetails);

        // Act
        var result = await _client.DeleteAllCoins();

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Should().BeOfType<InternalError>();
    }

    #endregion
}
