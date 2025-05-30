using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Exceptions;
using SharedLibrary.Models.ProblemDetails;
using static SharedLibrary.Errors.GenericErrors;

namespace SharedLibrary.Extensions.HttpClient.Internal;

/// <summary>
/// Provides extension methods for internal HTTP responses handling.
/// </summary>
public static class InternalHttpResponseExtensions
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
    ) => Result.Fail<T>(await response.ToErrorAsync(failureMessage));

    /// <summary>
    /// Creates a failed Result with an appropriate error based on the HTTP response.
    /// </summary>
    /// <param name="response">The HTTP response message.</param>
    /// <param name="failureMessage">A context-specific message describing the operation that failed.</param>
    /// <returns>A failed Result with an appropriate error.</returns>
    public static async Task<Result> ToFailedResultAsync(
        this HttpResponseMessage response,
        string failureMessage
    ) => Result.Fail(await response.ToErrorAsync(failureMessage));

    /// <summary>
    /// Maps an unsuccessful HTTP response to an appropriate FluentResults error.
    /// </summary>
    /// <param name="response">The HTTP response message.</param>
    /// <param name="failureMessage">A context-specific message describing the operation that failed.</param>
    /// <returns>A FluentResults error appropriate for the HTTP status code.</returns>
    private static async Task<Error> ToErrorAsync(
        this HttpResponseMessage response,
        string failureMessage
    )
    {
        var problemDetails = await GetProblemDetails(response);

        var reason = ConvertProblemDetailsToErrorReason(problemDetails, response);

        return CreateErrorByStatusCode(response.StatusCode, failureMessage, null, [reason]);
    }

    private static async Task<ExtendedProblemDetails> GetProblemDetails(
        HttpResponseMessage response
    )
    {
        ProblemDetails? problemDetails;
        try
        {
            problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        }
        catch (JsonException jsonEx)
        {
            var httpMethod = response.RequestMessage!.Method;
            var requestUri = response.RequestMessage!.RequestUri!;
            throw new ProblemDetailsException(
                $"Response content from [{httpMethod}]: {requestUri} was not in correct ProblemDetails format.",
                jsonEx
            );
        }

        return new ExtendedProblemDetails(problemDetails!);
    }

    private static Error ConvertProblemDetailsToErrorReason(
        ExtendedProblemDetails problemDetails,
        HttpResponseMessage response
    )
    {
        // Build metadata from HTTP response and ProblemDetails
        var metadata = BuildErrorReasonMetadata(response);
        AddProblemDetailsMetadata(metadata, problemDetails);

        // Extract nested reasons from ProblemDetails
        var reasons = ConvertReasonsToErrors(problemDetails.Reasons);

        return CreateErrorByStatusCode(
            response.StatusCode,
            problemDetails.Detail!,
            metadata,
            reasons
        );
    }

    private static Dictionary<string, object> BuildErrorReasonMetadata(HttpResponseMessage response)
    {
        var metadata = new Dictionary<string, object>
        {
            ["method"] = response.RequestMessage?.Method.Method ?? "UNKNOWN",
            ["uri"] = response.RequestMessage?.RequestUri?.ToString() ?? "UNKNOWN",
            ["status"] = (int)response.StatusCode,
            // ReasonPhrase is never null because it's being automatically generated if not provided.
            ["reason"] = response.ReasonPhrase!,
        };

        return metadata;
    }

    // Adds original metadata from ProblemDetails if present
    private static void AddProblemDetailsMetadata(
        Dictionary<string, object> metadata,
        ExtendedProblemDetails problemDetails
    )
    {
        if (problemDetails.Metadata == null)
        {
            return;
        }

        foreach (var kvp in problemDetails.Metadata)
        {
            metadata[kvp.Key] = kvp.Value;
        }
    }

    private static IEnumerable<Error>? ConvertReasonsToErrors(IEnumerable<ReasonDto>? reasons) =>
        reasons == null
            ? null
            : reasons.Select(ConvertReasonToError).Where(error => error != null)!;

    private static Error ConvertReasonToError(ReasonDto reason)
    {
        // Recursively convert nested reasons
        var nestedReasons = ConvertReasonsToErrors(reason.Reasons);

        return new InternalError(reason.Message, reason.Metadata, nestedReasons);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Style",
        "IDE0072:Add missing cases",
        Justification = "This is a catch-all for all HTTP status codes that are not explicitly handled."
    )]
    private static Error CreateErrorByStatusCode(
        HttpStatusCode statusCode,
        string errorMessage,
        Dictionary<string, object>? metadata,
        IEnumerable<Error>? reasons
    ) =>
        statusCode switch
        {
            HttpStatusCode.BadRequest => new BadRequestError(errorMessage, metadata, reasons),
            HttpStatusCode.Unauthorized => new UnauthorizedError(errorMessage, metadata, reasons),
            HttpStatusCode.Forbidden => new ForbiddenError(errorMessage, metadata, reasons),
            HttpStatusCode.NotFound => new NotFoundError(errorMessage, metadata, reasons),
            HttpStatusCode.Conflict => new ConflictError(errorMessage, metadata, reasons),
            HttpStatusCode.TooManyRequests => new TooManyRequestsError(
                errorMessage,
                metadata,
                reasons
            ),
            HttpStatusCode.InternalServerError => new InternalError(
                errorMessage,
                metadata,
                reasons
            ),
            HttpStatusCode.BadGateway => new GatewayError(errorMessage, metadata, reasons),
            HttpStatusCode.ServiceUnavailable => new UnavailableError(
                errorMessage,
                metadata,
                reasons
            ),
            HttpStatusCode.GatewayTimeout => new TimeoutError(errorMessage, metadata, reasons),
            _ => new InternalError(errorMessage, metadata, reasons),
        };
}
