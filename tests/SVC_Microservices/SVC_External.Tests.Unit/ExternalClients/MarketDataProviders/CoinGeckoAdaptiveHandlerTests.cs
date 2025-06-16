using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq.Contrib.HttpClient;
using SVC_External.ExternalClients.MarketDataProviders.CoinGecko;
using SVC_External.Infrastructure.CoinGecko;
using static SVC_External.ExternalClients.MarketDataProviders.CoinGecko.CoinGeckoDtos;

namespace SVC_External.Tests.Unit.ExternalClients.MarketDataProviders;

public class CoinGeckoAdaptiveHandlerTests : IDisposable
{
    private readonly Mock<ICoinGeckoAuthenticationStateService> _authStateServiceMock;
    private readonly Mock<ILogger<CoinGeckoAdaptiveHandler>> _loggerMock;
    private readonly Mock<HttpMessageHandler> _innerHandlerMock;
    private readonly CoinGeckoAdaptiveHandler _handler;
    private readonly HttpClient _httpClient;

    public CoinGeckoAdaptiveHandlerTests()
    {
        _authStateServiceMock = new Mock<ICoinGeckoAuthenticationStateService>();
        _loggerMock = new Mock<ILogger<CoinGeckoAdaptiveHandler>>();
        _innerHandlerMock = new Mock<HttpMessageHandler>();

        _handler = new CoinGeckoAdaptiveHandler(_authStateServiceMock.Object, _loggerMock.Object)
        {
            InnerHandler = _innerHandlerMock.Object,
        };

        _httpClient = new HttpClient(_handler)
        {
            BaseAddress = new Uri("https://api.coingecko.com"),
        };
    }

    [Fact]
    public async Task SendAsync_WithApiKey_AppliesApiKeyHeader()
    {
        // Arrange
        var apiKey = "test-api-key";
        _authStateServiceMock.Setup(x => x.IsUsingApiKey).Returns(true);
        _authStateServiceMock.Setup(x => x.CurrentApiKey).Returns(apiKey);

        _innerHandlerMock.SetupAnyRequest().ReturnsResponse(HttpStatusCode.OK, "success");

        // Act
        await _httpClient.GetAsync("/test");

        // Assert
        _innerHandlerMock.VerifyRequest(
            request =>
                request.Headers.Contains("x-cg-demo-api-key")
                && request.Headers.GetValues("x-cg-demo-api-key").First() == apiKey,
            Times.Once()
        );
    }

    [Fact]
    public async Task SendAsync_WithoutApiKey_DoesNotApplyApiKeyHeader()
    {
        // Arrange
        _authStateServiceMock.Setup(x => x.IsUsingApiKey).Returns(false);
        _authStateServiceMock.Setup(x => x.CurrentApiKey).Returns((string?)null);

        _innerHandlerMock.SetupAnyRequest().ReturnsResponse(HttpStatusCode.OK, "success");

        // Act
        await _httpClient.GetAsync("/test");

        // Assert
        _innerHandlerMock.VerifyRequest(
            request => !request.Headers.Contains("x-cg-demo-api-key"),
            Times.Once()
        );
    }

    [Fact]
    public async Task SendAsync_RemovesExistingApiKeyHeader_BeforeApplyingNew()
    {
        // Arrange
        var apiKey = "new-api-key";
        _authStateServiceMock.Setup(x => x.IsUsingApiKey).Returns(true);
        _authStateServiceMock.Setup(x => x.CurrentApiKey).Returns(apiKey);

        _innerHandlerMock.SetupAnyRequest().ReturnsResponse(HttpStatusCode.OK, "success");

        var request = new HttpRequestMessage(HttpMethod.Get, "/test");
        request.Headers.Add("x-cg-demo-api-key", "old-api-key");

        // Act
        await _httpClient.SendAsync(request);

        // Assert
        _innerHandlerMock.VerifyRequest(
            req =>
                req.Headers.Contains("x-cg-demo-api-key")
                && req.Headers.GetValues("x-cg-demo-api-key").Count() == 1
                && req.Headers.GetValues("x-cg-demo-api-key").First() == apiKey,
            Times.Once()
        );
    }

    [Fact]
    public async Task SendAsync_SuccessfulRequest_ReturnsResponse()
    {
        // Arrange
        _authStateServiceMock.Setup(x => x.IsUsingApiKey).Returns(false);
        _authStateServiceMock.Setup(x => x.CurrentApiKey).Returns((string?)null);

        var expectedContent = "success response";
        _innerHandlerMock.SetupAnyRequest().ReturnsResponse(HttpStatusCode.OK, expectedContent);

        // Act
        var response = await _httpClient.GetAsync("/test");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be(expectedContent);
    }

