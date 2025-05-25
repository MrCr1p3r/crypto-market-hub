using System.Net;
using FluentResults;
using static SharedLibrary.Errors.GenericErrors;

namespace SharedLibrary.Extensions.HttpClient.External;

/// <summary>
/// Provides extension methods for HTTP response handling.
/// </summary>
public static class ExternalHttpResponseExtensions
{
    /// <summary>
    /// Creates a failed Result with an appropriate error based on the HTTP response.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="response">The HTTP response message.</param>
    /// <param name="failureMessage">A context-specific message describing the operation that failed.</param>
    /// <returns>A failed Result with an appropriate error.</returns>
    public static async Task<Result<T>> ToFailedResultAsync<T>(
        this HttpResponseMessage response,
        string failureMessage
    )
    {
        return Result.Fail<T>(await response.ToErrorAsync(failureMessage));
    }

    /// <summary>
    /// Maps an unsuccessful HTTP response to an appropriate FluentResults error.
    /// </summary>
    /// <param name="response">The HTTP response message.</param>
    /// <param name="failureMessage">A context-specific message describing the operation that failed.</param>
    /// <returns>A FluentResults error appropriate for the HTTP status code.</returns>
    private static async Task<Error> ToErrorAsync(
        this HttpResponseMessage response,
        string failureMessage,
        int maxContentLength = 200
    )
    {
        var metadata = await BuildErrorMetadata(response, maxContentLength);
        return ToCorrectError(response.StatusCode, failureMessage, metadata);
    }

    private static async Task<Dictionary<string, object>> BuildErrorMetadata(
        HttpResponseMessage response,
        int maxContentLength
    )
    {
        var method = response.RequestMessage?.Method.Method ?? "UNKNOWN";
        var uri = response.RequestMessage?.RequestUri?.ToString() ?? "UNKNOWN";
        var status = (int)response.StatusCode;
        // ReasonPhrase is never null because it's being automatically generated if not provided.
        var reason = response.ReasonPhrase!;
        var content = await GetResponseContext(response, maxContentLength);

        var metadata = new Dictionary<string, object>
        {
            { "method", method },
            { "uri", uri },
            { "status", status },
            { "reason", reason },
        };

        if (!string.IsNullOrEmpty(content))
        {
            metadata.Add("body", content);
        }

        return metadata;
    }

    private static async Task<string> GetResponseContext(
        HttpResponseMessage response,
        int maxContentLength
    )
    {
        var content = string.Empty;
        if (response.Content != null)
        {
            var raw = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrWhiteSpace(raw))
            {
                content =
                    raw.Length <= maxContentLength
                        ? raw
                        : raw[..maxContentLength] + "...(truncated)";
            }
        }

        return content;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Style",
        "IDE0072:Add missing cases",
        Justification = "This is a catch-all for all HTTP status codes that are not explicitly handled."
    )]
    private static Error ToCorrectError(
        HttpStatusCode statusCode,
        string errorMessage,
        Dictionary<string, object> metadata
    ) =>
        statusCode switch
        {
            HttpStatusCode.BadRequest => new BadRequestError(errorMessage, metadata),
            HttpStatusCode.Unauthorized => new UnauthorizedError(errorMessage, metadata),
            HttpStatusCode.Forbidden => new ForbiddenError(errorMessage, metadata),
            HttpStatusCode.NotFound => new NotFoundError(errorMessage, metadata),
            HttpStatusCode.Conflict => new ConflictError(errorMessage, metadata),
            HttpStatusCode.TooManyRequests => new TooManyRequestsError(errorMessage, metadata),
            HttpStatusCode.InternalServerError => new InternalError(errorMessage, metadata),
            HttpStatusCode.BadGateway => new GatewayError(errorMessage, metadata),
            HttpStatusCode.ServiceUnavailable => new UnavailableError(errorMessage, metadata),
            HttpStatusCode.GatewayTimeout => new TimeoutError(errorMessage, metadata),
            _ => new InternalError(errorMessage, metadata),
        };
}
