using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Testing;
using Moq.Contrib.HttpClient;
using SVC_Bridge.MicroserviceClients.SvcKline;
using SVC_Bridge.MicroserviceClients.SvcKline.Contracts.Requests;
using SVC_Bridge.MicroserviceClients.SvcKline.Contracts.Responses;
using static SharedLibrary.Errors.GenericErrors;

namespace SVC_Bridge.Tests.Unit.MicroserviceClients.SvcKline;

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

    [Fact]
    public async Task ReplaceKlineData_CallsCorrectUrl()
    {
        // Arrange
        var newKlineData = _fixture.CreateMany<KlineDataCreationRequest>(3);
        var expectedResponse = _fixture.CreateMany<KlineDataResponse>();

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Put, url => true)
            .ReturnsJsonResponse(HttpStatusCode.OK, expectedResponse);

        // Act
        await _client.ReplaceKlineData(newKlineData);

        // Assert
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Put, "https://example.com/kline");
    }

    [Fact]
    public async Task ReplaceKlineData_OnSuccess_ReturnsSuccessWithKlineDataResponses()
    {
        // Arrange
        var newKlineData = _fixture.CreateMany<KlineDataCreationRequest>(5);
        var expectedResponse = _fixture.CreateMany<KlineDataResponse>(2).ToList();

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Put, url => true)
            .ReturnsJsonResponse(HttpStatusCode.OK, expectedResponse);

        // Act
        var result = await _client.ReplaceKlineData(newKlineData);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expectedResponse);
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task ReplaceKlineData_SendsRequestBodyAsJson()
    {
        // Arrange
        var newKlineData = _fixture.CreateMany<KlineDataCreationRequest>(3);
        var expectedResponse = _fixture.CreateMany<KlineDataResponse>();

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Put, url => true)
            .ReturnsJsonResponse(HttpStatusCode.OK, expectedResponse);

        // Act
        await _client.ReplaceKlineData(newKlineData);

        // Assert
        _httpMessageHandlerMock.VerifyRequest(
            HttpMethod.Put,
            httpRequest =>
            {
                httpRequest.RequestUri!.ToString().Should().Contain("kline");
                httpRequest.Content.Should().NotBeNull();
                httpRequest.Content!.Headers.ContentType!.MediaType.Should().Be("application/json");
                return true;
            }
        );
    }

    [Fact]
    public async Task ReplaceKlineData_OnClientError_ReturnsFailWithErrorsInside()
    {
        // Arrange
        var newKlineData = _fixture.CreateMany<KlineDataCreationRequest>(3);
        var problemDetails = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title = "Bad Request",
            Status = 400,
            Detail = "Invalid kline data provided.",
            Instance = "/kline",
        };

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Put, _ => true)
            .ReturnsJsonResponse(HttpStatusCode.BadRequest, problemDetails);

        // Act
        var result = await _client.ReplaceKlineData(newKlineData);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Should().BeOfType<BadRequestError>();
    }

    [Fact]
    public async Task ReplaceKlineData_OnServerError_ReturnsFailWithErrorsInside()
    {
        // Arrange
        var newKlineData = _fixture.CreateMany<KlineDataCreationRequest>(3);
        var problemDetails = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Title = "Internal Server Error",
            Status = 500,
            Detail = "An error occurred while replacing kline data.",
            Instance = "/kline",
        };

        _httpMessageHandlerMock
            .SetupRequest(HttpMethod.Put, _ => true)
            .ReturnsJsonResponse(HttpStatusCode.InternalServerError, problemDetails);

        // Act
        var result = await _client.ReplaceKlineData(newKlineData);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Should().BeOfType<InternalError>();
    }
}
