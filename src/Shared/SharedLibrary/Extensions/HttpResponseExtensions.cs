using System.Net;
using FluentResults;
using static SharedLibrary.Errors.GenericErrors;

namespace SharedLibrary.Extensions;

/// <summary>
/// Provides extension methods for HTTP response handling.
/// </summary>
public static class HttpResponseExtensions
{
    /// <summary>
    /// Maps an unsuccessful HTTP response to an appropriate FluentResults error.
    /// </summary>
    /// <param name="response">The HTTP response message.</param>
    /// <param name="contextMessage">A context-specific message describing the operation that failed.</param>
    /// <returns>A FluentResults error appropriate for the HTTP status code.</returns>
    public static Error ToError(this HttpResponseMessage response, string contextMessage)
    {
#pragma warning disable IDE0072 // Add missing cases
        return response.StatusCode switch
        {
            HttpStatusCode.BadRequest => new BadRequestError(
                $"{contextMessage}: {response.ReasonPhrase}"
            ),
            HttpStatusCode.Unauthorized => new UnauthorizedError(
                $"{contextMessage}: {response.ReasonPhrase}"
            ),
            HttpStatusCode.Forbidden => new ForbiddenError(
                $"{contextMessage}: {response.ReasonPhrase}"
            ),
            HttpStatusCode.NotFound => new NotFoundError(
                $"{contextMessage}: {response.ReasonPhrase}"
            ),
            HttpStatusCode.Conflict => new ConflictError(
                $"{contextMessage}: {response.ReasonPhrase}"
            ),
            HttpStatusCode.TooManyRequests => new TooManyRequestsError(
                $"{contextMessage}: {response.ReasonPhrase}"
            ),
            HttpStatusCode.InternalServerError => new InternalError(
                $"{contextMessage}: {response.ReasonPhrase}"
            ),
            HttpStatusCode.BadGateway => new GatewayError(
                $"{contextMessage}: {response.ReasonPhrase}"
            ),
            HttpStatusCode.ServiceUnavailable => new UnavailableError(
                $"{contextMessage}: {response.ReasonPhrase}"
            ),
            HttpStatusCode.GatewayTimeout => new TimeoutError(
                $"{contextMessage}: {response.ReasonPhrase}"
            ),
            _ => new InternalError($"{contextMessage}: {response.ReasonPhrase}"),
        };
#pragma warning restore IDE0072 // Add missing cases
    }

    /// <summary>
    /// Creates a failed Result with an appropriate error based on the HTTP response.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="response">The HTTP response message.</param>
    /// <param name="contextMessage">A context-specific message describing the operation that failed.</param>
    /// <returns>A failed Result with an appropriate error.</returns>
    public static Result<T> ToFailedResult<T>(
        this HttpResponseMessage response,
        string contextMessage
    )
    {
        return Result.Fail<T>(response.ToError(contextMessage));
    }

    /// <summary>
    /// Creates a failed Result with an appropriate error based on the HTTP response.
    /// </summary>
    /// <param name="response">The HTTP response message.</param>
    /// <param name="contextMessage">A context-specific message describing the operation that failed.</param>
    /// <returns>A failed Result with an appropriate error.</returns>
    public static Result ToFailedResult(this HttpResponseMessage response, string contextMessage)
    {
        return Result.Fail(response.ToError(contextMessage));
    }
}