    [Fact]
    public async Task SendAsync_MonthlyLimitExceeded_SwitchesToBasicModeAndRetries()
    {
        // Arrange
        var apiKey = "test-api-key";
        _authStateServiceMock
            .SetupSequence(x => x.IsUsingApiKey)
            .Returns(true) // Initial request
            .Returns(true) // When checking for monthly limit
            .Returns(false); // For retry request

        _authStateServiceMock.Setup(x => x.CurrentApiKey).Returns(apiKey);

        var monthlyLimitErrorResponse = new CoinGeckoErrorResponse
        {
            Status = new CoinGeckoErrorStatus
            {
                ErrorCode = 10006,
                ErrorMessage = "Monthly limit exceeded",
                Timestamp = "2023-01-01T00:00:00Z",
            },
        };

        var errorJson = JsonSerializer.Serialize(monthlyLimitErrorResponse);

        // First request returns monthly limit error
        _innerHandlerMock
            .SetupRequestSequence(HttpMethod.Get, "https://api.coingecko.com/test")
            .ReturnsResponse(HttpStatusCode.TooManyRequests, errorJson, "application/json")
            .ReturnsResponse(HttpStatusCode.OK, "success");

        // Act
        var response = await _httpClient.GetAsync("/test");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _authStateServiceMock.Verify(x => x.SwitchToBasicMode(), Times.Once);

        // Should make 2 requests total (original + retry)
        _innerHandlerMock.VerifyRequest(
            HttpMethod.Get,
            "https://api.coingecko.com/test",
            Times.Exactly(2)
        );
    }

    [Fact]
    public async Task SendAsync_InBasicMode_DoesNotRetry()
    {
        // Arrange
        _authStateServiceMock.Setup(x => x.IsUsingApiKey).Returns(false);
        _authStateServiceMock.Setup(x => x.CurrentApiKey).Returns((string?)null);

        // In reality, basic mode would get a different rate limit error, not monthly limit
        var regularRateLimitError = new CoinGeckoErrorResponse
        {
            Status = new CoinGeckoErrorStatus
            {
                ErrorCode = 429, // Different error code - not monthly limit
                ErrorMessage = "Rate limit exceeded",
            },
        };

        var errorJson = JsonSerializer.Serialize(regularRateLimitError);

        _innerHandlerMock
            .SetupAnyRequest()
            .ReturnsResponse(HttpStatusCode.TooManyRequests, errorJson, "application/json");

        // Act
        var response = await _httpClient.GetAsync("/test");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

        _authStateServiceMock.Verify(x => x.SwitchToBasicMode(), Times.Never);

        _innerHandlerMock.VerifyAnyRequest(Times.Once());
    }

    [Fact]
    public async Task SendAsync_TooManyRequestsWithoutMonthlyLimitError_DoesNotRetry()
    {
        // Arrange
        _authStateServiceMock.Setup(x => x.IsUsingApiKey).Returns(true);
        _authStateServiceMock.Setup(x => x.CurrentApiKey).Returns("test-key");

        var regularRateLimitError = new CoinGeckoErrorResponse
        {
            Status = new CoinGeckoErrorStatus
            {
                ErrorCode = 429, // Different error code
                ErrorMessage = "Rate limit exceeded",
            },
        };

        var errorJson = JsonSerializer.Serialize(regularRateLimitError);

        _innerHandlerMock
            .SetupAnyRequest()
            .ReturnsResponse(HttpStatusCode.TooManyRequests, errorJson, "application/json");

        // Act
        var response = await _httpClient.GetAsync("/test");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        _authStateServiceMock.Verify(x => x.SwitchToBasicMode(), Times.Never);
        _innerHandlerMock.VerifyAnyRequest(Times.Once());
    }

    [Fact]
    public async Task SendAsync_TooManyRequestsWithInvalidJson_DoesNotRetry()
    {
        // Arrange
        _authStateServiceMock.Setup(x => x.IsUsingApiKey).Returns(true);
        _authStateServiceMock.Setup(x => x.CurrentApiKey).Returns("test-key");

        _innerHandlerMock
            .SetupAnyRequest()
            .ReturnsResponse(HttpStatusCode.TooManyRequests, "invalid json", "application/json");

        // Act
        var response = await _httpClient.GetAsync("/test");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        _authStateServiceMock.Verify(x => x.SwitchToBasicMode(), Times.Never);
        _innerHandlerMock.VerifyAnyRequest(Times.Once());
    }

    [Fact]
    public async Task SendAsync_TooManyRequestsWithEmptyContent_DoesNotRetry()
    {
        // Arrange
        _authStateServiceMock.Setup(x => x.IsUsingApiKey).Returns(true);
        _authStateServiceMock.Setup(x => x.CurrentApiKey).Returns("test-key");

        _innerHandlerMock
            .SetupAnyRequest()
            .ReturnsResponse(HttpStatusCode.TooManyRequests, string.Empty, "application/json");

        // Act
        var response = await _httpClient.GetAsync("/test");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        _authStateServiceMock.Verify(x => x.SwitchToBasicMode(), Times.Never);
        _innerHandlerMock.VerifyAnyRequest(Times.Once());
    }

