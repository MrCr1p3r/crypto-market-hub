using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Moq.Contrib.HttpClient;
using SharedLibrary.Extensions.HttpClient.External;
using SharedLibrary.Extensions.Testing;
using static SharedLibrary.Errors.GenericErrors;

namespace SharedLibrary.Tests.Unit.Extensions.HttpClient.External;

public class ExternalHttpClientExtensionsTests
{
    private const string TestRequestUri = "http://localhost/api/test";
    private const string RelativeTestUri = "test";
    private const string FailureMessage = "Failed to retrieve data.";
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly System.Net.Http.HttpClient _httpClient;
    private readonly FakeLogger _fakeLogger;

    public ExternalHttpClientExtensionsTests()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = _mockHttpMessageHandler.CreateClient();
        _httpClient.BaseAddress = new Uri("http://localhost/api/");
        _fakeLogger = new FakeLogger();
    }

    [Fact]
    public async Task GetFromJsonSafeAsync_ReturnsOkResultWithContent()
    {
        // Arrange
        var expectedDto = new TestData.TestDto { Id = 1, Name = "Test Name" };
        _mockHttpMessageHandler
            .SetupRequest(HttpMethod.Get, TestRequestUri)
            .ReturnsJsonResponse(HttpStatusCode.OK, expectedDto);

        // Act
        var result = await _httpClient.GetFromJsonSafeAsync<TestData.TestDto>(
            RelativeTestUri,
            _fakeLogger,
            FailureMessage
        );

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expectedDto);

        // Verify no logs were written for successful case
        _fakeLogger.VerifyNoLogsWritten();
    }

    [Fact]
    public async Task GetFromJsonSafeAsync_ContentDeserializesToNull_ReturnsFailedDeserializationResult()
    {
        // Arrange
        _mockHttpMessageHandler
            .SetupRequest(HttpMethod.Get, TestRequestUri)
            .ReturnsJsonResponse(HttpStatusCode.OK, (object?)null);

        // Act
        var result = await _httpClient.GetFromJsonSafeAsync<TestData.TestDto>(
            RelativeTestUri,
            _fakeLogger,
            FailureMessage
        );

        // Assert
        result.Should().NotBeNull();
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e is InternalError && e.Message.Contains("null"));

        // Verify no logs were written for deserialization failure (not an HTTP error)
        _fakeLogger.VerifyNoLogsWritten();
    }

    [Fact]
    public async Task GetFromJsonSafeAsync_EmptyJsonObject_ReturnsOkResultWithDefaultProperties()
    {
        _mockHttpMessageHandler
            .SetupRequest(HttpMethod.Get, TestRequestUri)
            .ReturnsJsonResponse(HttpStatusCode.OK, new { });

        var result = await _httpClient.GetFromJsonSafeAsync<TestData.TestDto>(
            RelativeTestUri,
            _fakeLogger,
            FailureMessage
        );

        result.Should().NotBeNull();
        result
            .IsSuccess.Should()
            .BeTrue(
                "because an empty JSON object '{}' deserialized to a non-null class instance should not trigger the failure path."
            );
        result.Value.Should().BeEquivalentTo(new TestData.TestDto());

        // Verify no logs were written for successful case
        _fakeLogger.VerifyNoLogsWritten();
    }

    [Fact]
    public async Task GetFromJsonSafeAsync_ContentDeserializesToDefaultStruct_ReturnsFailedDeserializationResult()
    {
        _mockHttpMessageHandler
            .SetupRequest(HttpMethod.Get, TestRequestUri)
            .ReturnsJsonResponse(HttpStatusCode.OK, new { });

        var result = await _httpClient.GetFromJsonSafeAsync<TestData.TestStructDto>(
            RelativeTestUri,
            _fakeLogger,
            FailureMessage
        );

        result.Should().NotBeNull();
        result
            .IsFailed.Should()
            .BeTrue("because the deserialized struct is equal to default(TestStructDto)");
        result
            .Errors.Should()
            .ContainSingle(e =>
                e is InternalError && e.Message.Contains("was null or could not be deserialized")
            );

        // Verify no logs were written for deserialization failure (not an HTTP error)
        _fakeLogger.VerifyNoLogsWritten();
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    public async Task GetFromJsonSafeAsync_UnsuccessfulResponse_ReturnsFailedResultAndLogsError(
        HttpStatusCode statusCode
    )
    {
        // Arrange
        var errorMessage = "Something went wrong";
        _mockHttpMessageHandler
            .SetupRequest(HttpMethod.Get, TestRequestUri)
            .ReturnsJsonResponse(statusCode, "Something went wrong");

        // Act
        var result = await _httpClient.GetFromJsonSafeAsync<TestData.TestDto>(
            RelativeTestUri,
            _fakeLogger,
            FailureMessage
        );

        // Assert
        result.Should().NotBeNull();
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains(FailureMessage));

        // Verify that LogUnsuccessfulHttpResponse was called
        _fakeLogger.VerifyWasCalled(LogLevel.Warning, errorMessage);
    }

    private static class TestData
    {
        public sealed class TestDto
        {
            public int Id { get; set; }

            public string? Name { get; set; }
        }

        public struct TestStructDto { }
    }
}
