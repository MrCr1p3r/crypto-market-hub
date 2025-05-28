using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Moq.Contrib.HttpClient;
using SharedLibrary.Extensions.HttpClient.Internal;
using SharedLibrary.Extensions.Testing;
using static SharedLibrary.Errors.GenericErrors;

namespace SharedLibrary.Tests.Unit.Extensions.HttpClient.Internal;

public class InternalHttpClientExtensionsTests
{
    private const string TestRequestUri = "http://localhost/api/test";
    private const string RelativeTestUri = "test";
    private const string FailureMessage = "Failed to retrieve data.";
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly System.Net.Http.HttpClient _httpClient;
    private readonly FakeLogger _fakeLogger;

    public InternalHttpClientExtensionsTests()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = _mockHttpMessageHandler.CreateClient();
        _httpClient.BaseAddress = new Uri("http://localhost/api/");
        _fakeLogger = new FakeLogger();
    }

    #region GetFromJsonSafeAsync Tests

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
        result
            .Errors.Should()
            .ContainSingle(error =>
                error.Message.Contains("was null or could not be deserialized")
            );

        // Verify no logs were written for deserialization failure (not an HTTP error)
        _fakeLogger.VerifyNoLogsWritten();
    }

    [Fact]
    public async Task GetFromJsonSafeAsync_EmptyJsonObject_ReturnsOkResultWithDefaultProperties()
    {
        // Arrange
        _mockHttpMessageHandler
            .SetupRequest(HttpMethod.Get, TestRequestUri)
            .ReturnsJsonResponse(HttpStatusCode.OK, new { });

        // Act
        var result = await _httpClient.GetFromJsonSafeAsync<TestData.TestDto>(
            RelativeTestUri,
            _fakeLogger,
            FailureMessage
        );

        // Assert
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
        // Arrange
        _mockHttpMessageHandler
            .SetupRequest(HttpMethod.Get, TestRequestUri)
            .ReturnsJsonResponse(HttpStatusCode.OK, new { });

        // Act
        var result = await _httpClient.GetFromJsonSafeAsync<TestData.TestStructDto>(
            RelativeTestUri,
            _fakeLogger,
            FailureMessage
        );

        // Assert
        result.Should().NotBeNull();
        result
            .IsFailed.Should()
            .BeTrue("because the deserialized struct is equal to default(TestStructDto)");
        result
            .Errors.Should()
            .ContainSingle(error =>
                error.Message.Contains("was null or could not be deserialized")
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
        var problemDetails = CreateBasicProblemDetails("Something went wrong");
        _mockHttpMessageHandler
            .SetupRequest(HttpMethod.Get, TestRequestUri)
            .ReturnsJsonResponse(statusCode, problemDetails);

        // Act
        var result = await _httpClient.GetFromJsonSafeAsync<TestData.TestDto>(
            RelativeTestUri,
            _fakeLogger,
            FailureMessage
        );

        // Assert
        result.Should().NotBeNull();
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(error => error.Message.Contains(FailureMessage));

        // Verify that LogUnsuccessfulHttpResponse was called
        _fakeLogger.VerifyWasCalled(LogLevel.Warning, "Something went wrong");
    }

    #endregion

    #region PostAsJsonSafeAsync Tests

    [Fact]
    public async Task PostAsJsonSafeAsync_ReturnsOkResultWithContent()
    {
        // Arrange
        var requestDto = new TestData.TestRequestDto { Name = "Test Request" };
        var expectedResponse = new TestData.TestDto { Id = 1, Name = "Test Response" };
        _mockHttpMessageHandler
            .SetupRequest(HttpMethod.Post, TestRequestUri)
            .ReturnsJsonResponse(HttpStatusCode.OK, expectedResponse);

        // Act
        var result = await _httpClient.PostAsJsonSafeAsync<
            TestData.TestRequestDto,
            TestData.TestDto
        >(RelativeTestUri, requestDto, _fakeLogger, FailureMessage);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expectedResponse);

        // Verify no logs were written for successful case
        _fakeLogger.VerifyNoLogsWritten();
    }

    [Fact]
    public async Task PostAsJsonSafeAsync_ContentDeserializesToNull_ReturnsFailedDeserializationResult()
    {
        // Arrange
        var requestDto = new TestData.TestRequestDto { Name = "Test Request" };
        _mockHttpMessageHandler
            .SetupRequest(HttpMethod.Post, TestRequestUri)
            .ReturnsJsonResponse(HttpStatusCode.OK, (object?)null);

        // Act
        var result = await _httpClient.PostAsJsonSafeAsync<
            TestData.TestRequestDto,
            TestData.TestDto
        >(RelativeTestUri, requestDto, _fakeLogger, FailureMessage);

        // Assert
        result.Should().NotBeNull();
        result.IsFailed.Should().BeTrue();
        result
            .Errors.Should()
            .ContainSingle(error =>
                error.Message.Contains("was null or could not be deserialized")
            );

        // Verify no logs were written for deserialization failure (not an HTTP error)
        _fakeLogger.VerifyNoLogsWritten();
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task PostAsJsonSafeAsync_UnsuccessfulResponse_ReturnsFailedResultAndLogsError(
        HttpStatusCode statusCode
    )
    {
        // Arrange
        var requestDto = new TestData.TestRequestDto { Name = "Test Request" };
        var problemDetails = CreateBasicProblemDetails("Post operation failed");
        _mockHttpMessageHandler
            .SetupRequest(HttpMethod.Post, TestRequestUri)
            .ReturnsJsonResponse(statusCode, problemDetails);

        // Act
        var result = await _httpClient.PostAsJsonSafeAsync<
            TestData.TestRequestDto,
            TestData.TestDto
        >(RelativeTestUri, requestDto, _fakeLogger, FailureMessage);

        // Assert
        result.Should().NotBeNull();
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(error => error.Message.Contains(FailureMessage));

        // Verify that LogUnsuccessfulHttpResponse was called
        _fakeLogger.VerifyWasCalled(LogLevel.Warning, "Post operation failed");
    }

    [Fact]
    public async Task PostAsJsonSafeAsync_WithComplexProblemDetails_ReturnsStructuredError()
    {
        // Arrange
        var requestDto = new TestData.TestRequestDto { Name = "Test Request" };
        var problemDetails = CreateProblemDetailsWithMetadata(
            "Validation failed",
            new Dictionary<string, object>
            {
                ["field"] = "Name",
                ["code"] = "REQUIRED",
                ["correlationId"] = "abc-123",
            }
        );
        _mockHttpMessageHandler
            .SetupRequest(HttpMethod.Post, TestRequestUri)
            .ReturnsJsonResponse(HttpStatusCode.BadRequest, problemDetails);

        // Act
        var result = await _httpClient.PostAsJsonSafeAsync<
            TestData.TestRequestDto,
            TestData.TestDto
        >(RelativeTestUri, requestDto, _fakeLogger, FailureMessage);

        // Assert
        result.Should().NotBeNull();
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(error => error is BadRequestError);

        var mainError = result.Errors[0];
        mainError.Message.Should().Be(FailureMessage);
        mainError.Reasons.Should().ContainSingle();

        var reasonError = mainError.Reasons[0];
        reasonError.Message.Should().Be("Validation failed");
        reasonError.Metadata.Should().ContainKey("field").WhoseValue.Should().Be("Name");
        reasonError.Metadata.Should().ContainKey("code").WhoseValue.Should().Be("REQUIRED");
        reasonError.Metadata.Should().ContainKey("correlationId").WhoseValue.Should().Be("abc-123");
    }

    #endregion

    #region PatchAsJsonSafeAsync Tests

    [Fact]
    public async Task PatchAsJsonSafeAsync_ReturnsOkResultWithContent()
    {
        // Arrange
        var requestDto = new TestData.TestRequestDto { Name = "Updated Name" };
        var expectedResponse = new TestData.TestDto { Id = 1, Name = "Updated Name" };
        _mockHttpMessageHandler
            .SetupRequest(HttpMethod.Patch, TestRequestUri)
            .ReturnsJsonResponse(HttpStatusCode.OK, expectedResponse);

        // Act
        var result = await _httpClient.PatchAsJsonSafeAsync<
            TestData.TestRequestDto,
            TestData.TestDto
        >(RelativeTestUri, requestDto, _fakeLogger, FailureMessage);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expectedResponse);

        // Verify no logs were written for successful case
        _fakeLogger.VerifyNoLogsWritten();
    }

    [Fact]
    public async Task PatchAsJsonSafeAsync_ContentDeserializesToNull_ReturnsFailedDeserializationResult()
    {
        // Arrange
        var requestDto = new TestData.TestRequestDto { Name = "Updated Name" };
        _mockHttpMessageHandler
            .SetupRequest(HttpMethod.Patch, TestRequestUri)
            .ReturnsJsonResponse(HttpStatusCode.OK, (object?)null);

        // Act
        var result = await _httpClient.PatchAsJsonSafeAsync<
            TestData.TestRequestDto,
            TestData.TestDto
        >(RelativeTestUri, requestDto, _fakeLogger, FailureMessage);

        // Assert
        result.Should().NotBeNull();
        result.IsFailed.Should().BeTrue();
        result
            .Errors.Should()
            .ContainSingle(error =>
                error.Message.Contains("was null or could not be deserialized")
            );

        // Verify no logs were written for deserialization failure (not an HTTP error)
        _fakeLogger.VerifyNoLogsWritten();
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task PatchAsJsonSafeAsync_UnsuccessfulResponse_ReturnsFailedResultAndLogsError(
        HttpStatusCode statusCode
    )
    {
        // Arrange
        var requestDto = new TestData.TestRequestDto { Name = "Updated Name" };
        var problemDetails = CreateBasicProblemDetails("Patch operation failed");
        _mockHttpMessageHandler
            .SetupRequest(HttpMethod.Patch, TestRequestUri)
            .ReturnsJsonResponse(statusCode, problemDetails);

        // Act
        var result = await _httpClient.PatchAsJsonSafeAsync<
            TestData.TestRequestDto,
            TestData.TestDto
        >(RelativeTestUri, requestDto, _fakeLogger, FailureMessage);

        // Assert
        result.Should().NotBeNull();
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(error => error.Message.Contains(FailureMessage));

        // Verify that LogUnsuccessfulHttpResponse was called
        _fakeLogger.VerifyWasCalled(LogLevel.Warning, "Patch operation failed");
    }

    [Fact]
    public async Task PatchAsJsonSafeAsync_WithNestedReasons_ReturnsHierarchicalError()
    {
        // Arrange
        var requestDto = new TestData.TestRequestDto { Name = "Updated Name" };
        var nestedReasons = new[]
        {
            new Dictionary<string, object>
            {
                ["message"] = "Database constraint violation",
                ["metadata"] = new Dictionary<string, object>
                {
                    ["constraint"] = "unique_name",
                    ["table"] = "users",
                },
            },
            new Dictionary<string, object>
            {
                ["message"] = "Validation error",
                ["metadata"] = new Dictionary<string, object>
                {
                    ["field"] = "name",
                    ["rule"] = "max_length",
                },
            },
        };

        var problemDetails = CreateProblemDetailsWithReasons(
            "Multiple validation failures",
            nestedReasons
        );
        _mockHttpMessageHandler
            .SetupRequest(HttpMethod.Patch, TestRequestUri)
            .ReturnsJsonResponse(HttpStatusCode.Conflict, problemDetails);

        // Act
        var result = await _httpClient.PatchAsJsonSafeAsync<
            TestData.TestRequestDto,
            TestData.TestDto
        >(RelativeTestUri, requestDto, _fakeLogger, FailureMessage);

        // Assert
        result.Should().NotBeNull();
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(error => error is ConflictError);

        var mainError = result.Errors[0];
        mainError.Message.Should().Be(FailureMessage);

        var reasonError = mainError.Reasons[0];
        reasonError.Message.Should().Be("Multiple validation failures");
        reasonError.Reasons.Should().HaveCount(2);

        var dbError = reasonError.Reasons[0];
        dbError.Message.Should().Be("Database constraint violation");
        dbError.Metadata.Should().ContainKey("constraint").WhoseValue.Should().Be("unique_name");
        dbError.Metadata.Should().ContainKey("table").WhoseValue.Should().Be("users");

        var validationError = reasonError.Reasons[1];
        validationError.Message.Should().Be("Validation error");
        validationError.Metadata.Should().ContainKey("field").WhoseValue.Should().Be("name");
        validationError.Metadata.Should().ContainKey("rule").WhoseValue.Should().Be("max_length");
    }

    #endregion

    #region PutAsJsonSafeAsync Tests

    [Fact]
    public async Task PutAsJsonSafeAsync_ReturnsOkResultWithContent()
    {
        // Arrange
        var requestDto = new TestData.TestRequestDto { Name = "Updated Name" };
        var expectedResponse = new TestData.TestDto { Id = 1, Name = "Updated Name" };
        _mockHttpMessageHandler
            .SetupRequest(HttpMethod.Put, TestRequestUri)
            .ReturnsJsonResponse(HttpStatusCode.OK, expectedResponse);

        // Act
        var result = await _httpClient.PutAsJsonSafeAsync<
            TestData.TestRequestDto,
            TestData.TestDto
        >(RelativeTestUri, requestDto, _fakeLogger, FailureMessage);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expectedResponse);

        // Verify no logs were written for successful case
        _fakeLogger.VerifyNoLogsWritten();
    }

    [Fact]
    public async Task PutAsJsonSafeAsync_ContentDeserializesToNull_ReturnsFailedDeserializationResult()
    {
        // Arrange
        var requestDto = new TestData.TestRequestDto { Name = "Updated Name" };
        _mockHttpMessageHandler
            .SetupRequest(HttpMethod.Put, TestRequestUri)
            .ReturnsJsonResponse(HttpStatusCode.OK, (object?)null);

        // Act
        var result = await _httpClient.PutAsJsonSafeAsync<
            TestData.TestRequestDto,
            TestData.TestDto
        >(RelativeTestUri, requestDto, _fakeLogger, FailureMessage);

        // Assert
        result.Should().NotBeNull();
        result.IsFailed.Should().BeTrue();
        result
            .Errors.Should()
            .ContainSingle(error =>
                error.Message.Contains("was null or could not be deserialized")
            );

        // Verify no logs were written for deserialization failure (not an HTTP error)
        _fakeLogger.VerifyNoLogsWritten();
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task PutAsJsonSafeAsync_UnsuccessfulResponse_ReturnsFailedResultAndLogsError(
        HttpStatusCode statusCode
    )
    {
        // Arrange
        var requestDto = new TestData.TestRequestDto { Name = "Updated Name" };
        var problemDetails = CreateBasicProblemDetails("Put operation failed");
        _mockHttpMessageHandler
            .SetupRequest(HttpMethod.Put, TestRequestUri)
            .ReturnsJsonResponse(statusCode, problemDetails);

        // Act
        var result = await _httpClient.PutAsJsonSafeAsync<
            TestData.TestRequestDto,
            TestData.TestDto
        >(RelativeTestUri, requestDto, _fakeLogger, FailureMessage);

        // Assert
        result.Should().NotBeNull();
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(error => error.Message.Contains(FailureMessage));

        // Verify that LogUnsuccessfulHttpResponse was called
        _fakeLogger.VerifyWasCalled(LogLevel.Warning, "Put operation failed");
    }

    [Fact]
    public async Task PutAsJsonSafeAsync_WithValidationErrorMetadata_ReturnsStructuredError()
    {
        // Arrange
        var requestDto = new TestData.TestRequestDto { Name = "Invalid Name" };
        var problemDetails = CreateProblemDetailsWithMetadata(
            "Resource update validation failed",
            new Dictionary<string, object>
            {
                ["field"] = "Name",
                ["code"] = "INVALID_FORMAT",
                ["expectedFormat"] = "alphanumeric",
                ["correlationId"] = "put-123",
            }
        );
        _mockHttpMessageHandler
            .SetupRequest(HttpMethod.Put, TestRequestUri)
            .ReturnsJsonResponse(HttpStatusCode.BadRequest, problemDetails);

        // Act
        var result = await _httpClient.PutAsJsonSafeAsync<
            TestData.TestRequestDto,
            TestData.TestDto
        >(RelativeTestUri, requestDto, _fakeLogger, FailureMessage);

        // Assert
        result.Should().NotBeNull();
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(error => error is BadRequestError);

        var mainError = result.Errors[0];
        mainError.Message.Should().Be(FailureMessage);
        mainError.Reasons.Should().ContainSingle();

        var reasonError = mainError.Reasons[0];
        reasonError.Message.Should().Be("Resource update validation failed");
        reasonError.Metadata.Should().ContainKey("field").WhoseValue.Should().Be("Name");
        reasonError.Metadata.Should().ContainKey("code").WhoseValue.Should().Be("INVALID_FORMAT");
        reasonError
            .Metadata.Should()
            .ContainKey("expectedFormat")
            .WhoseValue.Should()
            .Be("alphanumeric");
        reasonError.Metadata.Should().ContainKey("correlationId").WhoseValue.Should().Be("put-123");
    }

    [Fact]
    public async Task PutAsJsonSafeAsync_WithConflictAndMultipleReasons_ReturnsHierarchicalError()
    {
        // Arrange
        var requestDto = new TestData.TestRequestDto { Name = "Conflicting Name" };
        var nestedReasons = new[]
        {
            new Dictionary<string, object>
            {
                ["message"] = "Resource version mismatch",
                ["metadata"] = new Dictionary<string, object>
                {
                    ["currentVersion"] = "v2.1",
                    ["providedVersion"] = "v1.0",
                    ["resourceId"] = "resource-456",
                },
            },
            new Dictionary<string, object>
            {
                ["message"] = "Concurrent modification detected",
                ["metadata"] = new Dictionary<string, object>
                {
                    ["lastModified"] = "2024-01-15T10:30:00Z",
                    ["modifiedBy"] = "user-789",
                },
            },
        };

        var problemDetails = CreateProblemDetailsWithReasons(
            "Resource update conflict",
            nestedReasons
        );
        _mockHttpMessageHandler
            .SetupRequest(HttpMethod.Put, TestRequestUri)
            .ReturnsJsonResponse(HttpStatusCode.Conflict, problemDetails);

        // Act
        var result = await _httpClient.PutAsJsonSafeAsync<
            TestData.TestRequestDto,
            TestData.TestDto
        >(RelativeTestUri, requestDto, _fakeLogger, FailureMessage);

        // Assert
        result.Should().NotBeNull();
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(error => error is ConflictError);

        var mainError = result.Errors[0];
        mainError.Message.Should().Be(FailureMessage);

        var reasonError = mainError.Reasons[0];
        reasonError.Message.Should().Be("Resource update conflict");
        reasonError.Reasons.Should().HaveCount(2);

        var versionError = reasonError.Reasons[0];
        versionError.Message.Should().Be("Resource version mismatch");
        versionError.Metadata.Should().ContainKey("currentVersion").WhoseValue.Should().Be("v2.1");
        versionError.Metadata.Should().ContainKey("providedVersion").WhoseValue.Should().Be("v1.0");
        versionError
            .Metadata.Should()
            .ContainKey("resourceId")
            .WhoseValue.Should()
            .Be("resource-456");

        var concurrencyError = reasonError.Reasons[1];
        concurrencyError.Message.Should().Be("Concurrent modification detected");
        concurrencyError
            .Metadata.Should()
            .ContainKey("lastModified")
            .WhoseValue.Should()
            .Be("2024-01-15T10:30:00Z");
        concurrencyError
            .Metadata.Should()
            .ContainKey("modifiedBy")
            .WhoseValue.Should()
            .Be("user-789");
    }

    [Fact]
    public async Task PutAsJsonSafeAsync_WithNotFoundError_ReturnsNotFoundError()
    {
        // Arrange
        var requestDto = new TestData.TestRequestDto { Name = "Nonexistent Resource" };
        var problemDetails = CreateBasicProblemDetails("Resource not found for update");
        _mockHttpMessageHandler
            .SetupRequest(HttpMethod.Put, TestRequestUri)
            .ReturnsJsonResponse(HttpStatusCode.NotFound, problemDetails);

        // Act
        var result = await _httpClient.PutAsJsonSafeAsync<
            TestData.TestRequestDto,
            TestData.TestDto
        >(RelativeTestUri, requestDto, _fakeLogger, FailureMessage);

        // Assert
        result.Should().NotBeNull();
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(error => error is NotFoundError);

        var mainError = result.Errors[0];
        mainError.Message.Should().Be(FailureMessage);
        mainError.Reasons.Should().ContainSingle();
        mainError.Reasons[0].Message.Should().Be("Resource not found for update");

        // Verify that LogUnsuccessfulHttpResponse was called
        _fakeLogger.VerifyWasCalled(LogLevel.Warning, "Resource not found for update");
    }

    #endregion

    #region Helper Methods

    private static ProblemDetails CreateBasicProblemDetails(string detail)
    {
        return new ProblemDetails
        {
            Type = "https://example.com/problem",
            Title = "Test Error",
            Status = 500,
            Detail = detail,
            Instance = "/test",
        };
    }

    private static ProblemDetails CreateProblemDetailsWithMetadata(
        string detail,
        Dictionary<string, object> metadata
    )
    {
        var problemDetails = new ProblemDetails
        {
            Type = "https://example.com/problem",
            Title = "Test Error",
            Status = 400,
            Detail = detail,
            Instance = "/test",
        };

        problemDetails.Extensions["metadata"] = metadata;
        return problemDetails;
    }

    private static ProblemDetails CreateProblemDetailsWithReasons(string detail, object[] reasons)
    {
        var problemDetails = new ProblemDetails
        {
            Type = "https://example.com/problem",
            Title = "Test Error",
            Status = 500,
            Detail = detail,
            Instance = "/test",
        };

        problemDetails.Extensions["reasons"] = reasons;
        return problemDetails;
    }

    #endregion

    #region Test Data Classes

    private static class TestData
    {
        public sealed class TestDto
        {
            public int Id { get; set; }

            public string? Name { get; set; }
        }

        public sealed class TestRequestDto
        {
            public string? Name { get; set; }
        }

        public struct TestStructDto { }
    }

    #endregion
}