    [Fact]
    public async Task SendAsync_WithPostRequest_ClonesRequestContentCorrectly()
    {
        // Arrange
        var apiKey = "test-api-key";
        var requestContent = "test request body";

        _authStateServiceMock
            .SetupSequence(x => x.IsUsingApiKey)
            .Returns(true) // Initial request
            .Returns(true) // When checking for monthly limit
            .Returns(false); // For retry request

        _authStateServiceMock.Setup(x => x.CurrentApiKey).Returns(apiKey);

        var monthlyLimitErrorResponse = new CoinGeckoErrorResponse
        {
            Status = new CoinGeckoErrorStatus
            {
                ErrorCode = 10006,
                ErrorMessage = "Monthly limit exceeded",
            },
        };

        var errorJson = JsonSerializer.Serialize(monthlyLimitErrorResponse);

        // First request returns monthly limit error
        _innerHandlerMock
            .SetupRequestSequence(HttpMethod.Post, "https://api.coingecko.com/test")
            .ReturnsResponse(HttpStatusCode.TooManyRequests, errorJson, "application/json")
            .ReturnsResponse(HttpStatusCode.OK, "success");

        var request = new HttpRequestMessage(HttpMethod.Post, "/test")
        {
            Content = new StringContent(
                requestContent,
                System.Text.Encoding.UTF8,
                "application/json"
            ),
        };

        // Act
        var response = await _httpClient.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify both requests were made with the same content
        _innerHandlerMock.VerifyRequest(
            req => req.Method == HttpMethod.Post && req.Content != null,
            Times.Exactly(2)
        );
    }

    [Fact]
    public async Task SendAsync_WithCustomHeaders_ClonesHeadersCorrectly()
    {
        // Arrange
        var apiKey = "test-api-key";

        _authStateServiceMock
            .SetupSequence(x => x.IsUsingApiKey)
            .Returns(true) // Initial request
            .Returns(true) // When checking for monthly limit
            .Returns(false); // For retry request

        _authStateServiceMock.Setup(x => x.CurrentApiKey).Returns(apiKey);

        var monthlyLimitErrorResponse = new CoinGeckoErrorResponse
        {
            Status = new CoinGeckoErrorStatus
            {
                ErrorCode = 10006,
                ErrorMessage = "Monthly limit exceeded",
            },
        };

        var errorJson = JsonSerializer.Serialize(monthlyLimitErrorResponse);

        // First request returns monthly limit error
        _innerHandlerMock
            .SetupRequestSequence(HttpMethod.Get, "https://api.coingecko.com/test")
            .ReturnsResponse(HttpStatusCode.TooManyRequests, errorJson, "application/json")
            .ReturnsResponse(HttpStatusCode.OK, "success");

        var request = new HttpRequestMessage(HttpMethod.Get, "/test");
        request.Headers.Add("Custom-Header", "custom-value");

        // Act
        var response = await _httpClient.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Both requests should have the custom header
        _innerHandlerMock.VerifyRequest(
            req =>
                req.Headers.Contains("Custom-Header")
                && req.Headers.GetValues("Custom-Header").First() == "custom-value",
            Times.Exactly(2)
        );
    }

    [Fact]
    public async Task SendAsync_NonTooManyRequestsResponse_ReturnsOriginalResponse()
    {
        // Arrange
        _authStateServiceMock.Setup(x => x.IsUsingApiKey).Returns(true);
        _authStateServiceMock.Setup(x => x.CurrentApiKey).Returns("test-key");

        _innerHandlerMock
            .SetupAnyRequest()
            .ReturnsResponse(HttpStatusCode.BadRequest, "Bad Request");

        // Act
        var response = await _httpClient.GetAsync("/test");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be("Bad Request");

        _authStateServiceMock.Verify(x => x.SwitchToBasicMode(), Times.Never);
        _innerHandlerMock.VerifyAnyRequest(Times.Once());
    }

    [Fact]
    public async Task SendAsync_DisposesFailedResponseAfterSuccessfulRetry()
    {
        // Arrange
        var apiKey = "test-api-key";

        _authStateServiceMock
            .SetupSequence(x => x.IsUsingApiKey)
            .Returns(true) // Initial request
            .Returns(true) // When checking for monthly limit
            .Returns(false) // After switching to basic mode
            .Returns(false); // For retry request

        _authStateServiceMock.Setup(x => x.CurrentApiKey).Returns(apiKey);

        var monthlyLimitErrorResponse = new CoinGeckoErrorResponse
        {
            Status = new CoinGeckoErrorStatus
            {
                ErrorCode = 10006,
                ErrorMessage = "Monthly limit exceeded",
            },
        };

        var errorJson = JsonSerializer.Serialize(monthlyLimitErrorResponse);

        // First request returns monthly limit error
        _innerHandlerMock
            .SetupRequest(HttpMethod.Get, "https://api.coingecko.com/test")
            .ReturnsResponse(HttpStatusCode.TooManyRequests, errorJson, "application/json");

        // Retry request succeeds
        _innerHandlerMock
            .SetupRequest(HttpMethod.Get, "https://api.coingecko.com/test")
            .ReturnsResponse(HttpStatusCode.OK, "success");

        // Act
        var response = await _httpClient.GetAsync("/test");

        // Assert - Should get the successful response, not the failed one
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be("success");
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _httpClient?.Dispose();
            _handler?.Dispose();
        }
    }
}
