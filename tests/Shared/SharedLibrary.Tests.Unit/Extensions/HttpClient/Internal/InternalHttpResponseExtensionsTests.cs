using System.Net;
using System.Text;
using System.Text.Json;
using SharedLibrary.Exceptions;
using SharedLibrary.Extensions.HttpClient.Internal;
using static SharedLibrary.Errors.GenericErrors;

namespace SharedLibrary.Tests.Unit.Extensions.HttpClient.Internal;

public class InternalHttpResponseExtensionsTests
{
    private const string TestUri = "http://localhost/api/test";
    private const string FailureMessage = "Operation failed";

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, typeof(BadRequestError))]
    [InlineData(HttpStatusCode.Unauthorized, typeof(UnauthorizedError))]
    [InlineData(HttpStatusCode.Forbidden, typeof(ForbiddenError))]
    [InlineData(HttpStatusCode.NotFound, typeof(NotFoundError))]
    [InlineData(HttpStatusCode.Conflict, typeof(ConflictError))]
    [InlineData(HttpStatusCode.TooManyRequests, typeof(TooManyRequestsError))]
    [InlineData(HttpStatusCode.InternalServerError, typeof(InternalError))]
    [InlineData(HttpStatusCode.BadGateway, typeof(GatewayError))]
    [InlineData(HttpStatusCode.ServiceUnavailable, typeof(UnavailableError))]
    [InlineData(HttpStatusCode.GatewayTimeout, typeof(TimeoutError))]
    public async Task ToFailedResultAsync_ReturnsCorrectErrorTypeForStatusCode(
        HttpStatusCode statusCode,
        Type expectedErrorType
    )
    {
        // Arrange
        var problemDetails = CreateBasicProblemDetails("Test error occurred");
        using var response = CreateHttpResponseMessage(statusCode, problemDetails);

        // Act
        var result = await response.ToFailedResultAsync<string>(FailureMessage);

        // Assert
        result.Should().NotBeNull();
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Should().BeOfType(expectedErrorType);
        result.Errors[0].Message.Should().Be(FailureMessage);

        // Verify the reason error
        result.Errors[0].Reasons.Should().ContainSingle();
        result.Errors[0].Reasons[0].Should().BeOfType(expectedErrorType);
        result.Errors[0].Reasons[0].Message.Should().Be("Test error occurred");
    }

    [Fact]
    public async Task ToFailedResultAsync_UnknownStatusCode_ReturnsInternalError()
    {
        // Arrange
        var problemDetails = CreateBasicProblemDetails("Unknown error");
        using var response = CreateHttpResponseMessage((HttpStatusCode)999, problemDetails);

        // Act
        var result = await response.ToFailedResultAsync<string>(FailureMessage);

        // Assert
        result.Should().NotBeNull();
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(error => error is InternalError);
        result.Errors[0].Message.Should().Be(FailureMessage);
        result.Errors[0].Reasons.Should().ContainSingle(reason => reason is InternalError);
    }

    [Fact]
    public async Task ToFailedResultAsync_IncludesHttpMetadataInReason()
    {
        // Arrange
        var problemDetails = CreateBasicProblemDetails("Detailed error message");
        using var response = CreateHttpResponseMessage(
            HttpStatusCode.BadRequest,
            problemDetails,
            "Bad Request"
        );

        // Act
        var result = await response.ToFailedResultAsync<string>(FailureMessage);

        // Assert
        result.Should().NotBeNull();
        result.IsFailed.Should().BeTrue();

        var mainError = result.Errors[0];
        mainError.Should().BeOfType<BadRequestError>();
        mainError.Message.Should().Be(FailureMessage);

        var reasonError = mainError.Reasons[0];
        reasonError.Metadata.Should().ContainKey("method").WhoseValue.Should().Be("GET");
        reasonError.Metadata.Should().ContainKey("uri").WhoseValue.Should().Be(TestUri);
        reasonError.Metadata.Should().ContainKey("status").WhoseValue.Should().Be(400);
        reasonError.Metadata.Should().ContainKey("reason").WhoseValue.Should().Be("Bad Request");
    }

    [Fact]
    public async Task ToFailedResultAsync_WithProblemDetailsMetadata_IncludesMetadataInReason()
    {
        // Arrange
        var problemDetails = CreateProblemDetailsWithMetadata(
            "Service unavailable",
            new Dictionary<string, object>
            {
                ["serviceId"] = "payment-service",
                ["retryAfter"] = 30,
                ["correlationId"] = "abc-123",
            }
        );
        using var response = CreateHttpResponseMessage(
            HttpStatusCode.ServiceUnavailable,
            problemDetails
        );

        // Act
        var result = await response.ToFailedResultAsync<string>(FailureMessage);

        // Assert
        result.Should().NotBeNull();
        result.IsFailed.Should().BeTrue();

        var reasonError = result.Errors[0].Reasons[0];

        // The current implementation should extract metadata from ProblemDetails.Extensions["metadata"]
        // and merge it with HTTP metadata. Let's check what we actually get:
        reasonError.Metadata.Should().NotBeNull();

        // HTTP metadata should always be present
        reasonError.Metadata.Should().ContainKey("method").WhoseValue.Should().Be("GET");
        reasonError.Metadata.Should().ContainKey("uri").WhoseValue.Should().Be(TestUri);
        reasonError.Metadata.Should().ContainKey("status").WhoseValue.Should().Be(503);
        reasonError
            .Metadata.Should()
            .ContainKey("reason")
            .WhoseValue.Should()
            .Be("Service Unavailable");

        // ProblemDetails metadata should be merged in
        reasonError
            .Metadata.Should()
            .ContainKey("serviceId")
            .WhoseValue.Should()
            .Be("payment-service");
        reasonError.Metadata.Should().ContainKey("retryAfter").WhoseValue.Should().Be(30);
        reasonError.Metadata.Should().ContainKey("correlationId").WhoseValue.Should().Be("abc-123");
    }

    [Fact]
    public async Task ToFailedResultAsync_WithNestedReasons_CreatesHierarchicalErrors()
    {
        // Arrange
        var nestedReasons = new[]
        {
            new Dictionary<string, object>
            {
                ["message"] = "Database connection failed",
                ["metadata"] = new Dictionary<string, object>
                {
                    ["connectionString"] = "Server=localhost;Database=test",
                    ["timeout"] = 30,
                },
            },
            new Dictionary<string, object>
            {
                ["message"] = "Cache service unavailable",
                ["metadata"] = new Dictionary<string, object>
                {
                    ["cacheType"] = "Redis",
                    ["endpoint"] = "localhost:6379",
                },
            },
        };

        var problemDetails = CreateProblemDetailsWithReasons(
            "Multiple service failures",
            nestedReasons
        );
        using var response = CreateHttpResponseMessage(
            HttpStatusCode.InternalServerError,
            problemDetails
        );

        // Act
        var result = await response.ToFailedResultAsync<string>(FailureMessage);

        // Assert
        result.Should().NotBeNull();
        result.IsFailed.Should().BeTrue();

        var mainError = result.Errors[0];
        mainError.Should().BeOfType<InternalError>();
        mainError.Message.Should().Be(FailureMessage);

        var reasonError = mainError.Reasons[0];
        reasonError.Message.Should().Be("Multiple service failures");
        reasonError.Reasons.Should().HaveCount(2);

        var dbError = reasonError.Reasons[0];
        dbError.Message.Should().Be("Database connection failed");
        dbError.Metadata.Should().ContainKey("connectionString");
        dbError.Metadata.Should().ContainKey("timeout").WhoseValue.Should().Be(30);

        var cacheError = reasonError.Reasons[1];
        cacheError.Message.Should().Be("Cache service unavailable");
        cacheError.Metadata.Should().ContainKey("cacheType").WhoseValue.Should().Be("Redis");
        cacheError
            .Metadata.Should()
            .ContainKey("endpoint")
            .WhoseValue.Should()
            .Be("localhost:6379");
    }

    [Fact]
    public async Task ToFailedResultAsync_WithDeeplyNestedReasons_HandlesRecursion()
    {
        // Arrange
        var deeplyNestedReasons = new[]
        {
            new Dictionary<string, object>
            {
                ["message"] = "Level 1 error",
                ["reasons"] = new[]
                {
                    new Dictionary<string, object>
                    {
                        ["message"] = "Level 2 error",
                        ["reasons"] = new[]
                        {
                            new Dictionary<string, object>
                            {
                                ["message"] = "Level 3 error",
                                ["metadata"] = new Dictionary<string, object> { ["level"] = 3 },
                            },
                        },
                    },
                },
            },
        };

        var problemDetails = CreateProblemDetailsWithReasons(
            "Nested error structure",
            deeplyNestedReasons
        );
        using var response = CreateHttpResponseMessage(
            HttpStatusCode.InternalServerError,
            problemDetails
        );

        // Act
        var result = await response.ToFailedResultAsync<string>(FailureMessage);

        // Assert
        result.Should().NotBeNull();
        result.IsFailed.Should().BeTrue();

        var mainError = result.Errors[0];
        var reasonError = mainError.Reasons[0];

        // Navigate through the nested structure
        var level1Error = reasonError.Reasons[0];
        level1Error.Message.Should().Be("Level 1 error");

        var level2Error = level1Error.Reasons[0];
        level2Error.Message.Should().Be("Level 2 error");

        var level3Error = level2Error.Reasons[0];
        level3Error.Message.Should().Be("Level 3 error");
        level3Error.Metadata.Should().ContainKey("level").WhoseValue.Should().Be(3);
    }

    [Fact]
    public async Task ToFailedResultAsync_MissingRequestMessage_HandlesGracefully()
    {
        // Arrange
        var problemDetails = CreateBasicProblemDetails("Error occurred");
        using var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            ReasonPhrase = "Bad Request",
            Content = new StringContent(
                JsonSerializer.Serialize(problemDetails),
                Encoding.UTF8,
                "application/json"
            ),
            RequestMessage = null, // Explicitly set to null
        };

        // Act
        var result = await response.ToFailedResultAsync<string>(FailureMessage);

        // Assert
        result.Should().NotBeNull();
        result.IsFailed.Should().BeTrue();

        var reasonError = result.Errors[0].Reasons[0];
        reasonError.Metadata.Should().ContainKey("method").WhoseValue.Should().Be("UNKNOWN");
        reasonError.Metadata.Should().ContainKey("uri").WhoseValue.Should().Be("UNKNOWN");
        reasonError.Metadata.Should().ContainKey("status").WhoseValue.Should().Be(400);
        reasonError.Metadata.Should().ContainKey("reason").WhoseValue.Should().Be("Bad Request");
    }

    [Theory]
    [InlineData("Simple error message")]
    [InlineData("Error with special characters: !@#$%^&*()")]
    [InlineData(
        "Very long error message that contains multiple sentences and should be handled properly by the extension method without any issues or truncation."
    )]
    public async Task ToFailedResultAsync_VariousFailureMessages_PreservesMessage(string message)
    {
        // Arrange
        var problemDetails = CreateBasicProblemDetails("Internal error");
        using var response = CreateHttpResponseMessage(
            HttpStatusCode.InternalServerError,
            problemDetails
        );

        // Act
        var result = await response.ToFailedResultAsync<string>(message);

        // Assert
        result.Should().NotBeNull();
        result.IsFailed.Should().BeTrue();
        result.Errors[0].Message.Should().Be(message);
    }

    [Fact]
    public async Task ToFailedResultAsync_InvalidJsonContent_ThrowsProblemDetailsException()
    {
        // Arrange
        using var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            ReasonPhrase = "Bad Request",
            Content = new StringContent("Invalid JSON content", Encoding.UTF8, "application/json"),
            RequestMessage = new HttpRequestMessage(HttpMethod.Get, TestUri),
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ProblemDetailsException>(
            () => response.ToFailedResultAsync<string>(FailureMessage)
        );

        exception.Message.Should().Contain("was not in correct ProblemDetails format");
        exception.Message.Should().Contain("GET");
        exception.Message.Should().Contain(TestUri);
    }

    private static object CreateBasicProblemDetails(string detail)
    {
        return new
        {
            type = "https://example.com/problem",
            title = "Test Error",
            status = 500,
            detail,
            instance = "/test",
        };
    }

    private static Dictionary<string, object> CreateProblemDetailsWithMetadata(
        string detail,
        Dictionary<string, object> metadata
    )
    {
        // Create a ProblemDetails-compatible structure where additional properties
        // are added directly to the root object (not in an "extensions" property)
        var problemDetails = new Dictionary<string, object>
        {
            ["type"] = "https://example.com/problem",
            ["title"] = "Test Error",
            ["status"] = 503,
            ["detail"] = detail,
            ["instance"] = "/test",
            ["metadata"] = metadata,
        };

        return problemDetails;
    }

    private static Dictionary<string, object> CreateProblemDetailsWithReasons(
        string detail,
        object[] reasons
    )
    {
        // Create a ProblemDetails-compatible structure where additional properties
        // are added directly to the root object (not in an "extensions" property)
        var problemDetails = new Dictionary<string, object>
        {
            ["type"] = "https://example.com/problem",
            ["title"] = "Test Error",
            ["status"] = 500,
            ["detail"] = detail,
            ["instance"] = "/test",
            ["reasons"] = reasons,
        };

        return problemDetails;
    }

    private static HttpResponseMessage CreateHttpResponseMessage(
        HttpStatusCode statusCode,
        object problemDetails,
        string? reasonPhrase = null
    )
    {
        var json = JsonSerializer.Serialize(problemDetails);
        var request = new HttpRequestMessage(HttpMethod.Get, TestUri);

        return new HttpResponseMessage(statusCode)
        {
            ReasonPhrase = reasonPhrase,
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
            RequestMessage = request,
        };
    }
}
