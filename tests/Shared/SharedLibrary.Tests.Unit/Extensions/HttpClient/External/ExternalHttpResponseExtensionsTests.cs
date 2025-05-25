using System.Net;
using System.Text;
using SharedLibrary.Extensions.HttpClient.External;
using static SharedLibrary.Errors.GenericErrors;

namespace SharedLibrary.Tests.Unit.Extensions.HttpClient.External;

public class ExternalHttpResponseExtensionsTests
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
        using var response = CreateHttpResponseMessage(statusCode, "Error response content");

        // Act
        var result = await response.ToFailedResultAsync<string>(FailureMessage);

        // Assert
        result.Should().NotBeNull();
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Should().BeOfType(expectedErrorType);
        result.Errors[0].Message.Should().Be(FailureMessage);
    }

    [Fact]
    public async Task ToFailedResultAsync_UnknownStatusCode_ReturnsInternalError()
    {
        // Arrange
        using var response = CreateHttpResponseMessage((HttpStatusCode)999, "Unknown error");

        // Act
        var result = await response.ToFailedResultAsync<string>(FailureMessage);

        // Assert
        result.Should().NotBeNull();
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(error => error is InternalError);
        result.Errors[0].Message.Should().Be(FailureMessage);
    }

    [Fact]
    public async Task ToFailedResultAsync_IncludesHttpMetadata()
    {
        // Arrange
        const string responseContent = "Detailed error message";
        using var response = CreateHttpResponseMessage(
            HttpStatusCode.BadRequest,
            responseContent,
            "Bad Request"
        );

        // Act
        var result = await response.ToFailedResultAsync<string>(FailureMessage);

        // Assert
        result.Should().NotBeNull();
        result.IsFailed.Should().BeTrue();

        var error = result.Errors[0];
        error.Metadata.Should().ContainKey("method").WhoseValue.Should().Be("GET");
        error.Metadata.Should().ContainKey("uri").WhoseValue.Should().Be(TestUri);
        error.Metadata.Should().ContainKey("status").WhoseValue.Should().Be(400);
        error.Metadata.Should().ContainKey("reason").WhoseValue.Should().Be("Bad Request");
        error.Metadata.Should().ContainKey("body").WhoseValue.Should().Be(responseContent);
    }

    [Fact]
    public async Task ToFailedResultAsync_EmptyResponseContent_DoesNotIncludeBodyMetadata()
    {
        // Arrange
        using var response = CreateHttpResponseMessage(HttpStatusCode.NotFound, string.Empty);

        // Act
        var result = await response.ToFailedResultAsync<string>(FailureMessage);

        // Assert
        result.Should().NotBeNull();
        result.IsFailed.Should().BeTrue();

        var error = result.Errors[0];
        error.Metadata.Should().NotContainKey("body");
        error.Metadata.Should().ContainKey("method");
        error.Metadata.Should().ContainKey("uri");
        error.Metadata.Should().ContainKey("status");
        error.Metadata.Should().ContainKey("reason");
    }

    [Fact]
    public async Task ToFailedResultAsync_NullResponseContent_DoesNotIncludeBodyMetadata()
    {
        // Arrange
        using var response = CreateHttpResponseMessage(HttpStatusCode.InternalServerError);

        // Act
        var result = await response.ToFailedResultAsync<string>(FailureMessage);

        // Assert
        result.Should().NotBeNull();
        result.IsFailed.Should().BeTrue();

        var error = result.Errors[0];
        error.Metadata.Should().NotContainKey("body");
        error.Metadata.Should().ContainKey("method");
        error.Metadata.Should().ContainKey("uri");
        error.Metadata.Should().ContainKey("status");
        error.Metadata.Should().ContainKey("reason");
    }

    [Fact]
    public async Task ToFailedResultAsync_LongResponseContent_TruncatesContent()
    {
        // Arrange
        var longContent = new string('a', 300); // Longer than default max length of 200
        using var response = CreateHttpResponseMessage(HttpStatusCode.BadRequest, longContent);

        // Act
        var result = await response.ToFailedResultAsync<string>(FailureMessage);

        // Assert
        result.Should().NotBeNull();
        result.IsFailed.Should().BeTrue();

        var error = result.Errors[0];
        error.Metadata.Should().ContainKey("body");
        var bodyValue = error.Metadata["body"].ToString();
        bodyValue.Should().EndWith("...(truncated)");
        bodyValue!.Length.Should().BeLessThan(longContent.Length);
    }

    [Fact]
    public async Task ToFailedResultAsync_MissingRequestMessage_HandlesGracefully()
    {
        // Arrange
        using var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            ReasonPhrase = "Bad Request",
            Content = new StringContent("Error", Encoding.UTF8, "application/json"),
            RequestMessage = null, // Explicitly set to null
        };

        // Act
        var result = await response.ToFailedResultAsync<string>(FailureMessage);

        // Assert
        result.Should().NotBeNull();
        result.IsFailed.Should().BeTrue();

        var error = result.Errors[0];
        error.Metadata.Should().ContainKey("method").WhoseValue.Should().Be("UNKNOWN");
        error.Metadata.Should().ContainKey("uri").WhoseValue.Should().Be("UNKNOWN");
        error.Metadata.Should().ContainKey("status").WhoseValue.Should().Be(400);
        error.Metadata.Should().ContainKey("reason").WhoseValue.Should().Be("Bad Request");
    }

    [Fact]
    public async Task ToFailedResultAsync_DifferentGenericType_ReturnsCorrectFailedResult()
    {
        // Arrange
        using var response = CreateHttpResponseMessage(HttpStatusCode.NotFound, "Not found");

        // Act
        var stringResult = await response.ToFailedResultAsync<string>(FailureMessage);
        var intResult = await response.ToFailedResultAsync<int>(FailureMessage);
        var objectResult = await response.ToFailedResultAsync<object>(FailureMessage);

        // Assert
        stringResult.Should().NotBeNull();
        stringResult.IsFailed.Should().BeTrue();
        stringResult.Errors.Should().ContainSingle(error => error is NotFoundError);

        intResult.Should().NotBeNull();
        intResult.IsFailed.Should().BeTrue();
        intResult.Errors.Should().ContainSingle(error => error is NotFoundError);

        objectResult.Should().NotBeNull();
        objectResult.IsFailed.Should().BeTrue();
        objectResult.Errors.Should().ContainSingle(error => error is NotFoundError);
    }

    [Theory]
    [InlineData("Simple error message")]
    [InlineData("Error with special characters: !@#$%^&*()")]
    [InlineData("")]
    [InlineData(
        "Very long error message that contains multiple sentences and should be handled properly by the extension method without any issues or truncation."
    )]
    public async Task ToFailedResultAsync_VariousFailureMessages_PreservesMessage(string message)
    {
        // Arrange
        using var response = CreateHttpResponseMessage(HttpStatusCode.BadRequest, "Content");

        // Act
        var result = await response.ToFailedResultAsync<string>(message);

        // Assert
        result.Should().NotBeNull();
        result.IsFailed.Should().BeTrue();
        result.Errors[0].Message.Should().Be(message);
    }

    private static HttpResponseMessage CreateHttpResponseMessage(
        HttpStatusCode statusCode,
        string? content = null,
        string? reasonPhrase = null
    )
    {
        var request = new HttpRequestMessage(HttpMethod.Get, TestUri);
        var response = new HttpResponseMessage(statusCode)
        {
            RequestMessage = request,
            ReasonPhrase = reasonPhrase ?? statusCode.ToString(),
        };

        if (content is not null)
        {
            response.Content = new StringContent(content, Encoding.UTF8, "application/json");
        }

        return response;
    }
}
