using System.Net.Http.Json;
using FluentResults;
using Microsoft.Extensions.Logging;
using static SharedLibrary.Errors.GenericErrors;

namespace SharedLibrary.Extensions.HttpClient.External;

/// <summary>
/// Provides extension methods for HttpClient to perform external HTTP requests
/// with graceful error handling.
/// </summary>
public static class ExternalHttpClientExtensions
{
    /// <summary>
    /// Performs an HTTP GET request to fetch JSON data from the specified endpoint
    /// and deserializes it into the given type while handling errors gracefully.
    /// </summary>
    /// <typeparam name="T">The type into which the JSON payload is deserialized.</typeparam>
    /// <param name="client">The HttpClient instance used for making the request.</param>
    /// <param name="uri">The URI of the HTTP GET request.</param>
    /// <param name="logger">The logger instance for logging errors.</param>
    /// <param name="failureMessage">A custom failure message.</param>
    /// <returns>
    /// Success: Result with the deserialized object.
    /// Failure: Failure message.
    /// </returns>
    public static async Task<Result<T>> GetFromJsonSafeAsync<T>(
        this System.Net.Http.HttpClient client,
        string uri,
        ILogger logger,
        string failureMessage
    )
    {
        var response = await client.GetAsync(uri);
        if (!response.IsSuccessStatusCode)
        {
            await logger.LogUnsuccessfulHttpResponse(response);

            return await ExternalHttpResponseExtensions.ToFailedResultAsync<T>(
                response,
                failureMessage
            );
        }

        var content = await response.Content.ReadFromJsonAsync<T>();
        return content is null || content.Equals(default(T))
            ? ToFailedDeserializationResult<T>(response)
            : Result.Ok(content);
    }

    private static Result<T> ToFailedDeserializationResult<T>(HttpResponseMessage response)
    {
        var httpMethod = response.RequestMessage!.Method;
        var requestUri = response.RequestMessage!.RequestUri!;
        string deserializationFailureMessage =
            $"Response content from [{httpMethod}]: {requestUri} was null or could not be deserialized.";
        return Result.Fail(new InternalError(deserializationFailureMessage));
    }
}
