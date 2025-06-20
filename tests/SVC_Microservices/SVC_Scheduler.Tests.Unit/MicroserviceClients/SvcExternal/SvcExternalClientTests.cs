using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Testing;
using Moq.Contrib.HttpClient;
using SVC_Scheduler.MicroserviceClients.SvcExternal;
using SVC_Scheduler.MicroserviceClients.SvcExternal.Contracts.Responses.Coins;
using static SharedLibrary.Errors.GenericErrors;

namespace SVC_Scheduler.Tests.Unit.MicroserviceClients.SvcExternal;

public class SvcExternalClientTests
{
    private readonly IFixture _fixture;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly FakeLogger<SvcExternalClient> _logger;
    private readonly SvcExternalClient _testedClient;

    public SvcExternalClientTests()
    {
        _fixture = new Fixture();

        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        var httpClient = _httpMessageHandlerMock.CreateClient();
        httpClient.BaseAddress = new Uri("https://example.com");

        _logger = new FakeLogger<SvcExternalClient>();

        _testedClient = new SvcExternalClient(httpClient, _logger);
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
        await _testedClient.GetAllSpotCoins();

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
        var result = await _testedClient.GetAllSpotCoins();

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
        var result = await _testedClient.GetAllSpotCoins();

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
        var result = await _testedClient.GetAllSpotCoins();

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
        var result = await _testedClient.GetAllSpotCoins();

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Should().BeOfType<InternalError>();
    }
}
